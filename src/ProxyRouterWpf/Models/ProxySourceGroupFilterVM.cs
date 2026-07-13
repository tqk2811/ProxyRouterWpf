using System.ComponentModel.DataAnnotations;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Models
{
    public class ProxySourceGroupFilterVM
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public ProxySourceGroupFilterType FilterType { get; set; }
        public ProxyTrafficDirection? TrafficDirection { get; set; }
        public string Filter { get; set; } = string.Empty;
        public bool IsNot { get; set; }
    }

    public class CreateProxySourceGroupFilterVM
    {
        public Guid GroupId { get; set; }
        public ProxySourceGroupFilterType FilterType { get; set; } = ProxySourceGroupFilterType.Wildcard;
        public ProxyTrafficDirection? TrafficDirection { get; set; }

        [Required, StringLength(256)]
        public string Filter { get; set; } = string.Empty;

        public bool IsNot { get; set; }
    }

    public class UpdateProxySourceGroupFilterVM
    {
        public Guid Id { get; set; }
        public ProxySourceGroupFilterType FilterType { get; set; }
        public ProxyTrafficDirection? TrafficDirection { get; set; }

        [Required, StringLength(256)]
        public string Filter { get; set; } = string.Empty;

        public bool IsNot { get; set; }
    }

    public class UpdateProxySourceGroupFilterIsNotVM
    {
        public Guid Id { get; set; }
        public bool IsNot { get; set; }
    }

    public class DeleteProxySourceGroupFilterVM
    {
        public Guid Id { get; set; }
    }

    public class BulkCreateProxySourceGroupFilterVM
    {
        public Guid GroupId { get; set; }
        public ProxySourceGroupFilterType FilterType { get; set; } = ProxySourceGroupFilterType.Wildcard;
        public ProxyTrafficDirection? TrafficDirection { get; set; }

        public bool IsNot { get; set; }

        [Required]
        public string Lines { get; set; } = string.Empty;
    }

    public class BulkCreateProxySourceGroupFilterResultVM
    {
        public int Created { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
