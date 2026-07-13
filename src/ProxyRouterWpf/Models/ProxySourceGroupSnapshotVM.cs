using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Models
{
    public class ProxySourceGroupSnapshotVM
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public ProxySourceGroupMatchMode MatchMode { get; set; } = ProxySourceGroupMatchMode.Or;
        public List<ProxySourceGroupFilterVM> Filters { get; set; } = new();
    }
}
