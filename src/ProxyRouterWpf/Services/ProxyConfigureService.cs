using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public class ProxyConfigureService : IProxyConfigureService
    {
        readonly ConfigStore _store;

        public ProxyConfigureService(ConfigStore store)
        {
            _store = store;
        }

        public AppUserProxyConfigureVM Get()
        {
            lock (_store.SyncRoot)
            {
                var c = _store.Config.Configure;
                // Return a copy so the engine/UI reads a stable snapshot.
                return new AppUserProxyConfigureVM
                {
                    StartPort = c.StartPort,
                    ProxyUserName = c.ProxyUserName,
                    ProxyPassword = c.ProxyPassword,
                    ProxySocks4UserId = c.ProxySocks4UserId,
                    IsEnableProxy = c.IsEnableProxy,
                    IsHttpEnabled = c.IsHttpEnabled,
                    IsSocks4Enabled = c.IsSocks4Enabled,
                    IsSocks5Enabled = c.IsSocks5Enabled,
                };
            }
        }

        public void Update(UpdateProxyConfigureVM model)
        {
            lock (_store.SyncRoot)
            {
                var c = _store.Config.Configure;
                c.StartPort = model.StartPort;
                c.ProxyUserName = string.IsNullOrWhiteSpace(model.ProxyUserName) ? null : model.ProxyUserName.Trim();
                c.ProxyPassword = string.IsNullOrWhiteSpace(model.ProxyPassword) ? null : model.ProxyPassword;
                c.ProxySocks4UserId = string.IsNullOrWhiteSpace(model.ProxySocks4UserId) ? null : model.ProxySocks4UserId.Trim();
                c.IsHttpEnabled = model.IsHttpEnabled;
                c.IsSocks4Enabled = model.IsSocks4Enabled;
                c.IsSocks5Enabled = model.IsSocks5Enabled;
                _store.Save();
            }
        }

        public void SetEnabled(bool enabled)
        {
            lock (_store.SyncRoot)
            {
                _store.Config.Configure.IsEnableProxy = enabled;
                _store.Save();
            }
        }
    }
}
