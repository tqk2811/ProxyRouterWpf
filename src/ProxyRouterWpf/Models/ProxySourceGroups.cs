namespace ProxyRouterWpf.Models
{
    /// <summary>Well-known group ids that are not stored in <c>AppConfig.Groups</c>.</summary>
    public static class ProxySourceGroups
    {
        /// <summary>
        /// Sentinel group for host proxies: every source in this group gets its own local listener
        /// on <c>StartPort + i</c>. It is deliberately absent from the routing-group list, so it can
        /// never be renamed, reordered, filtered or deleted from the Groups grid.
        /// </summary>
        public static readonly Guid HostGroupId = new("00000000-0000-0000-0000-000000000001");
    }
}
