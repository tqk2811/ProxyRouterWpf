namespace ProxyRouterWpf.Views.Dialogs
{
    /// <summary>Group picker item for combo boxes (Ungrouped = null Id).</summary>
    public sealed class GroupChoice
    {
        public string Label { get; init; } = string.Empty;
        public Guid? Id { get; init; }
    }
}
