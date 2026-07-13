namespace ProxyRouterWpf.Proxy.EventLogs
{
    // Permanent cache (host) -> highest upload/download/both bytes seen in a single committed tunnel.
    // Used by the TotalBytes routing filter.
    public interface IProxyHostTrafficCache
    {
        bool TryGet(string host, out long maxUpload, out long maxDownload, out long maxBoth);
        void Set(string host, long maxUpload, long maxDownload, long maxBoth);
        bool TryUpdateMax(string host, long upload, long download, long both);
        (long maxUpload, long maxDownload, long maxBoth) GetOrLoad(string host);
    }
}
