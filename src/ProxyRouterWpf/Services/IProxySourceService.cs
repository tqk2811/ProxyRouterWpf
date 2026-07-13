using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public interface IProxySourceService
    {
        IReadOnlyList<ProxySourceVM> ListByGroup(Guid? groupId);
        Guid Create(CreateProxySourceVM model);
        BulkCreateProxySourceResultVM BulkCreate(BulkCreateProxySourceVM model);
        void Update(UpdateProxySourceVM model);
        int AssignGroup(AssignGroupProxySourceVM model);
        void Reorder(Guid? groupId, IReadOnlyList<Guid> orderedIds);
        void Delete(Guid id);
        int BulkDelete(BulkDeleteProxySourceVM model);
    }
}
