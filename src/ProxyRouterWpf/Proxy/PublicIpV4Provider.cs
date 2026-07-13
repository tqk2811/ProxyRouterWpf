using System.Net;
using System.Net.Http;

namespace ProxyRouterWpf.Proxy
{
    /// <summary>
    /// Best-effort public IPv4 lookup (used to build the copy-to-clipboard proxy endpoint list).
    /// Cached for a few minutes; returns null when offline.
    /// </summary>
    public sealed class PublicIpV4Provider
    {
        static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };

        readonly object _sync = new();
        string? _cached;
        DateTime _cachedAtUtc;
        static readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

        public async Task<string?> GetAsync(CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                if (_cached != null && DateTime.UtcNow - _cachedAtUtc < _ttl)
                    return _cached;
            }

            try
            {
                var text = (await _http.GetStringAsync("https://api.ipify.org", cancellationToken).ConfigureAwait(false)).Trim();
                if (IPAddress.TryParse(text, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    lock (_sync)
                    {
                        _cached = text;
                        _cachedAtUtc = DateTime.UtcNow;
                    }
                    return text;
                }
            }
            catch
            {
                // Offline / blocked — leave cache as-is.
            }
            lock (_sync)
                return _cached;
        }
    }
}
