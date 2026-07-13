using System.ComponentModel.DataAnnotations;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Models
{
    public class ProxySourceVM
    {
        public required Guid Id { get; set; }
        public Guid? GroupId { get; set; }
        public required string Address { get; set; }
        public required int Port { get; set; }
        public required ProxyType ProxyType { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public int Index { get; set; }

        public Uri GetUri()
        {
            var builder = new UriBuilder
            {
                Scheme = ProxyType.ToString().ToLower(),
                Host = Address,
                Port = Port
            };
            if (!string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password))
            {
                builder.UserName = UserName;
                builder.Password = Password;
            }
            return builder.Uri;
        }
    }

    public class CreateProxySourceVM
    {
        public Guid? GroupId { get; set; }

        [Required, StringLength(253)]
        public string Address { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; }

        public ProxyType ProxyType { get; set; }

        [StringLength(128)]
        public string? UserName { get; set; }

        [StringLength(256)]
        public string? Password { get; set; }
    }

    public class UpdateProxySourceVM : CreateProxySourceVM
    {
        public Guid Id { get; set; }
    }

    public class DeleteProxySourceVM
    {
        public Guid Id { get; set; }
    }

    public class BulkCreateProxySourceVM
    {
        public Guid? GroupId { get; set; }

        public ProxyType ProxyType { get; set; }

        [Required]
        public string Lines { get; set; } = string.Empty;
    }

    public class BulkCreateProxySourceResultVM
    {
        public int Created { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class AssignGroupProxySourceVM
    {
        public Guid? GroupId { get; set; }

        [Required]
        public List<Guid> Ids { get; set; } = new();
    }

    public class BulkDeleteProxySourceVM
    {
        [Required]
        public List<Guid> Ids { get; set; } = new();
    }

    public class ReorderProxySourceVM
    {
        public Guid? GroupId { get; set; }

        [Required]
        public List<Guid> Ids { get; set; } = new();
    }
}
