using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;
using TqkLibrary.Proxy.Authentications;
using TqkLibrary.Proxy.Enums;
using TqkLibrary.Proxy.Handlers;
using TqkLibrary.Proxy.Interfaces;
using TqkLibrary.Proxy.ProxySources;

namespace ProxyRouterWpf.Proxy
{
    public partial class ProxySession
    {
        partial class MyProxyServerHandler : BaseProxyServerHandler
        {
            readonly ProxySession _proxySession;
            readonly ProxySourceVM _proxySourceVM;
            readonly ILogger _logger;

            public MyProxyServerHandler(ProxySession proxySession, ProxySourceVM proxySourceVM)
            {
                _proxySession = proxySession;
                _proxySourceVM = proxySourceVM;
                _logger = proxySession._loggerFactory.CreateLogger<MyProxyServerHandler>();
            }

            public override Task<bool> IsAcceptUserAsync(
                IUserInfo userInfo,
                CancellationToken cancellationToken = default)
            {
                var userConfig = _proxySession._configureService.Get();

                bool requiresAuth = !string.IsNullOrWhiteSpace(userConfig.ProxyUserName)
                                    && !string.IsNullOrWhiteSpace(userConfig.ProxyPassword);

                var logService = _proxySession._logService;
                var state = logService.GetState(userInfo.TunnelId);

                // SOCKS4: no password — match the request UserId against ProxySocks4UserId.
                // Empty config means accept any client.
                if (userInfo.Authentication is Socks4Authentication socks4Auth)
                {
                    if (state != null)
                    {
                        state.AuthMethod = ProxyTunnelAuthMethod.Socks4UserId;
                        state.AuthUserName = socks4Auth.UserId;
                    }
                    if (string.IsNullOrWhiteSpace(userConfig.ProxySocks4UserId))
                        return Task.FromResult(true);

                    bool ok = string.Equals(userConfig.ProxySocks4UserId, socks4Auth.UserId);
                    if (!ok)
                    {
                        logService.Commit(userInfo.TunnelId, ProxyTunnelOutcome.AuthRejected, ProxyTunnelRejectReason.Socks4UserIdMismatch);
                    }
                    return Task.FromResult(ok);
                }

                // SOCKS5 greeting phase: pick the auth method to advertise back.
                if (userInfo.Authentication is Socks5Authentication socks5Auth)
                {
                    if (state != null)
                        state.AuthMethod = ProxyTunnelAuthMethod.Socks5Greeting;

                    if (requiresAuth)
                    {
                        if (socks5Auth.Auths.Contains(Socks5_Auth.UsernamePassword))
                        {
                            socks5Auth.Choice = Socks5_Auth.UsernamePassword;
                            return Task.FromResult(true);
                        }
                        logService.Commit(userInfo.TunnelId, ProxyTunnelOutcome.AuthRejected, ProxyTunnelRejectReason.Socks5GreetingMissingUsernamePassword);
                        return Task.FromResult(false);
                    }

                    if (socks5Auth.Auths.Contains(Socks5_Auth.NoAuthentication))
                    {
                        socks5Auth.Choice = Socks5_Auth.NoAuthentication;
                        if (state != null)
                            state.AuthMethod = ProxyTunnelAuthMethod.NoAuthRequired;
                        return Task.FromResult(true);
                    }
                    logService.Commit(userInfo.TunnelId, ProxyTunnelOutcome.AuthRejected, ProxyTunnelRejectReason.Socks5GreetingNoAcceptableMethod);
                    return Task.FromResult(false);
                }

                // HTTP Basic auth or SOCKS5 post-sub-negotiation: validate the supplied credential.
                var credMethod = (state?.AuthMethod == ProxyTunnelAuthMethod.Socks5Greeting)
                    ? ProxyTunnelAuthMethod.Socks5UserPassword
                    : ProxyTunnelAuthMethod.HttpBasic;

                if (requiresAuth)
                {
                    if (userInfo.Authentication is ProxyCredential proxyCredential)
                    {
                        if (state != null)
                        {
                            state.AuthMethod = credMethod;
                            state.AuthUserName = proxyCredential.UserName;
                            state.AuthPassword = proxyCredential.Password;
                        }
                        if (string.Equals(userConfig.ProxyUserName, proxyCredential.UserName)
                            && string.Equals(userConfig.ProxyPassword, proxyCredential.Password))
                        {
                            return Task.FromResult(true);
                        }
                        logService.Commit(userInfo.TunnelId, ProxyTunnelOutcome.AuthRejected, ProxyTunnelRejectReason.CredentialMismatch);
                    }
                    else
                    {
                        if (state != null)
                            state.AuthMethod = credMethod;
                        logService.Commit(userInfo.TunnelId, ProxyTunnelOutcome.AuthRejected, ProxyTunnelRejectReason.MissingCredential);
                    }
                    return Task.FromResult(false);
                }

                if (state != null && state.AuthMethod != ProxyTunnelAuthMethod.Socks4UserId)
                    state.AuthMethod = ProxyTunnelAuthMethod.NoAuthRequired;
                return Task.FromResult(true);
            }

