using System.Globalization;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Services
{
    public class ProxySourceGroupFilterService : IProxySourceGroupFilterService
    {
        readonly ConfigStore _store;

        public ProxySourceGroupFilterService(ConfigStore store)
        {
            _store = store;
        }

        List<ProxySourceGroupFilterVM> Filters => _store.Config.Filters;
        List<ProxySourceGroupVM> Groups => _store.Config.Groups;

        static ProxySourceGroupFilterVM Clone(ProxySourceGroupFilterVM f) => new()
        {
            Id = f.Id,
            GroupId = f.GroupId,
            FilterType = f.FilterType,
            TrafficDirection = f.TrafficDirection,
            Filter = f.Filter,
            IsNot = f.IsNot,
        };

        public IReadOnlyList<ProxySourceGroupFilterVM> ListByGroup(Guid groupId)
        {
            lock (_store.SyncRoot)
            {
                if (!Groups.Any(x => x.Id == groupId))
                    return Array.Empty<ProxySourceGroupFilterVM>();

                return Filters
                    .Where(x => x.GroupId == groupId)
                    .OrderBy(x => x.FilterType)
                    .ThenBy(x => x.Filter)
                    .Select(Clone)
                    .ToList();
            }
        }

        public IReadOnlyList<ProxySourceGroupSnapshotVM> ListGroupSnapshots()
        {
            lock (_store.SyncRoot)
            {
                return Groups
                    .OrderBy(g => g.Priority)
                    .ThenBy(g => g.Name)
                    .Select(g => new ProxySourceGroupSnapshotVM
                    {
                        GroupId = g.Id,
                        GroupName = g.Name,
                        Priority = g.Priority,
                        MatchMode = g.MatchMode,
                        Filters = Filters
                            .Where(f => f.GroupId == g.Id)
                            .OrderBy(f => f.FilterType)
                            .ThenBy(f => f.Filter)
                            .Select(Clone)
                            .ToList(),
                    })
                    .ToList();
            }
        }

        public Guid Create(CreateProxySourceGroupFilterVM model)
        {
            lock (_store.SyncRoot)
            {
                if (!Groups.Any(x => x.Id == model.GroupId))
                    throw new InvalidOperationException("Group not found.");

                var (normalized, direction) = NormalizeFilter(model.FilterType, model.Filter, model.TrafficDirection);
                var entity = new ProxySourceGroupFilterVM
                {
                    Id = Guid.NewGuid(),
                    GroupId = model.GroupId,
                    FilterType = model.FilterType,
                    TrafficDirection = direction,
                    Filter = normalized,
                    IsNot = model.IsNot,
                };
                Filters.Add(entity);
                _store.Save();
                return entity.Id;
            }
        }

        public BulkCreateProxySourceGroupFilterResultVM BulkCreate(BulkCreateProxySourceGroupFilterVM model)
        {
            lock (_store.SyncRoot)
            {
                var result = new BulkCreateProxySourceGroupFilterResultVM();

                if (!Groups.Any(x => x.Id == model.GroupId))
                    throw new InvalidOperationException("Group not found.");

                var lines = (model.Lines ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var entities = new List<ProxySourceGroupFilterVM>();
                for (int i = 0; i < lines.Length; i++)
                {
                    var raw = lines[i].Trim();
                    if (raw.Length == 0) { result.Skipped++; continue; }
                    if (raw.StartsWith('#')) { result.Skipped++; continue; }
                    if (raw.Length > 256) { result.Errors.Add($"Line {i + 1}: filter too long."); continue; }

                    string normalized;
                    ProxyTrafficDirection? direction;
                    try
                    {
                        (normalized, direction) = NormalizeFilter(model.FilterType, raw, model.TrafficDirection);
                    }
                    catch (InvalidOperationException ex)
                    {
                        result.Errors.Add($"Line {i + 1}: {ex.Message}");
                        continue;
                    }

                    entities.Add(new ProxySourceGroupFilterVM
                    {
                        Id = Guid.NewGuid(),
                        GroupId = model.GroupId,
                        FilterType = model.FilterType,
                        TrafficDirection = direction,
                        Filter = normalized,
                        IsNot = model.IsNot,
                    });
                }

                if (entities.Count > 0)
                {
                    Filters.AddRange(entities);
                    result.Created = entities.Count;
                    _store.Save();
                }

                return result;
            }
        }

        public void Update(UpdateProxySourceGroupFilterVM model)
        {
            lock (_store.SyncRoot)
            {
                var entity = Filters.FirstOrDefault(x => x.Id == model.Id);
                if (entity == null) throw new InvalidOperationException("Filter not found.");

                var (normalized, direction) = NormalizeFilter(model.FilterType, model.Filter, model.TrafficDirection);
                entity.FilterType = model.FilterType;
                entity.TrafficDirection = direction;
                entity.Filter = normalized;
                entity.IsNot = model.IsNot;
                _store.Save();
            }
        }

        public void UpdateIsNot(UpdateProxySourceGroupFilterIsNotVM model)
        {
            lock (_store.SyncRoot)
            {
                var entity = Filters.FirstOrDefault(x => x.Id == model.Id);
                if (entity == null) throw new InvalidOperationException("Filter not found.");
                if (entity.IsNot == model.IsNot) return;
                entity.IsNot = model.IsNot;
                _store.Save();
            }
        }

        public void Delete(Guid id)
        {
            lock (_store.SyncRoot)
            {
                if (Filters.RemoveAll(x => x.Id == id) > 0)
                    _store.Save();
            }
        }

        // TotalBytes: Filter is a non-negative byte threshold (long), TrafficDirection required.
        // Others: TrafficDirection forced null, Filter trimmed only.
        static (string Filter, ProxyTrafficDirection? Direction) NormalizeFilter(
            ProxySourceGroupFilterType filterType,
            string rawFilter,
            ProxyTrafficDirection? direction)
        {
            var trimmed = (rawFilter ?? string.Empty).Trim();
            if (filterType == ProxySourceGroupFilterType.TotalBytes)
            {
                if (!direction.HasValue)
                    throw new InvalidOperationException("TrafficDirection is required for TotalBytes filter.");
                if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold) || threshold < 0)
                    throw new InvalidOperationException("Filter must be a non-negative integer (bytes) for TotalBytes filter.");
                return (threshold.ToString(CultureInfo.InvariantCulture), direction);
            }
            return (trimmed, null);
        }
    }
}
