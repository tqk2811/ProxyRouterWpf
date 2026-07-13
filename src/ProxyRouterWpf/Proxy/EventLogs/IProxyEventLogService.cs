using System.Net;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    public interface IProxyEventLogService
    {
        void TrackTunnelStart(Guid tunnelId, IPEndPoint clientEndPoint, IPEndPoint? serverEndPoint);
        ProxyTunnelLogState? GetState(Guid tunnelId);
        void Commit(Guid tunnelId, ProxyTunnelOutcome outcome, ProxyTunnelRejectReason? rejectReason = null, string? errorMessage = null);
        void Discard(Guid tunnelId);
    }
}
