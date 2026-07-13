using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Models
{
    public class ProxyTunnelLogVM
    {
        public long Id { get; set; }
        public Guid TunnelId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public ProxyTunnelOutcome Outcome { get; set; }

        public string ClientIPAddress { get; set; } = string.Empty;
        public int ClientPort { get; set; }
        public string? ServerIPAddress { get; set; }
        public int ServerPort { get; set; }
        public ProxyTunnelClientProtocol? ClientProtocol { get; set; }

        public ProxyTunnelRejectReason? RejectReason { get; set; }
        public string? ErrorMessage { get; set; }

        public string? TargetHost { get; set; }
        public int? TargetPort { get; set; }

        public ProxyTunnelAuthMethod? AuthMethod { get; set; }
        public string? AuthUserName { get; set; }
        public string? AuthPassword { get; set; }

        public ProxyTunnelRoutingDecision? RoutingDecision { get; set; }
        public ProxySourceGroupFilterType? MatchedFilterType { get; set; }
        public string? MatchedFilterPattern { get; set; }
        public Guid? MatchedGroupId { get; set; }
        public string? MatchedGroupName { get; set; }

        public Guid? PickedSourceId { get; set; }
        public string? PickedSourceAddress { get; set; }
        public int? PickedSourcePort { get; set; }
        public ProxyType? PickedSourceProxyType { get; set; }
        public string? PickedSourceUserName { get; set; }

        public long TotalBytesUpload { get; set; }
        public long TotalBytesDownload { get; set; }
    }

    public class ProxyTunnelLogListItemVM
    {
        public long Id { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public ProxyTunnelOutcome Outcome { get; set; }
        public string ClientIPAddress { get; set; } = string.Empty;
        public int ClientPort { get; set; }
        public string? ServerIPAddress { get; set; }
        public int ServerPort { get; set; }
        public ProxyTunnelClientProtocol? ClientProtocol { get; set; }
        public string? TargetHost { get; set; }
        public int? TargetPort { get; set; }
        public ProxyTunnelRejectReason? RejectReason { get; set; }
        public string? PickedSourceAddress { get; set; }
        public int? PickedSourcePort { get; set; }
        public ProxyType? PickedSourceProxyType { get; set; }
        public string? MatchedGroupName { get; set; }
        public long TotalBytesUpload { get; set; }
        public long TotalBytesDownload { get; set; }
    }

    public class ProxyTunnelLogListRequestVM
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public ProxyTunnelOutcome? Outcome { get; set; }
        public string? ClientServer { get; set; }
        public string? TargetHost { get; set; }
        public string? PickedSource { get; set; }
        public ProxyType? PickedSourceProxyType { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public ProxyTunnelLogSortBy? SortBy { get; set; }
        public bool SortDesc { get; set; } = true;
    }

    public class ProxyTunnelLogListResponseVM
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<ProxyTunnelLogListItemVM> Items { get; set; } = new();
    }
}
