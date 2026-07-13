using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    // Per-TCP-tunnel state — lives in ProxyEventLogService._states (ConcurrentDictionary) from
    // TrackTunnelStart until Commit/Discard/Cleanup removes it. Each field is set at a specific
    // pipeline stage; a field still null at Commit means the pipeline never reached that stage.
    public class ProxyTunnelLogState
    {
        public Guid TunnelId { get; init; }

        public required string ClientIPAddress { get; init; }
        public required int ClientPort { get; init; }
        public string? ServerIPAddress { get; init; }
        public int ServerPort { get; init; }
        public ProxyTunnelClientProtocol? ClientProtocol { get; set; }
        public DateTime StartedAt { get; init; }
        public DateTime LastTouchedAt { get; set; }

        public DateTime EndAt { get; set; }
        public ProxyTunnelOutcome Outcome { get; set; }
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

        long _totalBytesUpload;
        long _totalBytesDownload;

        public long TotalBytesUpload => Interlocked.Read(ref _totalBytesUpload);
        public long TotalBytesDownload => Interlocked.Read(ref _totalBytesDownload);

        public void AddBytesUpload(long bytes) => Interlocked.Add(ref _totalBytesUpload, bytes);
        public void AddBytesDownload(long bytes) => Interlocked.Add(ref _totalBytesDownload, bytes);

        // Expected outcome consumed by BytesCountingStream.Dispose. Default Resolved; overridden by
        // hooks when a failure is detected before the tunnel closes.
        public ProxyTunnelOutcome PendingOutcome { get; set; } = ProxyTunnelOutcome.Resolved;
        public ProxyTunnelRejectReason? PendingRejectReason { get; set; }
        public string? PendingErrorMessage { get; set; }
    }
}
