using System.Collections.Concurrent;
using System.Net;
using System.Threading.Channels;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    /// <summary>
    /// In-memory tunnel event pipeline (DB-free). A <see cref="ConcurrentDictionary"/> holds open
    /// tunnel states; committed states flow through a bounded <see cref="Channel"/> (FIFO) to the
    /// consumer that pushes them into the RAM log store. Unchanged from the original ASP.NET port.
    /// </summary>
    public class ProxyEventLogService : IProxyEventLogService
    {
        readonly ConcurrentDictionary<Guid, ProxyTunnelLogState> _states = new();
        readonly Channel<ProxyTunnelLogState> _channel;
        long _droppedCount;

        public ProxyEventLogService(int capacity = 100_000)
        {
            _channel = Channel.CreateBounded<ProxyTunnelLogState>(new BoundedChannelOptions(Math.Max(1000, capacity))
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false,
            });
        }

        public ChannelReader<ProxyTunnelLogState> Reader => _channel.Reader;

        public void TrackTunnelStart(Guid tunnelId, IPEndPoint clientEndPoint, IPEndPoint? serverEndPoint)
        {
            var now = DateTime.UtcNow;
            var state = new ProxyTunnelLogState
            {
                TunnelId = tunnelId,
                ClientIPAddress = clientEndPoint.Address.ToString(),
                ClientPort = clientEndPoint.Port,
                ServerIPAddress = serverEndPoint?.Address.ToString(),
                ServerPort = serverEndPoint?.Port ?? 0,
                StartedAt = now,
                LastTouchedAt = now,
            };
            _states[tunnelId] = state;
        }

        public ProxyTunnelLogState? GetState(Guid tunnelId)
        {
            if (_states.TryGetValue(tunnelId, out var s))
            {
                s.LastTouchedAt = DateTime.UtcNow;
                return s;
            }
            return null;
        }

        public void Commit(Guid tunnelId, ProxyTunnelOutcome outcome, ProxyTunnelRejectReason? rejectReason = null, string? errorMessage = null)
        {
            if (!_states.TryRemove(tunnelId, out var state))
                return;

            state.Outcome = outcome;
            state.RejectReason = rejectReason;
            state.ErrorMessage = errorMessage;
            state.EndAt = DateTime.UtcNow;
            if (!_channel.Writer.TryWrite(state))
                Interlocked.Increment(ref _droppedCount);
        }

        public void Discard(Guid tunnelId) => _states.TryRemove(tunnelId, out _);

        public long ConsumeDroppedCount() => Interlocked.Exchange(ref _droppedCount, 0);

        public int CleanupAbandoned(TimeSpan ttl)
        {
            var threshold = DateTime.UtcNow - ttl;
            int removed = 0;
            foreach (var kvp in _states)
            {
                if (kvp.Value.LastTouchedAt < threshold && _states.TryRemove(kvp.Key, out _))
                    removed++;
            }
            return removed;
        }
    }
}
