namespace ProxyRouterWpf.Enums
{
    public enum ProxyTunnelOutcome
    {
        /// <summary>Tunnel still open — the row is a live snapshot, not a committed log yet.</summary>
        Active = 0,
        ClientRejected = 1,
        RequestFailed = 2,
        AuthRejected = 3,
        RouteFailed = 4,
        Resolved = 5,
    }
}
