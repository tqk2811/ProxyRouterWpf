using System.Management;
using System.Runtime.Versioning;

namespace ProxyRouterWpf.Bandwidth
{
    /// <summary>
    /// Background WMI sampler (Windows only). Reads cumulative NIC byte counters from
    /// Win32_PerfRawData_Tcpip_NetworkInterface and derives per-second rates from the delta between
    /// samples (same approach as Task Manager). Pushes snapshots into <see cref="NetworkBandwidthCache"/>.
    /// </summary>
    public sealed class NetworkBandwidthSampler : IAsyncDisposable
    {
        readonly NetworkBandwidthCache _cache;
        readonly int _intervalMs;
        readonly CancellationTokenSource _cts = new();
        Task? _task;

        public NetworkBandwidthSampler(NetworkBandwidthCache cache, int sampleIntervalMs = 1000)
        {
            _cache = cache;
            _intervalMs = Math.Max(250, sampleIntervalMs);
        }

        public void Start()
        {
            if (!OperatingSystem.IsWindows())
                return;
            _task = Task.Run(() => LoopAsync(_cts.Token));
        }

        [SupportedOSPlatform("windows")]
        async Task LoopAsync(CancellationToken ct)
        {
            var interval = TimeSpan.FromMilliseconds(_intervalMs);
            _cache.Add(new NetworkBandwidthSnapshot(0, 0, DateTime.UtcNow));

            long prevRx = 0, prevTx = 0;
            DateTime prevAt = default;
            bool hasPrev = false;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var (rxTotal, txTotal) = QueryCumulative();
                    var now = DateTime.UtcNow;

                    if (hasPrev)
                    {
                        double seconds = (now - prevAt).TotalSeconds;
                        if (seconds > 0)
                        {
                            long rxRate = (long)Math.Max(0d, (rxTotal - prevRx) / seconds);
                            long txRate = (long)Math.Max(0d, (txTotal - prevTx) / seconds);
                            _cache.Add(new NetworkBandwidthSnapshot(rxRate, txRate, now));
                        }
                    }

                    prevRx = rxTotal;
                    prevTx = txTotal;
                    prevAt = now;
                    hasPrev = true;
                }
                catch
                {
                    // WMI hiccup — skip this sample.
                }

                try { await Task.Delay(interval, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
        }

        [SupportedOSPlatform("windows")]
        static (long rxTotal, long txTotal) QueryCumulative()
        {
            long rx = 0, tx = 0;
            using var searcher = new ManagementObjectSearcher(
                "root\\CIMV2",
                "SELECT Name, BytesReceivedPersec, BytesSentPersec FROM Win32_PerfRawData_Tcpip_NetworkInterface");
            using var results = searcher.Get();
            foreach (ManagementBaseObject mo in results)
            {
                using (mo)
                {
                    var name = mo["Name"] as string ?? string.Empty;
                    if (IsPseudoInterface(name)) continue;
                    rx += ToLong(mo["BytesReceivedPersec"]);
                    tx += ToLong(mo["BytesSentPersec"]);
                }
            }
            return (rx, tx);
        }

        static long ToLong(object? value) => value is null ? 0L : Convert.ToInt64(value);

        static bool IsPseudoInterface(string name)
        {
            if (string.IsNullOrEmpty(name)) return true;
            return name.Contains("Loopback", StringComparison.OrdinalIgnoreCase)
                || name.Contains("isatap", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Teredo", StringComparison.OrdinalIgnoreCase);
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try { if (_task != null) await _task.ConfigureAwait(false); }
            catch { /* ignore */ }
            _cts.Dispose();
        }
    }
}
