using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    // Per-TCP-tunnel state — lives in ProxyEventLogService._states (ConcurrentDictionary) from
    // TrackTunnelStart until Commit/Discard/Cleanup removes it. Each field is set at a specific
    // pipeline stage; a field still null at Commit means the pipeline never reached that stage.
    public class ProxyTunnelLogState
    {
        public Guid TunnelId { get; init; }

        /// <summary>
        /// Log row id reserved by <see cref="InMemoryTunnelLogStore.AddLive"/> the moment the
        /// connection arrives, so the live row and the committed row are the same row for the UI.
        /// </summary>
        public long LogId { get; set; }

        public required string ClientIPAddress { get; init; }
        public required int ClientPort { get; init; }
        public string? ServerIPAddress { get; init; }
        public int ServerPort { get; init; }
        public ProxyTunnelClientProtocol? ClientProtocol { get; set; }
        public DateTime StartedAt { get; init; }
        public DateTime LastTouchedAt { get; set; }

        /// <summary>Null while the tunnel is still open (Outcome stays <see cref="ProxyTunnelOutcome.Active"/>).</summary>
        public DateTime? EndAt { get; set; }
        public ProxyTunnelOutcome Outcome { get; set; } = ProxyTunnelOutcome.Active;
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

        // Traffic also counts as activity: without this a tunnel busy for longer than the abandoned
        // TTL (5 min) would be evicted by CleanupAbandoned and its log lost.
        public void AddBytesUpload(long bytes)
        {
            Interlocked.Add(ref _totalBytesUpload, bytes);
            LastTouchedAt = DateTime.UtcNow;
        }

        public void AddBytesDownload(long bytes)
        {
            Interlocked.Add(ref _totalBytesDownload, bytes);
            LastTouchedAt = DateTime.UtcNow;
        }

        // Expected outcome consumed by BytesCountingStream.Dispose. Default Resolved; overridden by
        // hooks when a failure is detected before the tunnel closes.
        public ProxyTunnelOutcome PendingOutcome { get; set; } = ProxyTunnelOutcome.Resolved;
        public ProxyTunnelRejectReason? PendingRejectReason { get; set; }
        public string? PendingErrorMessage { get; set; }
    }
}
