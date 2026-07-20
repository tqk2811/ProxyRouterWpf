using System.Net;
using Microsoft.Extensions.Logging;
using ProxyRouterWpf.Models;
using ProxyRouterWpf.Proxy.EventLogs;
using ProxyRouterWpf.Services;

namespace ProxyRouterWpf.Proxy
{
    /// <summary>
    /// Owns the set of running <see cref="ProxySession"/> listeners. Single-user port: one listener
    /// per host proxy source (<see cref="ProxySourceGroups.HostGroupId"/>), on port
    /// <c>StartPort + i</c>. Never auto-starts — the UI
    /// must call <see cref="Start"/> explicitly.
    /// </summary>
    public sealed class ProxiesHostedManager : IDisposable
    {
        readonly IProxySourceService _sourceService;
        readonly IProxyConfigureService _configureService;
        readonly IProxySourceGroupFilterService _filterService;
        readonly IProxyHostTrafficCache _trafficCache;
        readonly IProxyEventLogService _eventLog;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger<ProxiesHostedManager> _logger;

        readonly object _sync = new();
        readonly List<ProxySession> _sessions = new();

        public ProxiesHostedManager(
            IProxySourceService sourceService,
            IProxyConfigureService configureService,
            IProxySourceGroupFilterService filterService,
            IProxyHostTrafficCache trafficCache,
            IProxyEventLogService eventLog,
            ILoggerFactory loggerFactory)
        {
            _sourceService = sourceService;
            _configureService = configureService;
            _filterService = filterService;
            _trafficCache = trafficCache;
            _eventLog = eventLog;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ProxiesHostedManager>();
        }

        /// <summary>Raised (on the calling thread) after Start/Stop changes the running state.</summary>
        public event Action? StateChanged;

        public bool IsRunning
        {
            get { lock (_sync) return _sessions.Count > 0; }
        }

        /// <summary>Number of listeners actually running.</summary>
        public int ActiveCount
        {
            get { lock (_sync) return _sessions.Count; }
        }

        /// <summary>
        /// Starts a listener for every host proxy source. Returns the number started. Requires
        /// at least one protocol enabled and at least one host source (both validated by caller).
        /// </summary>
        public int Start()
        {
            int started;
            lock (_sync)
            {
                if (_sessions.Count > 0)
                    return _sessions.Count; // already running

                var sources = _sourceService.ListByGroup(ProxySourceGroups.HostGroupId);
                var configure = _configureService.Get();
                var bindAddress = IPAddress.TryParse(configure.ListenAddress, out var parsed) ? parsed : IPAddress.Any;
                _logger.LogInformation("Start requested SourceCount={SourceCount}, StartPort={StartPort}, ListenAddress={ListenAddress}", sources.Count, configure.StartPort, bindAddress);

                started = 0;
                for (int i = 0; i < sources.Count; i++)
                {
                    var endpoint = new IPEndPoint(bindAddress, configure.StartPort + i);
                    try
                    {
                        var session = new ProxySession(
                            sources[i], endpoint,
                            _sourceService, _configureService, _filterService, _trafficCache, _eventLog, _loggerFactory);
                        session.Start();
                        _sessions.Add(session);
                        started++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Start failed for SourceId={SourceId}, Endpoint={Endpoint}", sources[i].Id, endpoint);
                    }
                }
                _logger.LogInformation("Start completed Started={Started}/{Total}", started, sources.Count);
            }
            _configureService.SetEnabled(started > 0);
            StateChanged?.Invoke();
            return started;
        }

        public void Stop()
        {
            int stopped;
            lock (_sync)
            {
                stopped = _sessions.Count;
                foreach (var s in _sessions)
                    s.Dispose();
                _sessions.Clear();
            }
            _configureService.SetEnabled(false);
            if (stopped > 0)
                _logger.LogInformation("Stop completed Stopped={Stopped}", stopped);
            StateChanged?.Invoke();
        }

        public IReadOnlyList<Guid> GetActiveSourceIds()
        {
            lock (_sync)
                return _sessions.Select(x => x.ProxySourceVM.Id).ToList();
        }

        public void Dispose()
        {
            lock (_sync)
            {
                foreach (var s in _sessions)
                    s.Dispose();
                _sessions.Clear();
            }
        }
    }
}
