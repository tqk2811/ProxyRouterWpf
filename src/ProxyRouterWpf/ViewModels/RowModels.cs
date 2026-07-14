using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Helpers;
using ProxyRouterWpf.Localization;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.ViewModels
{
    /// <summary>Display row for a proxy source in a DataGrid.</summary>
    public sealed class ProxySourceRow
    {
        public ProxySourceVM Source { get; }
        public bool IsRunning { get; }

        public ProxySourceRow(ProxySourceVM source, bool isRunning)
        {
            Source = source;
            IsRunning = isRunning;
        }

        public Guid Id => Source.Id;
        public string ProxyTypeText => Source.ProxyType.ToString();

        public string Display
        {
            get
            {
                var scheme = Source.ProxyType.ToString().ToLowerInvariant();
                var cred = !string.IsNullOrEmpty(Source.UserName)
                    ? (!string.IsNullOrEmpty(Source.Password) ? $"{Source.UserName}:{Source.Password}@" : $"{Source.UserName}@")
                    : string.Empty;
                return $"{scheme}://{cred}{Source.Address}:{Source.Port}";
            }
        }

        public string LockTip => IsRunning ? Loc.S("Str.Proxies.RunningLockTip") : string.Empty;
    }

    /// <summary>Display row for a routing group.</summary>
    public sealed class GroupRow
    {
        public ProxySourceGroupVM Group { get; }
        public int FilterCount { get; }
        public int SourceCount { get; }

        public GroupRow(ProxySourceGroupVM group, int filterCount, int sourceCount)
        {
            Group = group;
            FilterCount = filterCount;
            SourceCount = sourceCount;
        }

        public Guid Id => Group.Id;
        public string Name => Group.Name;
        public int Priority => Group.Priority;
        public ProxySourceGroupMatchMode MatchMode => Group.MatchMode;
        public string Summary => Loc.F("Str.Proxies.GroupSummary", SourceCount, FilterCount);
    }

    /// <summary>Display row for a group filter.</summary>
    public sealed class FilterRow
    {
        public ProxySourceGroupFilterVM Filter { get; }

        public FilterRow(ProxySourceGroupFilterVM filter)
        {
            Filter = filter;
        }

        public Guid Id => Filter.Id;
        public string TypeText => LocalizationManager.EnumText(Filter.FilterType);
        public bool IsNot => Filter.IsNot;

        public string Display
        {
            get
            {
                if (Filter.FilterType == ProxySourceGroupFilterType.TotalBytes && long.TryParse(Filter.Filter, out var bytes))
                {
                    var dir = LocalizationManager.EnumText(Filter.TrafficDirection ?? ProxyTrafficDirection.Both);
                    return $"{dir} ≥ {BytesFormatter.FormatBytes(bytes)}";
                }
                return Filter.Filter;
            }
        }
    }
}
