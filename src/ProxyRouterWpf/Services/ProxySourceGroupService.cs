using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public class ProxySourceGroupService : IProxySourceGroupService
    {
        readonly ConfigStore _store;

        public ProxySourceGroupService(ConfigStore store)
        {
            _store = store;
        }

        List<ProxySourceGroupVM> Groups => _store.Config.Groups;
        List<ProxySourceGroupFilterVM> Filters => _store.Config.Filters;
        List<ProxySourceVM> Sources => _store.Config.Sources;

        public IReadOnlyList<ProxySourceGroupVM> List()
        {
            lock (_store.SyncRoot)
            {
                return Groups
                    .OrderBy(x => x.Priority)
                    .ThenBy(x => x.Name)
                    .Select(g => new ProxySourceGroupVM
                    {
                        Id = g.Id,
                        Name = g.Name,
                        Priority = g.Priority,
                        MatchMode = g.MatchMode,
                    })
                    .ToList();
            }
        }

        public Guid Create(CreateProxySourceGroupVM model)
        {
            lock (_store.SyncRoot)
            {
                var name = model.Name.Trim();
                if (Groups.Any(x => x.Name == name))
                    throw new InvalidOperationException("Group name already exists.");

                var maxPriority = Groups.Select(x => (int?)x.Priority).DefaultIfEmpty(null).Max();
                var entity = new ProxySourceGroupVM
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Priority = (maxPriority ?? -1) + 1,
                    MatchMode = model.MatchMode,
                };
                Groups.Add(entity);
                _store.Save();
                return entity.Id;
            }
        }

        public void Update(UpdateProxySourceGroupVM model)
        {
            lock (_store.SyncRoot)
            {
                var entity = Groups.FirstOrDefault(x => x.Id == model.Id);
                if (entity == null) throw new InvalidOperationException("Group not found.");

                var name = model.Name.Trim();
                if (!string.Equals(entity.Name, name, StringComparison.Ordinal))
                {
                    if (Groups.Any(x => x.Name == name && x.Id != model.Id))
                        throw new InvalidOperationException("Group name already exists.");
                    entity.Name = name;
                }
                entity.MatchMode = model.MatchMode;
                _store.Save();
            }
        }

        public void UpdateMatchMode(UpdateProxySourceGroupMatchModeVM model)
        {
            lock (_store.SyncRoot)
            {
                var entity = Groups.FirstOrDefault(x => x.Id == model.Id);
                if (entity == null) throw new InvalidOperationException("Group not found.");
                if (entity.MatchMode == model.MatchMode) return;
                entity.MatchMode = model.MatchMode;
                _store.Save();
            }
        }

        public void Reorder(IReadOnlyList<Guid> orderedIds)
        {
            lock (_store.SyncRoot)
            {
                if (orderedIds.Count == 0) return;
                var byId = Groups.ToDictionary(x => x.Id);
                for (int i = 0; i < orderedIds.Count; i++)
                {
                    if (byId.TryGetValue(orderedIds[i], out var e))
                        e.Priority = i;
                }
                _store.Save();
            }
        }

        public void Delete(Guid id)
        {
            lock (_store.SyncRoot)
            {
                var entity = Groups.FirstOrDefault(x => x.Id == id);
                if (entity == null) return;

                // Cascade: remove the group's filters and sources (matches the original DB cascade).
                Filters.RemoveAll(x => x.GroupId == id);
                Sources.RemoveAll(x => x.GroupId == id);
                Groups.Remove(entity);
                _store.Save();
            }
        }
    }
}
