using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProxyRouterWpf.Bandwidth;
using ProxyRouterWpf.Proxy;
using ProxyRouterWpf.Proxy.EventLogs;
using ProxyRouterWpf.Services;

namespace ProxyRouterWpf.Configuration
{
    /// <summary>
    /// Composition root: builds and owns every singleton and wires them together. Background workers
    /// (log consumer, bandwidth sampler) are started here, but the proxy engine is intentionally NOT
    /// started — the user must enable it from the Proxies tab (no auto-start on launch).
    /// </summary>
    public sealed class AppServices : IAsyncDisposable
    {
        public ConfigStore Config { get; }
        public ILoggerFactory LoggerFactory { get; }

        public IProxyConfigureService Configure { get; }
        public IProxySourceService Sources { get; }
        public IProxySourceGroupService Groups { get; }
        public IProxySourceGroupFilterService Filters { get; }

        public ProxyEventLogService EventLog { get; }
        public InMemoryTunnelLogStore LogStore { get; }
        public IProxyHostTrafficCache TrafficCache { get; }
        public IProxyTunnelLogService TunnelLogs { get; }
        public TunnelLogChannelConsumer LogConsumer { get; }

        public ProxiesHostedManager Manager { get; }
        public LocalIpV4Provider LocalIp { get; }

        public NetworkBandwidthCache BandwidthCache { get; }
        public NetworkBandwidthSampler BandwidthSampler { get; }

        public AppServices()
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "proxyrouter.config.json");
            Config = new ConfigStore(configPath);
            LoggerFactory = NullLoggerFactory.Instance;

            Configure = new ProxyConfigureService(Config);
            Sources = new ProxySourceService(Config);
            Groups = new ProxySourceGroupService(Config);
            Filters = new ProxySourceGroupFilterService(Config);

            EventLog = new ProxyEventLogService();
            LogStore = new InMemoryTunnelLogStore(Config.Config.Settings.LogCapacity);
            TrafficCache = new ProxyHostTrafficCache(LogStore);
            TunnelLogs = new ProxyTunnelLogService(LogStore);
            LogConsumer = new TunnelLogChannelConsumer(EventLog, LogStore, TrafficCache);

            LocalIp = new LocalIpV4Provider();
            Manager = new ProxiesHostedManager(Sources, Configure, Filters, TrafficCache, EventLog, LoggerFactory);

            BandwidthCache = new NetworkBandwidthCache(60);
            BandwidthSampler = new NetworkBandwidthSampler(BandwidthCache, 1000);
        }

        /// <summary>Starts background workers. Does NOT start the proxy engine.</summary>
        public void StartBackground()
        {
            LogConsumer.Start();
            BandwidthSampler.Start();
        }

        public async ValueTask DisposeAsync()
        {
            Manager.Dispose();
            await BandwidthSampler.DisposeAsync().ConfigureAwait(false);
            await LogConsumer.DisposeAsync().ConfigureAwait(false);
            Config.Save();
        }
    }
}
