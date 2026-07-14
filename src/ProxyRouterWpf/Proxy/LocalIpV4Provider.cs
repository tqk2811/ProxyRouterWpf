using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ProxyRouterWpf.Proxy
{
    /// <summary>
    /// Enumerates the IPv4 addresses bound to this machine (no internet call). Because the proxy
    /// listeners bind to <c>0.0.0.0</c>, they are reachable on every one of these addresses — e.g.
    /// <c>192.168.1.5</c>, <c>127.0.0.1</c>, or a VPS public IP on the NIC. The address the OS would
    /// route outbound through is listed first (best default for building the copy-to-clipboard list).
    /// </summary>
    public sealed class LocalIpV4Provider
    {
        /// <summary>Primary/outbound IPv4 (first entry of <see cref="GetAll"/>), or null if none.</summary>
        public string? Get() => GetAll().FirstOrDefault();

        public IReadOnlyList<string> GetAll()
        {
            var result = new List<string>();
            var seen = new HashSet<string>();

            void Add(string? ip)
            {
                if (!string.IsNullOrEmpty(ip) && seen.Add(ip))
                    result.Add(ip);
            }

            // Outbound-route source IP first (public IP on a VPS, LAN IP behind NAT).
            Add(FromOutboundRoute());

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        var ip = ua.Address;
                        if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IsLinkLocal(ip)) continue; // skip 169.254.x.x noise
                        Add(ip.ToString());
                    }
                }
            }
            catch
            {
                // Ignore enumeration failures.
            }

            Add("127.0.0.1"); // always offer loopback
            return result;
        }

        static string? FromOutboundRoute()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 65530); // UDP connect sends nothing; just selects the route
                if (socket.LocalEndPoint is IPEndPoint ep
                    && ep.AddressFamily == AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(ep.Address)
                    && !IsLinkLocal(ep.Address))
                {
                    return ep.Address.ToString();
                }
            }
            catch
            {
                // No default route — skip.
            }
            return null;
        }

        static bool IsLinkLocal(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return b.Length == 4 && b[0] == 169 && b[1] == 254; // 169.254.0.0/16
        }
    }
}
