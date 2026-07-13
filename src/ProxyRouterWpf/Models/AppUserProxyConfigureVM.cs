using System.ComponentModel.DataAnnotations;

namespace ProxyRouterWpf.Models
{
    public class AppUserProxyConfigureVM
    {
        public int StartPort { get; set; } = 30000;
        public string? ProxyUserName { get; set; }
        public string? ProxyPassword { get; set; }
        public string? ProxySocks4UserId { get; set; }

        public bool IsEnableProxy { get; set; }
        public bool IsHttpEnabled { get; set; }
        public bool IsSocks4Enabled { get; set; }
        public bool IsSocks5Enabled { get; set; }
    }

    public class UpdateProxyConfigureVM
    {
        [Range(10000, 65535)]
        public int StartPort { get; set; } = 30000;

        [MaxLength(128)]
        public string? ProxyUserName { get; set; }

        [MaxLength(256)]
        public string? ProxyPassword { get; set; }

        [MaxLength(128)]
        public string? ProxySocks4UserId { get; set; }

        public bool IsHttpEnabled { get; set; }
        public bool IsSocks4Enabled { get; set; }
        public bool IsSocks5Enabled { get; set; }
    }

    public class ToggleProxyEnableVM
    {
        public bool IsEnableProxy { get; set; }
    }

    public class ProxyRuntimeStatusVM
    {
        /// <summary>ProxySourceId of currently running ProxySession(s) (empty when stopped).</summary>
        public List<Guid> ActiveSourceIds { get; set; } = new();

        /// <summary>Public IPv4 of this machine (null if not detected).</summary>
        public string? PublicIpV4 { get; set; }
    }
}
