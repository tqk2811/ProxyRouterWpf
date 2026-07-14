using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ProxyRouterWpf.Proxy
{
    /// <summary>
    /// Resolves the machine's primary IPv4 from the local network (no internet call). Uses the
    /// outbound-routing trick — a UDP socket "connected" to a public address reports the source IP
    /// the OS would route through, without sending any packet. On a VPS whose NIC holds the public
    /// address, this returns that public IP; on a LAN it returns the private IP (e.g. 192.168.x.x).
    /// Falls back to enumerating active interfaces.
    /// </summary>
    public sealed class LocalIpV4Provider
    {
        public string? Get()
        {
            var routed = FromOutboundRoute();
            if (routed != null) return routed;
            return FromInterfaces();
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
                // No default route / socket unavailable — fall back to interface scan.
            }
            return null;
        }

        static string? FromInterfaces()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        var ip = ua.Address;
                        if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                        if (IPAddress.IsLoopback(ip) || IsLinkLocal(ip)) continue;
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                // Ignore — return null.
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
