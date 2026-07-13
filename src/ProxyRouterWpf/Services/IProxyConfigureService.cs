using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public interface IProxyConfigureService
    {
        AppUserProxyConfigureVM Get();
        void Update(UpdateProxyConfigureVM model);
        void SetEnabled(bool enabled);
    }
}
