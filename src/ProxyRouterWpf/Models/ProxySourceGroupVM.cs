using System.ComponentModel.DataAnnotations;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Models
{
    public class ProxySourceGroupVM
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Priority { get; set; }
        public ProxySourceGroupMatchMode MatchMode { get; set; } = ProxySourceGroupMatchMode.Or;
    }

    public class CreateProxySourceGroupVM
    {
        [Required, StringLength(128)]
        public string Name { get; set; } = string.Empty;

        public ProxySourceGroupMatchMode MatchMode { get; set; } = ProxySourceGroupMatchMode.Or;
    }

    public class UpdateProxySourceGroupVM
    {
        public Guid Id { get; set; }

        [Required, StringLength(128)]
        public string Name { get; set; } = string.Empty;

        public ProxySourceGroupMatchMode MatchMode { get; set; } = ProxySourceGroupMatchMode.Or;
    }

    public class UpdateProxySourceGroupMatchModeVM
    {
        public Guid Id { get; set; }
        public ProxySourceGroupMatchMode MatchMode { get; set; }
    }

    public class DeleteProxySourceGroupVM
    {
        public Guid Id { get; set; }
    }

    public class ReorderProxySourceGroupVM
    {
        [Required]
        public List<Guid> Ids { get; set; } = new();
    }
}