            public override async Task<IProxySource> GetProxySourceAsync(
                Uri? uri,
                IUserInfo userInfo,
                CancellationToken cancellationToken = default)
            {
                var filterService = _proxySession._filterService;
                var sourceService = _proxySession._sourceService;

                var logService = _proxySession._logService;
                var state = logService.GetState(userInfo.TunnelId);
                if (state != null && uri != null)
                {
                    state.TargetHost = uri.Host;
                    state.TargetPort = uri.Port;
                }

                var snapshots = filterService.ListGroupSnapshots();

                ProxySourceGroupSnapshotVM? matchedGroup = null;
                ProxySourceGroupFilterVM? matchedFilter = null;
                string? matchedFilterPatternForLog = null;

                if (uri != null)
                {
                    (long maxUpload, long maxDownload, long maxBoth)? hostTraffic = null;
                    bool hasTotalBytes = snapshots.Any(g => g.Filters.Any(f => f.FilterType == ProxySourceGroupFilterType.TotalBytes));
                    if (hasTotalBytes && !string.IsNullOrEmpty(uri.Host))
                    {
                        hostTraffic = _proxySession._trafficCache.GetOrLoad(uri.Host);
                    }

                    foreach (var group in snapshots)
                    {
                        if (group.Filters.Count == 0) continue;

                        bool groupMatched;
                        ProxySourceGroupFilterVM? reportFilter = null;

                        if (group.MatchMode == ProxySourceGroupMatchMode.And)
                        {
                            groupMatched = true;
                            foreach (var filter in group.Filters)
                            {
                                var filterMatch = EvaluateFilter(uri, filter, hostTraffic);
                                if (!filterMatch)
                                {
                                    groupMatched = false;
                                    break;
                                }
                                reportFilter ??= filter;
                            }
                        }
                        else
                        {
                            groupMatched = false;
                            foreach (var filter in group.Filters)
                            {
                                if (EvaluateFilter(uri, filter, hostTraffic))
                                {
                                    groupMatched = true;
                                    reportFilter = filter;
                                    break;
                                }
                            }
                        }

                        if (groupMatched)
                        {
                            matchedGroup = group;
                            matchedFilter = reportFilter;
                            if (matchedFilter != null)
                            {
                                if (matchedFilter.FilterType == ProxySourceGroupFilterType.TotalBytes && hostTraffic.HasValue)
                                    matchedFilterPatternForLog = FormatTotalBytesPattern(matchedFilter, hostTraffic.Value.maxUpload, hostTraffic.Value.maxDownload, hostTraffic.Value.maxBoth);
                                else
                                    matchedFilterPatternForLog = matchedFilter.Filter;
                            }
                            break;
                        }
                    }
                }

                ProxySourceVM picked;
                if (matchedGroup != null)
                {
                    var proxySources = sourceService.ListByGroup(matchedGroup.GroupId);

                    if (state != null)
                    {
                        if (matchedFilter != null)
                        {
                            state.MatchedFilterType = matchedFilter.FilterType;
                            state.MatchedFilterPattern = matchedFilterPatternForLog ?? matchedFilter.Filter;
                        }
                        state.MatchedGroupId = matchedGroup.GroupId;
                        state.MatchedGroupName = matchedGroup.GroupName;
                    }

                    if (proxySources.Count == 0)
                    {
                        _logger.LogInformation("GetProxySource matched filter but group empty → local Uri={Uri}, GroupId={GroupId}", uri, matchedGroup.GroupId);
                        if (state != null)
                            state.RoutingDecision = ProxyTunnelRoutingDecision.GroupEmptyFallback;
                        var localSource = await base.GetProxySourceAsync(uri, userInfo, cancellationToken).ConfigureAwait(false);
                        return new TrackingProxySource(localSource, logService, userInfo.TunnelId);
                    }
                    picked = proxySources[Random.Shared.Next(proxySources.Count)];
                    if (state != null)
                        state.RoutingDecision = ProxyTunnelRoutingDecision.GroupMatched;
                }
                else
                {
                    picked = _proxySourceVM;
                    if (state != null)
                        state.RoutingDecision = ProxyTunnelRoutingDecision.Default;
                }

                if (state != null)
                {
                    state.PickedSourceId = picked.Id;
                    state.PickedSourceAddress = picked.Address;
                    state.PickedSourcePort = picked.Port;
                    state.PickedSourceProxyType = picked.ProxyType;
                    state.PickedSourceUserName = picked.UserName;
                }

                Uri proxyUri = picked.GetUri();
                var loggerFactory = _proxySession._loggerFactory;
                IProxySource innerSource;
                switch (picked.ProxyType)
                {
                    case ProxyType.Http:
                    case ProxyType.Https:
                        innerSource = new HttpProxySource(proxyUri, loggerFactory);
                        return new TrackingProxySource(innerSource, logService, userInfo.TunnelId);

                    case ProxyType.Socks4:
                        innerSource = new Socks4ProxySource(proxyUri, loggerFactory);
                        return new TrackingProxySource(innerSource, logService, userInfo.TunnelId);

                    case ProxyType.Socks5:
                        innerSource = new Socks5ProxySource(proxyUri, loggerFactory);
                        return new TrackingProxySource(innerSource, logService, userInfo.TunnelId);

                    default:
                        _logger.LogError("GetProxySource unsupported ProxyType={ProxyType} SourceId={SourceId}", picked.ProxyType, picked.Id);
                        logService.Commit(userInfo.TunnelId, ProxyTunnelOutcome.RouteFailed, ProxyTunnelRejectReason.ProxyTypeUnsupported, $"Unsupported proxy type {picked.ProxyType}");
                        throw new InvalidOperationException($"Unsupported proxy type {picked.ProxyType}");
                }
            }

