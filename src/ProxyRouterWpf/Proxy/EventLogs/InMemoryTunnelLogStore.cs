using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    /// <summary>
    /// Bounded in-memory FIFO store of committed tunnel logs. Replaces the SQL <c>ProxyTunnelLogs</c>
    /// table: when the row count exceeds <see cref="Capacity"/>, the oldest rows are dropped first.
    /// Each added row gets a monotonically increasing <see cref="ProxyTunnelLogVM.Id"/>.
    /// <para>
    /// It also holds the <b>live</b> tunnels (still-open connections, registered at
    /// <c>TrackTunnelStart</c>) so a connection shows up in the Logs tab immediately with the
    /// "Active" status instead of only when it closes. Live entries keep the state object itself, so
    /// every <see cref="Snapshot"/> re-projects the current byte counters.
    /// </para>
    /// </summary>
    public sealed class InMemoryTunnelLogStore
    {
        readonly object _sync = new();
        readonly List<ProxyTunnelLogVM> _items = new();
        readonly Dictionary<Guid, ProxyTunnelLogState> _live = new();
        long _nextId;
        int _capacity;

        public InMemoryTunnelLogStore(int capacity)
        {
            _capacity = Math.Max(100, capacity);
        }

        /// <summary>Raised (possibly on a background thread) whenever the store changes.</summary>
        public event Action? Changed;

        public int Capacity
        {
            get { lock (_sync) return _capacity; }
        }

        public void SetCapacity(int capacity)
        {
            capacity = Math.Max(100, capacity);
            lock (_sync)
            {
                _capacity = capacity;
                Trim();
            }
            Changed?.Invoke();
        }

        /// <summary>
        /// Registers a just-accepted connection as a live row and reserves its log id, so the row
        /// keeps the same identity once it is committed.
        /// </summary>
        public long AddLive(ProxyTunnelLogState state)
        {
            long id;
            lock (_sync)
            {
                id = ++_nextId;
                state.LogId = id;
                _live[state.TunnelId] = state;
            }
            Changed?.Invoke();
            return id;
        }

        /// <summary>Drops a live row that will never be committed (discarded / abandoned / channel full).</summary>
        public bool RemoveLive(Guid tunnelId)
        {
            bool removed;
            lock (_sync)
                removed = _live.Remove(tunnelId);
            if (removed) Changed?.Invoke();
            return removed;
        }

        /// <summary>
        /// Adds a committed row, replacing its live counterpart in the same lock so a snapshot can
        /// never see the tunnel twice (nor miss it in between).
        /// </summary>
        public long Add(ProxyTunnelLogVM vm)
        {
            long id;
            lock (_sync)
            {
                _live.Remove(vm.TunnelId);
                // Id was reserved by AddLive; only fall back for rows that never had a live phase.
                id = vm.Id > 0 ? vm.Id : ++_nextId;
                vm.Id = id;
                _items.Add(vm);
                Trim();
            }
            Changed?.Invoke();
            return id;
        }

        void Trim()
        {
            int overflow = _items.Count - _capacity;
            if (overflow > 0)
                _items.RemoveRange(0, overflow);
        }

        /// <summary>Committed rows plus a fresh projection of every open tunnel.</summary>
        public IReadOnlyList<ProxyTunnelLogVM> Snapshot()
        {
            lock (_sync)
            {
                var list = new List<ProxyTunnelLogVM>(_items.Count + _live.Count);
                list.AddRange(_items);
                foreach (var state in _live.Values)
                    list.Add(TunnelLogMapper.Map(state));
                return list;
            }
        }

        public ProxyTunnelLogVM? GetById(long id)
        {
            lock (_sync)
            {
                var found = _items.FirstOrDefault(x => x.Id == id);
                if (found != null) return found;
                foreach (var state in _live.Values)
                {
                    if (state.LogId == id) return TunnelLogMapper.Map(state);
                }
                return null;
            }
        }

        /// <summary>Committed rows + open tunnels.</summary>
        public int Count
        {
            get { lock (_sync) return _items.Count + _live.Count; }
        }

        /// <summary>Clears the log history. Open tunnels stay listed — they are not history yet.</summary>
        public void Clear()
        {
            lock (_sync)
                _items.Clear();
            Changed?.Invoke();
        }
    }
}
