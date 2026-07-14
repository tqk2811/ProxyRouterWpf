using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Configuration
{
    /// <summary>
    /// Root persisted configuration (single-user desktop). Serialized to a JSON file next to the
    /// executable. Replaces the SQL database of the original ASP.NET ProxyRouter. Tunnel logs are
    /// NOT persisted here — they live only in a bounded in-memory FIFO store.
    /// </summary>
    public class AppConfig
    {
        public AppUserProxyConfigureVM Configure { get; set; } = new();
        public List<ProxySourceVM> Sources { get; set; } = new();
        public List<ProxySourceGroupVM> Groups { get; set; } = new();
        public List<ProxySourceGroupFilterVM> Filters { get; set; } = new();
        public AppSettings Settings { get; set; } = new();
    }

    public class AppSettings
    {
        /// <summary>"System" | "Light" | "Dark".</summary>
        public string Theme { get; set; } = "System";

        /// <summary>Max number of tunnel log rows kept in RAM (FIFO, oldest dropped first).</summary>
        public int LogCapacity { get; set; } = 5000;

        /// <summary>Proxy output copy format: "http_socks5" | "socks4".</summary>
        public string ProxyOutputFormat { get; set; } = "http_socks5";
    }
}
