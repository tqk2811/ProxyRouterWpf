namespace ProxyRouterWpf.Enums
{
    public enum ProxyTunnelRejectReason
    {
        IpNotInWhitelist = 1,

        EmptyRequest = 10,
        Socks4Disabled = 11,
        Socks5Disabled = 12,
        HttpDisabled = 13,
        UnknownProtocol = 14,
        PreReadFailed = 15,

        Socks4UserIdMismatch = 20,
        Socks5GreetingMissingUsernamePassword = 21,
        Socks5GreetingNoAcceptableMethod = 22,
        CredentialMismatch = 23,
        MissingCredential = 24,

        ProxyTypeUnsupported = 30,
        UpstreamConnectFailed = 31,
        TunnelAborted = 32,
    }
}
