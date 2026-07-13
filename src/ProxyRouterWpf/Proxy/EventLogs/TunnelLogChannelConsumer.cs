using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    /// <summary>
    /// Replaces the SQL <c>ProxyEventLogBackgroundService</c>: instead of SqlBulkCopy it drains the
    /// committed-tunnel channel into the RAM FIFO store and updates the host-traffic cache. Also
    /// periodically evicts abandoned (never-committed) tunnel states.
    /// </summary>
    public sealed class TunnelLogChannelConsumer : IAsyncDisposable
    {
        readonly ProxyEventLogService _eventLog;
        readonly InMemoryTunnelLogStore _store;
        readonly IProxyHostTrafficCache _trafficCache;
        readonly CancellationTokenSource _cts = new();
        Task? _consumeTask;
        Task? _cleanupTask;

        public TunnelLogChannelConsumer(
            ProxyEventLogService eventLog,
            InMemoryTunnelLogStore store,
            IProxyHostTrafficCache trafficCache)
        {
            _eventLog = eventLog;
            _store = store;
            _trafficCache = trafficCache;
        }

        public void Start()
        {
            _consumeTask = Task.Run(() => ConsumeLoopAsync(_cts.Token));
            _cleanupTask = Task.Run(() => CleanupLoopAsync(_cts.Token));
        }

        async Task ConsumeLoopAsync(CancellationToken ct)
        {
            var reader = _eventLog.Reader;
            try
            {
                while (await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var state))
                    {
                        var vm = Map(state);
                        _store.Add(vm);
                        if (!string.IsNullOrEmpty(vm.TargetHost))
                            _trafficCache.TryUpdateMax(vm.TargetHost!, vm.TotalBytesUpload, vm.TotalBytesDownload, vm.TotalBytesUpload + vm.TotalBytesDownload);
                    }
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }
        }

        async Task CleanupLoopAsync(CancellationToken ct)
        {
            try
            {
                using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    _eventLog.CleanupAbandoned(TimeSpan.FromMinutes(5));
                }
            }
            catch (OperationCanceledException) { /* shutting down */ }
        }

        static ProxyTunnelLogVM Map(ProxyTunnelLogState s) => new()
        {
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

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try
            {
                if (_consumeTask != null) await _consumeTask.ConfigureAwait(false);
                if (_cleanupTask != null) await _cleanupTask.ConfigureAwait(false);
            }
            catch { /* ignore shutdown races */ }
            _cts.Dispose();
        }
    }
}
