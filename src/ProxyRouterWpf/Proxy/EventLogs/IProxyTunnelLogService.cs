using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    public interface IProxyTunnelLogService
    {
        ProxyTunnelLogListResponseVM List(ProxyTunnelLogListRequestVM request);
        ProxyTunnelLogVM? GetById(long id);
        int Count();
        void Clear();
    }
}
