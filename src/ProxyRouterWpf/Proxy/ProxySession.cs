using System.Net;
using Microsoft.Extensions.Logging;
using ProxyRouterWpf.Models;
using ProxyRouterWpf.Proxy.EventLogs;
using ProxyRouterWpf.Services;
using TqkLibrary.Proxy;

namespace ProxyRouterWpf.Proxy
{
    /// <summary>
    /// One TCP proxy listener on a single port, bound to one default upstream <see cref="ProxySourceVM"/>.
    /// Protocol (HTTP/SOCKS4/SOCKS5) is auto-detected per connection; upstream selection can be
    /// overridden per request by the group/filter routing rules. Single-user port of the ASP.NET
    /// ProxySession (no userId, no DI scope, services injected directly).
    /// </summary>
    public partial class ProxySession : IDisposable
    {
        public ProxySourceVM ProxySourceVM { get; }
        public IPEndPoint ListenEndpoint { get; }

        readonly ILoggerFactory _loggerFactory;
        readonly ILogger<ProxySession> _logger;
        readonly ProxyServer _proxyServer;
        readonly IProxyEventLogService _logService;

        readonly IProxySourceService _sourceService;
        readonly IProxyConfigureService _configureService;
        readonly IProxySourceGroupFilterService _filterService;
        readonly IProxyHostTrafficCache _trafficCache;

        public ProxySession(
            ProxySourceVM proxySourceVM,
            IPEndPoint listenEndpoint,
            IProxySourceService sourceService,
            IProxyConfigureService configureService,
            IProxySourceGroupFilterService filterService,
            IProxyHostTrafficCache trafficCache,
            IProxyEventLogService logService,
            ILoggerFactory loggerFactory)
        {
            ProxySourceVM = proxySourceVM;
            ListenEndpoint = listenEndpoint;
            _sourceService = sourceService;
            _configureService = configureService;
            _filterService = filterService;
            _trafficCache = trafficCache;
            _logService = logService;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ProxySession>();

            _proxyServer = new ProxyServer(listenEndpoint, _loggerFactory);
            _proxyServer.PreProxyServerHandler = new MyPreProxyServerHandler(this);
            _proxyServer.ProxyServerHandler = new MyProxyServerHandler(this, proxySourceVM);

            _logger.LogInformation(
                "ProxySession created SourceId={SourceId}, SourceAddress={Address}:{Port}, ListenEndpoint={Listen}",
                proxySourceVM.Id, proxySourceVM.Address, proxySourceVM.Port, listenEndpoint);
        }

        ~ProxySession()
        {
            _proxyServer.Dispose();
        }

        public void Dispose()
        {
            _logger.LogInformation("ProxySession disposing SourceId={SourceId}, ListenEndpoint={Listen}",
                ProxySourceVM.Id, ListenEndpoint);
            _proxyServer.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            _proxyServer.StartListen();
            _logger.LogInformation("ProxySession started SourceId={SourceId}, ListenEndpoint={Listen}",
                ProxySourceVM.Id, ListenEndpoint);
        }

        public void Stop()
        {
            _proxyServer.StopListen();
            _proxyServer.ShutdownCurrentConnection();
            _logger.LogInformation("ProxySession stopped SourceId={SourceId}, ListenEndpoint={Listen}",
                ProxySourceVM.Id, ListenEndpoint);
        }
    }
}