            static bool EvaluateFilter(Uri uri, ProxySourceGroupFilterVM filter, (long maxUpload, long maxDownload, long maxBoth)? hostTraffic)
            {
                bool match;
                if (filter.FilterType == ProxySourceGroupFilterType.TotalBytes)
                    match = hostTraffic.HasValue && MatchesTotalBytes(filter, hostTraffic.Value.maxUpload, hostTraffic.Value.maxDownload, hostTraffic.Value.maxBoth);
                else
                    match = IsMatchHost(uri, filter);
                return filter.IsNot ? !match : match;
            }

            static bool IsMatchHost(Uri uri, ProxySourceGroupFilterVM filter)
            {
                var host = uri.Host;
                if (string.IsNullOrEmpty(host)) return false;
                var pattern = filter.Filter?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(pattern)) return false;

                switch (filter.FilterType)
                {
                    case ProxySourceGroupFilterType.Equals:
                        return string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
                    case ProxySourceGroupFilterType.StartsWith:
                        return host.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
                    case ProxySourceGroupFilterType.EndsWith:
                        return host.EndsWith(pattern, StringComparison.OrdinalIgnoreCase);
                    case ProxySourceGroupFilterType.Contains:
                        return host.Contains(pattern, StringComparison.OrdinalIgnoreCase);
                    case ProxySourceGroupFilterType.Wildcard:
                        {
                            var regexPattern = "^" + Regex.Escape(pattern)
                                .Replace("\\*", ".*")
                                .Replace("\\?", ".") + "$";
                            return Regex.IsMatch(host, regexPattern, RegexOptions.IgnoreCase);
                        }
                    case ProxySourceGroupFilterType.Regex:
                        try { return Regex.IsMatch(host, pattern, RegexOptions.IgnoreCase); }
                        catch { return false; }
                    case ProxySourceGroupFilterType.CidrIp:
                        return MatchesCidr(host, pattern);
                    default:
                        return false;
                }
            }

            static bool MatchesTotalBytes(ProxySourceGroupFilterVM filter, long maxUp, long maxDown, long maxBoth)
            {
                if (!filter.TrafficDirection.HasValue) return false;
                if (!long.TryParse(filter.Filter, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold)) return false;
                long actual = filter.TrafficDirection.Value switch
                {
                    ProxyTrafficDirection.Upload => maxUp,
                    ProxyTrafficDirection.Download => maxDown,
                    ProxyTrafficDirection.Both => maxBoth,
                    _ => 0,
                };
                return actual >= threshold;
            }

            static string FormatTotalBytesPattern(ProxySourceGroupFilterVM filter, long maxUp, long maxDown, long maxBoth)
            {
                long actual = filter.TrafficDirection switch
                {
                    ProxyTrafficDirection.Upload => maxUp,
                    ProxyTrafficDirection.Download => maxDown,
                    ProxyTrafficDirection.Both => maxBoth,
                    _ => 0,
                };
                return $"max({filter.TrafficDirection})>={filter.Filter} (actual={actual})";
            }

            static bool MatchesCidr(string host, string cidr)
            {
                if (!IPAddress.TryParse(host, out var ip)) return false;
                var parts = cidr.Split('/', 2);
                if (!IPAddress.TryParse(parts[0], out var network)) return false;
                if (parts.Length == 1) return ip.Equals(network);
                if (!int.TryParse(parts[1], out var prefix) || prefix < 0) return false;

                var ipBytes = ip.GetAddressBytes();
                var networkBytes = network.GetAddressBytes();
                if (ipBytes.Length != networkBytes.Length) return false;
                if (prefix > ipBytes.Length * 8) return false;

                int fullBytes = prefix / 8;
                int remainingBits = prefix % 8;
                for (int i = 0; i < fullBytes; i++)
                {
                    if (ipBytes[i] != networkBytes[i]) return false;
                }
                if (remainingBits > 0 && fullBytes < ipBytes.Length)
                {
                    int mask = (0xFF << (8 - remainingBits)) & 0xFF;
                    if ((ipBytes[fullBytes] & mask) != (networkBytes[fullBytes] & mask)) return false;
                }
                return true;
            }
        }
    }
}
