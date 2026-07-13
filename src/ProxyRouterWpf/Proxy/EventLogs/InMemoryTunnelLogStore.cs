using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    /// <summary>
    /// Bounded in-memory FIFO store of committed tunnel logs. Replaces the SQL <c>ProxyTunnelLogs</c>
    /// table: when the row count exceeds <see cref="Capacity"/>, the oldest rows are dropped first.
    /// Each added row gets a monotonically increasing <see cref="ProxyTunnelLogVM.Id"/>.
    /// </summary>
    public sealed class InMemoryTunnelLogStore
    {
        readonly object _sync = new();
        readonly List<ProxyTunnelLogVM> _items = new();
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

        public long Add(ProxyTunnelLogVM vm)
        {
            long id;
            lock (_sync)
            {
                id = ++_nextId;
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

        public IReadOnlyList<ProxyTunnelLogVM> Snapshot()
        {
            lock (_sync)
                return _items.ToList();
        }

        public ProxyTunnelLogVM? GetById(long id)
        {
            lock (_sync)
                return _items.FirstOrDefault(x => x.Id == id);
        }

        public int Count
        {
            get { lock (_sync) return _items.Count; }
        }

        public void Clear()
        {
            lock (_sync)
                _items.Clear();
            Changed?.Invoke();
        }
    }
}
