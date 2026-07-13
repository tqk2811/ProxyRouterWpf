namespace ProxyRouterWpf.Bandwidth
{
    /// <summary>Ring buffer of the most recent bandwidth samples (sliding window).</summary>
    public sealed class NetworkBandwidthCache
    {
        readonly object _lock = new();
        readonly NetworkBandwidthSnapshot[] _ring;
        int _count;
        int _head;

        public NetworkBandwidthCache(int historySize = 120)
        {
            _ring = new NetworkBandwidthSnapshot[Math.Max(10, historySize)];
        }

        public NetworkBandwidthSnapshot? Current
        {
            get
            {
                lock (_lock)
                {
                    if (_count == 0) return null;
                    int last = (_head - 1 + _ring.Length) % _ring.Length;
                    return _ring[last];
                }
            }
        }

        public void Add(NetworkBandwidthSnapshot snapshot)
        {
            lock (_lock)
            {
                _ring[_head] = snapshot;
                _head = (_head + 1) % _ring.Length;
                if (_count < _ring.Length) _count++;
            }
        }

        public IReadOnlyList<NetworkBandwidthSnapshot> GetRecent()
        {
            lock (_lock)
            {
                var result = new List<NetworkBandwidthSnapshot>(_count);
                int start = (_head - _count + _ring.Length) % _ring.Length;
                for (int i = 0; i < _count; i++)
                    result.Add(_ring[(start + i) % _ring.Length]);
                return result;
            }
        }
    }
}
