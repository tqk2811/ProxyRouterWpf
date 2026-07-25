using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    /// <summary>
    /// State -> log VM projection. Used both by the channel consumer (committed rows) and by
    /// <see cref="InMemoryTunnelLogStore.Snapshot"/> (live rows re-projected on every query, which is
    /// what makes the byte counters of an open tunnel move).
    /// </summary>
    public static class TunnelLogMapper
    {
        public static ProxyTunnelLogVM Map(ProxyTunnelLogState s) => new()
        {
            Id = s.LogId,
            TunnelId = s.TunnelId,
            StartAt = s.StartedAt,
            EndAt = s.EndAt,
            Outcome = s.Outcome,
            ClientIPAddress = s.ClientIPAddress,
            ClientPort = s.ClientPort,
            ServerIPAddress = s.ServerIPAddress,
            ServerPort = s.ServerPort,
            ClientProtocol = s.ClientProtocol,
            RejectReason = s.RejectReason,
            ErrorMessage = s.ErrorMessage,
            TargetHost = s.TargetHost,
            TargetPort = s.TargetPort,
            AuthMethod = s.AuthMethod,
            AuthUserName = s.AuthUserName,
            AuthPassword = s.AuthPassword,
            RoutingDecision = s.RoutingDecision,
            MatchedFilterType = s.MatchedFilterType,
            MatchedFilterPattern = s.MatchedFilterPattern,
            MatchedGroupId = s.MatchedGroupId,
            MatchedGroupName = s.MatchedGroupName,
            PickedSourceId = s.PickedSourceId,
            PickedSourceAddress = s.PickedSourceAddress,
            PickedSourcePort = s.PickedSourcePort,
            PickedSourceProxyType = s.PickedSourceProxyType,
            PickedSourceUserName = s.PickedSourceUserName,
            TotalBytesUpload = s.TotalBytesUpload,
            TotalBytesDownload = s.TotalBytesDownload,
        };
    }
}
