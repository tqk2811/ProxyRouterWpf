using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    /// <summary>
    /// LINQ-to-objects port of the EF-backed query service. Filters/sorts/pages over the RAM FIFO
    /// store. String filters use OrdinalIgnoreCase to mirror SQL Server's case-insensitive collation.
    /// </summary>
    public class ProxyTunnelLogService : IProxyTunnelLogService
    {
        readonly InMemoryTunnelLogStore _store;

        public ProxyTunnelLogService(InMemoryTunnelLogStore store)
        {
            _store = store;
        }

        static bool ContainsCI(string? haystack, string needle)
            => haystack != null && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

        public ProxyTunnelLogListResponseVM List(ProxyTunnelLogListRequestVM request)
        {
            int page = request.Page < 1 ? 1 : request.Page;
            int pageSize = request.PageSize switch
            {
                < 1 => 50,
                > 200 => 200,
                _ => request.PageSize,
            };

            IEnumerable<ProxyTunnelLogVM> q = _store.Snapshot();

            if (request.Outcome.HasValue)
                q = q.Where(x => x.Outcome == request.Outcome.Value);

            if (!string.IsNullOrWhiteSpace(request.ClientServer))
            {
                var kw = request.ClientServer.Trim();
                var (addrPart, portPart) = ParseAddressPort(kw);
                if (portPart.HasValue)
                {
                    var port = portPart.Value;
                    if (string.IsNullOrEmpty(addrPart))
                        q = q.Where(x => x.ClientPort == port || x.ServerPort == port);
                    else
                        q = q.Where(x =>
                            (ContainsCI(x.ClientIPAddress, addrPart) && x.ClientPort == port)
                            || (ContainsCI(x.ServerIPAddress, addrPart) && x.ServerPort == port));
                }
                else
                {
                    q = q.Where(x => ContainsCI(x.ClientIPAddress, kw) || ContainsCI(x.ServerIPAddress, kw));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.TargetHost))
            {
                var kw = request.TargetHost.Trim();
                var (addrPart, portPart) = ParseAddressPort(kw);
                if (portPart.HasValue)
                {
                    var port = portPart.Value;
                    if (string.IsNullOrEmpty(addrPart))
                        q = q.Where(x => x.TargetPort == port);
                    else
                        q = q.Where(x => ContainsCI(x.TargetHost, addrPart) && x.TargetPort == port);
                }
                else
                {
                    q = q.Where(x => ContainsCI(x.TargetHost, kw));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.PickedSource))
            {
                var kw = request.PickedSource.Trim();
                var (addrPart, portPart) = ParseAddressPort(kw);
                if (portPart.HasValue)
                {
                    var port = portPart.Value;
                    if (string.IsNullOrEmpty(addrPart))
                        q = q.Where(x => x.PickedSourcePort == port);
                    else
                        q = q.Where(x => ContainsCI(x.PickedSourceAddress, addrPart) && x.PickedSourcePort == port);
                }
                else
                {
                    q = q.Where(x => ContainsCI(x.PickedSourceAddress, kw));
                }
            }

            if (request.PickedSourceProxyType.HasValue)
                q = q.Where(x => x.PickedSourceProxyType == request.PickedSourceProxyType.Value);
            if (request.FromUtc.HasValue)
                q = q.Where(x => x.EndAt >= request.FromUtc.Value);
            if (request.ToUtc.HasValue)
                q = q.Where(x => x.EndAt <= request.ToUtc.Value);

            var list = q.ToList();
            int total = list.Count;

            var ordered = ApplySort(list, request.SortBy, request.SortDesc);

            var items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToListItem)
                .ToList();

            return new ProxyTunnelLogListResponseVM
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items,
            };
        }

        static IEnumerable<ProxyTunnelLogVM> ApplySort(List<ProxyTunnelLogVM> list, ProxyTunnelLogSortBy? sortBy, bool desc)
        {
            IOrderedEnumerable<ProxyTunnelLogVM> ordered = (sortBy ?? ProxyTunnelLogSortBy.EndAt) switch
            {
                ProxyTunnelLogSortBy.StartAt => desc ? list.OrderByDescending(x => x.StartAt) : list.OrderBy(x => x.StartAt),
                ProxyTunnelLogSortBy.Outcome => desc ? list.OrderByDescending(x => x.Outcome) : list.OrderBy(x => x.Outcome),
                ProxyTunnelLogSortBy.TotalBytesUpload => desc ? list.OrderByDescending(x => x.TotalBytesUpload) : list.OrderBy(x => x.TotalBytesUpload),
                ProxyTunnelLogSortBy.TotalBytesDownload => desc ? list.OrderByDescending(x => x.TotalBytesDownload) : list.OrderBy(x => x.TotalBytesDownload),
                ProxyTunnelLogSortBy.TargetHost => desc ? list.OrderByDescending(x => x.TargetHost).ThenByDescending(x => x.TargetPort) : list.OrderBy(x => x.TargetHost).ThenBy(x => x.TargetPort),
                ProxyTunnelLogSortBy.RejectReason => desc ? list.OrderByDescending(x => x.RejectReason) : list.OrderBy(x => x.RejectReason),
                ProxyTunnelLogSortBy.MatchedGroupName => desc ? list.OrderByDescending(x => x.MatchedGroupName) : list.OrderBy(x => x.MatchedGroupName),
                ProxyTunnelLogSortBy.PickedSourceAddress => desc ? list.OrderByDescending(x => x.PickedSourceAddress).ThenByDescending(x => x.PickedSourcePort) : list.OrderBy(x => x.PickedSourceAddress).ThenBy(x => x.PickedSourcePort),
                ProxyTunnelLogSortBy.ClientAddress => desc ? list.OrderByDescending(x => x.ClientIPAddress).ThenByDescending(x => x.ClientPort) : list.OrderBy(x => x.ClientIPAddress).ThenBy(x => x.ClientPort),
                ProxyTunnelLogSortBy.ServerPort => desc ? list.OrderByDescending(x => x.ServerPort) : list.OrderBy(x => x.ServerPort),
                _ => desc ? list.OrderByDescending(x => x.EndAt) : list.OrderBy(x => x.EndAt),
            };
            return desc ? ordered.ThenByDescending(x => x.Id) : ordered.ThenBy(x => x.Id);
        }

        static (string addr, int? port) ParseAddressPort(string input)
        {
            if (input.Length > 1 && input[0] == ':' && int.TryParse(input.AsSpan(1), out var portOnlyColon))
                return (string.Empty, portOnlyColon);
            if (int.TryParse(input, out var portOnly))
                return (string.Empty, portOnly);
            int idx = input.LastIndexOf(':');
            if (idx > 0 && idx < input.Length - 1 && int.TryParse(input.AsSpan(idx + 1), out var p))
                return (input.Substring(0, idx), p);
            return (input, null);
        }

        static ProxyTunnelLogListItemVM ToListItem(ProxyTunnelLogVM x) => new()
        {
            Id = x.Id,
            StartAt = x.StartAt,
            EndAt = x.EndAt,
            Outcome = x.Outcome,
            ClientIPAddress = x.ClientIPAddress,
            ClientPort = x.ClientPort,
            ServerIPAddress = x.ServerIPAddress,
            ServerPort = x.ServerPort,
            ClientProtocol = x.ClientProtocol,
            TargetHost = x.TargetHost,
            TargetPort = x.TargetPort,
            RejectReason = x.RejectReason,
            PickedSourceAddress = x.PickedSourceAddress,
            PickedSourcePort = x.PickedSourcePort,
            PickedSourceProxyType = x.PickedSourceProxyType,
            MatchedGroupName = x.MatchedGroupName,
            TotalBytesUpload = x.TotalBytesUpload,
            TotalBytesDownload = x.TotalBytesDownload,
        };

        public ProxyTunnelLogVM? GetById(long id) => _store.GetById(id);

        public int Count() => _store.Count;

        public void Clear() => _store.Clear();
    }
}
