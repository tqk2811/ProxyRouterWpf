using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public interface IProxySourceGroupFilterService
    {
        IReadOnlyList<ProxySourceGroupFilterVM> ListByGroup(Guid groupId);
        IReadOnlyList<ProxySourceGroupSnapshotVM> ListGroupSnapshots();
        Guid Create(CreateProxySourceGroupFilterVM model);
        BulkCreateProxySourceGroupFilterResultVM BulkCreate(BulkCreateProxySourceGroupFilterVM model);
        void Update(UpdateProxySourceGroupFilterVM model);
        void UpdateIsNot(UpdateProxySourceGroupFilterIsNotVM model);
        void Delete(Guid id);
    }
}
