using System.Collections.Concurrent;

namespace ProxyRouterWpf.Proxy.EventLogs
{
    // In-memory port. On a cache miss GetOrLoad computes the max from the RAM log store instead of
    // querying SQL. Note: if the FIFO store has evicted old rows the computed max may be lower than
    // the true historical peak, but TryUpdateMax keeps the running max alive across evictions.
    public class ProxyHostTrafficCache : IProxyHostTrafficCache
    {
        readonly ConcurrentDictionary<string, Snapshot> _store = new();
        readonly InMemoryTunnelLogStore _logStore;

        public ProxyHostTrafficCache(InMemoryTunnelLogStore logStore)
        {
            _logStore = logStore;
        }

        sealed class Snapshot
        {
            long _maxUpload;
            long _maxDownload;
            long _maxBoth;
            public long MaxUpload => Interlocked.Read(ref _maxUpload);
            public long MaxDownload => Interlocked.Read(ref _maxDownload);
            public long MaxBoth => Interlocked.Read(ref _maxBoth);
            public Snapshot(long up, long down, long both) { _maxUpload = up; _maxDownload = down; _maxBoth = both; }
            public void UpdateMax(long u, long d, long b)
            {
                InterlockedMax(ref _maxUpload, u);
                InterlockedMax(ref _maxDownload, d);
                InterlockedMax(ref _maxBoth, b);
            }
            static void InterlockedMax(ref long location, long value)
            {
                long initial;
                do
                {
                    initial = Interlocked.Read(ref location);
                    if (value <= initial) return;
                } while (Interlocked.CompareExchange(ref location, value, initial) != initial);
            }
        }

        static string MakeKey(string host) => host.ToLowerInvariant();

        public bool TryGet(string host, out long maxUpload, out long maxDownload, out long maxBoth)
        {
            if (string.IsNullOrEmpty(host)) { maxUpload = 0; maxDownload = 0; maxBoth = 0; return false; }
            if (_store.TryGetValue(MakeKey(host), out var snap))
            {
                maxUpload = snap.MaxUpload;
                maxDownload = snap.MaxDownload;
                maxBoth = snap.MaxBoth;
                return true;
            }
            maxUpload = 0; maxDownload = 0; maxBoth = 0;
            return false;
        }

        public void Set(string host, long maxUpload, long maxDownload, long maxBoth)
        {
            if (string.IsNullOrEmpty(host)) return;
            _store.TryAdd(MakeKey(host), new Snapshot(maxUpload, maxDownload, maxBoth));
        }

        public bool TryUpdateMax(string host, long upload, long download, long both)
        {
            if (string.IsNullOrEmpty(host)) return false;
            if (upload <= 0 && download <= 0 && both <= 0) return false;
            if (_store.TryGetValue(MakeKey(host), out var snap))
            {
                snap.UpdateMax(upload, download, both);
                return true;
            }
            return false;
        }

        public (long maxUpload, long maxDownload, long maxBoth) GetOrLoad(string host)
        {
            if (string.IsNullOrEmpty(host)) return (0L, 0L, 0L);
            if (TryGet(host, out var up, out var down, out var both))
                return (up, down, both);

            long maxUp = 0, maxDown = 0, maxBoth = 0;
            foreach (var log in _logStore.Snapshot())
            {
                if (!string.Equals(log.TargetHost, host, StringComparison.OrdinalIgnoreCase)) continue;
                if (log.TotalBytesUpload > maxUp) maxUp = log.TotalBytesUpload;
                if (log.TotalBytesDownload > maxDown) maxDown = log.TotalBytesDownload;
                long sum = log.TotalBytesUpload + log.TotalBytesDownload;
                if (sum > maxBoth) maxBoth = sum;
            }
            Set(host, maxUp, maxDown, maxBoth);
            return (maxUp, maxDown, maxBoth);
        }
    }
}
