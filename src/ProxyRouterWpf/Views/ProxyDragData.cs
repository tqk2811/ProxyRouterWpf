namespace ProxyRouterWpf.Views
{
    /// <summary>
    /// Payload carried by a drag operation on the Proxies screen. Drags are intra-process, so the
    /// live CLR object travels inside the <see cref="System.Windows.DataObject"/> unchanged.
    /// </summary>
    public sealed class ProxyDragData
    {
        /// <summary>Custom clipboard format key used to tag/retrieve this payload.</summary>
        public const string Format = "ProxyRouterProxyDrag";

        public ProxyDragData(DragKind kind, IReadOnlyList<Guid> ids, Guid? sourceGroupId)
        {
            Kind = kind;
            Ids = ids;
            SourceGroupId = sourceGroupId;
        }

        /// <summary>Which grid the drag started from.</summary>
        public DragKind Kind { get; }

        /// <summary>The dragged row ids (proxy source ids, or a single group id).</summary>
        public IReadOnlyList<Guid> Ids { get; }

        /// <summary>Origin group of a <see cref="DragKind.GroupSource"/> drag; null otherwise.</summary>
        public Guid? SourceGroupId { get; }
    }
}
