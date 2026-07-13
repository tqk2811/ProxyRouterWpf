using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public interface IProxySourceGroupService
    {
        IReadOnlyList<ProxySourceGroupVM> List();
        Guid Create(CreateProxySourceGroupVM model);
        void Update(UpdateProxySourceGroupVM model);
        void UpdateMatchMode(UpdateProxySourceGroupMatchModeVM model);
        void Delete(Guid id);
        void Reorder(IReadOnlyList<Guid> orderedIds);
    }
}
