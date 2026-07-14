using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Localization;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.ViewModels
{
    public sealed class FilterOption<T> where T : struct
    {
        public string Label { get; init; } = string.Empty;
        public T? Value { get; init; }
        public override string ToString() => Label;
    }

    public partial class LogsViewModel : ObservableObject
    {
        readonly AppServices _svc;
        readonly DispatcherTimer _autoTimer;

        public LogsViewModel(AppServices svc)
        {
            _svc = svc;

            OutcomeOptions = new();
            ProxyTypeOptions = new();
            BuildFilterOptions();

            _autoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _autoTimer.Tick += (_, _) => Reload();

            Reload();
            if (AutoRefresh)
                _autoTimer.Start();
        }

        /// <summary>(Re)builds the localized filter dropdown options, preserving the current selection by value.</summary>
        void BuildFilterOptions()
        {
            var prevOutcome = SelectedOutcome?.Value;
            OutcomeOptions.Clear();
            OutcomeOptions.Add(new FilterOption<ProxyTunnelOutcome> { Label = Loc.S("Str.Logs.AllOutcome"), Value = null });
            foreach (ProxyTunnelOutcome o in Enum.GetValues<ProxyTunnelOutcome>())
                OutcomeOptions.Add(new FilterOption<ProxyTunnelOutcome> { Label = LocalizationManager.EnumText(o), Value = o });
            SelectedOutcome = OutcomeOptions.FirstOrDefault(x => x.Value.Equals(prevOutcome)) ?? OutcomeOptions[0];

            var prevType = SelectedProxyType?.Value;
            ProxyTypeOptions.Clear();
            ProxyTypeOptions.Add(new FilterOption<ProxyType> { Label = Loc.S("Str.Logs.AllType"), Value = null });
            foreach (ProxyType t in Enum.GetValues<ProxyType>())
                ProxyTypeOptions.Add(new FilterOption<ProxyType> { Label = LocalizationManager.EnumText(t), Value = t });
            SelectedProxyType = ProxyTypeOptions.FirstOrDefault(x => x.Value.Equals(prevType)) ?? ProxyTypeOptions[0];
        }

        /// <summary>Rebuild localized option labels, re-run the query (regenerates PageInfo/items) after a language change.</summary>
        public void OnLanguageChanged()
        {
            BuildFilterOptions();
            Reload();
        }

        public List<int> PageSizeOptions { get; } = new() { 25, 50, 100, 200 };
        public ObservableCollection<FilterOption<ProxyTunnelOutcome>> OutcomeOptions { get; }
        public ObservableCollection<FilterOption<ProxyType>> ProxyTypeOptions { get; }

        [ObservableProperty] FilterOption<ProxyTunnelOutcome>? selectedOutcome;
        [ObservableProperty] FilterOption<ProxyType>? selectedProxyType;
        [ObservableProperty] string? clientServer;
        [ObservableProperty] string? targetHost;
        [ObservableProperty] string? pickedSource;
        [ObservableProperty] DateTime? fromDate;
        [ObservableProperty] DateTime? toDate;

        [ObservableProperty] int page = 1;
        [ObservableProperty] int pageSize = 50;
        [ObservableProperty] int totalCount;
        [ObservableProperty] int totalPages = 1;
        [ObservableProperty] string pageInfo = "0 log";

        [ObservableProperty] bool autoRefresh = true;
        [ObservableProperty] bool isEmpty = true;

        ProxyTunnelLogSortBy _sortBy = ProxyTunnelLogSortBy.EndAt;
        bool _sortDesc = true;

        public ObservableCollection<ProxyTunnelLogListItemVM> Items { get; } = new();

        partial void OnAutoRefreshChanged(bool value)
        {
            if (value) _autoTimer.Start(); else _autoTimer.Stop();
        }

        partial void OnPageSizeChanged(int value) { Page = 1; Reload(); }

        public void Reload()
        {
            var req = new ProxyTunnelLogListRequestVM
            {
                Page = Page,
                PageSize = PageSize,
                Outcome = SelectedOutcome?.Value,
                ClientServer = ClientServer,
                TargetHost = TargetHost,
                PickedSource = PickedSource,
                PickedSourceProxyType = SelectedProxyType?.Value,
                FromUtc = FromDate?.ToUniversalTime(),
                ToUtc = ToDate?.Date.AddDays(1).AddTicks(-1).ToUniversalTime(),
                SortBy = _sortBy,
                SortDesc = _sortDesc,
            };
            var res = _svc.TunnelLogs.List(req);
            Items.Clear();
            foreach (var it in res.Items) Items.Add(it);
            IsEmpty = res.TotalCount == 0;
            TotalCount = res.TotalCount;
            TotalPages = Math.Max(1, (int)Math.Ceiling(res.TotalCount / (double)res.PageSize));
            if (Page > TotalPages) { Page = TotalPages; }
            PageInfo = Loc.F("Str.Logs.PageInfo", Page, TotalPages, TotalCount, _svc.LogStore.Capacity);
        }

        [RelayCommand]
        void Apply() { Page = 1; Reload(); }

        [RelayCommand]
        void ClearFilters()
        {
            SelectedOutcome = OutcomeOptions[0];
            SelectedProxyType = ProxyTypeOptions[0];
            ClientServer = null;
            TargetHost = null;
            PickedSource = null;
            FromDate = null;
            ToDate = null;
            Page = 1;
            Reload();
        }

        [RelayCommand]
        void Prev() { if (Page > 1) { Page--; Reload(); } }

        [RelayCommand]
        void Next() { if (Page < TotalPages) { Page++; Reload(); } }

        [RelayCommand]
        void Refresh() => Reload();

        [RelayCommand]
        void ClearAll()
        {
            if (MessageBox.Show(Loc.S("Str.Logs.ClearAllConfirm"), Loc.S("Str.Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            _svc.TunnelLogs.Clear();
            Page = 1;
            Reload();
        }

        public void SetSort(ProxyTunnelLogSortBy sortBy)
        {
            if (_sortBy == sortBy) _sortDesc = !_sortDesc;
            else { _sortBy = sortBy; _sortDesc = true; }
            Reload();
        }

        public ProxyTunnelLogVM? GetDetail(long id) => _svc.TunnelLogs.GetById(id);

        /// <summary>Preset the filter for a picked upstream source and apply (used by the Proxies tab).</summary>
        public void FilterByPickedSource(string address, int port, ProxyType type)
        {
            PickedSource = $"{address}:{port}";
            SelectedProxyType = ProxyTypeOptions.FirstOrDefault(o => o.Value == type) ?? ProxyTypeOptions[0];
            Page = 1;
            Reload();
        }
    }
}
