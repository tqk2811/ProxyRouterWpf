using CommunityToolkit.Mvvm.ComponentModel;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Models
{
    public class ProxyTunnelLogVM
    {
        public long Id { get; set; }
        public Guid TunnelId { get; set; }
        public DateTime StartAt { get; set; }
        /// <summary>Null = tunnel still open (see <see cref="ProxyTunnelOutcome.Active"/>).</summary>
        public DateTime? EndAt { get; set; }
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

        /// <summary>Tunnel is still open: every field below the connect stage may still change.</summary>
        public bool IsLive => EndAt is null;

        /// <summary>
        /// Time-filter key: a live row has no EndAt yet, so its StartAt stands in — otherwise an
        /// open tunnel would drop out of the list as soon as a From/To bound is set.
        /// </summary>
        public DateTime EffectiveAt => EndAt ?? StartAt;
    }

    /// <summary>
    /// Grid row. Observable because live rows are refreshed in place by the Logs tab timer
    /// (bytes/outcome keep changing while the tunnel is open) instead of rebuilding the collection.
    /// </summary>
    public partial class ProxyTunnelLogListItemVM : ObservableObject
    {
        [ObservableProperty] long id;
        [ObservableProperty] DateTime startAt;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLive))]
        DateTime? endAt;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDirectFromVps))]
        [NotifyPropertyChangedFor(nameof(ShowPickedSourceDash))]
        ProxyTunnelOutcome outcome;

        [ObservableProperty] string clientIPAddress = string.Empty;
        [ObservableProperty] int clientPort;
        [ObservableProperty] string? serverIPAddress;
        [ObservableProperty] int serverPort;
        [ObservableProperty] ProxyTunnelClientProtocol? clientProtocol;
        [ObservableProperty] string? targetHost;
        [ObservableProperty] int? targetPort;
        [ObservableProperty] ProxyTunnelRejectReason? rejectReason;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasPickedSource))]
        [NotifyPropertyChangedFor(nameof(IsDirectFromVps))]
        [NotifyPropertyChangedFor(nameof(ShowPickedSourceDash))]
        string? pickedSourceAddress;

        [ObservableProperty] int? pickedSourcePort;
        [ObservableProperty] ProxyType? pickedSourceProxyType;
        [ObservableProperty] string? matchedGroupName;
        [ObservableProperty] long totalBytesUpload;
        [ObservableProperty] long totalBytesDownload;

        /// <summary>True while the tunnel is still open — shown as the "Active" status badge.</summary>
        public bool IsLive => EndAt is null;
        /// <summary>True when an upstream proxy was picked (show its address + type badge).</summary>
        public bool HasPickedSource => !string.IsNullOrEmpty(PickedSourceAddress);
        /// <summary>Resolved with no picked source = traffic went out directly from the VPS.</summary>
        public bool IsDirectFromVps => Outcome == ProxyTunnelOutcome.Resolved && string.IsNullOrEmpty(PickedSourceAddress);
        /// <summary>Neither a picked source nor a direct-from-VPS resolve: show the "—" placeholder.</summary>
        public bool ShowPickedSourceDash => !HasPickedSource && !IsDirectFromVps;

        /// <summary>Refresh this row from a freshly queried one, keeping the instance (and the grid selection).</summary>
        public void CopyFrom(ProxyTunnelLogListItemVM o)
        {
            Id = o.Id;
            StartAt = o.StartAt;
            EndAt = o.EndAt;
            Outcome = o.Outcome;
            ClientIPAddress = o.ClientIPAddress;
            ClientPort = o.ClientPort;
            ServerIPAddress = o.ServerIPAddress;
            ServerPort = o.ServerPort;
            ClientProtocol = o.ClientProtocol;
            TargetHost = o.TargetHost;
            TargetPort = o.TargetPort;
            RejectReason = o.RejectReason;
            PickedSourceAddress = o.PickedSourceAddress;
            PickedSourcePort = o.PickedSourcePort;
            PickedSourceProxyType = o.PickedSourceProxyType;
            MatchedGroupName = o.MatchedGroupName;
            TotalBytesUpload = o.TotalBytesUpload;
            TotalBytesDownload = o.TotalBytesDownload;
        }
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
