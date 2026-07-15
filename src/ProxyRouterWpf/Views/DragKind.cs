namespace ProxyRouterWpf.Views
{
    /// <summary>Identifies which grid a drag-drop payload originated from.</summary>
    public enum DragKind
    {
        /// <summary>A proxy row dragged from the ungrouped grid.</summary>
        UngroupedSource,
        /// <summary>A proxy row dragged from a group's source list.</summary>
        GroupSource,
        /// <summary>A group row dragged from the groups grid.</summary>
        Group,
    }
}
