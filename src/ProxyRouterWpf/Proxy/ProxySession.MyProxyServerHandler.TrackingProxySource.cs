using System.IO;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Proxy.EventLogs;
using TqkLibrary.Proxy.Interfaces;

namespace ProxyRouterWpf.Proxy
{
    public partial class ProxySession
    {
        partial class MyProxyServerHandler
        {
            // Wraps the IProxySource handed to the library to catch exceptions during the
            // upstream-connect chain (GetConnectSourceAsync → ConnectAsync → GetStreamAsync) and tag
            // the state as RouteFailed/UpstreamConnectFailed before rethrowing.
            sealed class TrackingProxySource : IProxySource
            {
                readonly IProxySource _inner;
                readonly IProxyEventLogService _logService;
                readonly Guid _tunnelId;

                public TrackingProxySource(IProxySource inner, IProxyEventLogService logService, Guid tunnelId)
                {
                    _inner = inner;
                    _logService = logService;
                    _tunnelId = tunnelId;
                }

                public bool IsSupportUdp => _inner.IsSupportUdp;
                public bool IsSupportIpv6 => _inner.IsSupportIpv6;
                public bool IsSupportBind => _inner.IsSupportBind;

                public async Task<IConnectSource> GetConnectSourceAsync(Guid tunnelId, CancellationToken cancellationToken = default)
                {
                    try
                    {
                        var connect = await _inner.GetConnectSourceAsync(tunnelId, cancellationToken).ConfigureAwait(false);
                        return new TrackingConnectSource(connect, _logService, _tunnelId);
                    }
                    catch (Exception ex)
                    {
                        MarkRouteFailed(ex);
                        throw;
                    }
                }

                public async Task<IBindSource> GetBindSourceAsync(Guid tunnelId, CancellationToken cancellationToken = default)
                {
                    try
                    {
                        return await _inner.GetBindSourceAsync(tunnelId, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        MarkRouteFailed(ex);
                        throw;
                    }
                }

                public async Task<IUdpAssociateSource> GetUdpAssociateSourceAsync(Guid tunnelId, CancellationToken cancellationToken = default)
                {
                    try
                    {
                        return await _inner.GetUdpAssociateSourceAsync(tunnelId, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        MarkRouteFailed(ex);
                        throw;
                    }
                }

                void MarkRouteFailed(Exception ex)
                {
                    var state = _logService.GetState(_tunnelId);
                    if (state == null) return;
                    state.PendingOutcome = ProxyTunnelOutcome.RouteFailed;
                    state.PendingRejectReason = ProxyTunnelRejectReason.UpstreamConnectFailed;
                    state.PendingErrorMessage = ex.Message;
                }

                sealed class TrackingConnectSource : IConnectSource
                {
                    readonly IConnectSource _inner;
                    readonly IProxyEventLogService _logService;
                    readonly Guid _tunnelId;

                    public TrackingConnectSource(IConnectSource inner, IProxyEventLogService logService, Guid tunnelId)
                    {
                        _inner = inner;
                        _logService = logService;
                        _tunnelId = tunnelId;
                    }

                    public async Task ConnectAsync(Uri address, CancellationToken cancellationToken = default)
                    {
                        try
                        {
                            await _inner.ConnectAsync(address, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            MarkRouteFailed(ex);
                            throw;
                        }
                    }

                    public async Task<Stream> GetStreamAsync(CancellationToken cancellationToken = default)
                    {
                        try
                        {
                            return await _inner.GetStreamAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            MarkRouteFailed(ex);
                            throw;
                        }
                    }

                    public void Dispose() => _inner.Dispose();

                    void MarkRouteFailed(Exception ex)
                    {
                        var state = _logService.GetState(_tunnelId);
                        if (state == null) return;
                        state.PendingOutcome = ProxyTunnelOutcome.RouteFailed;
                        state.PendingRejectReason = ProxyTunnelRejectReason.UpstreamConnectFailed;
                        state.PendingErrorMessage = ex.Message;
                    }
                }
            }
        }
    }
}
