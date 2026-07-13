using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;
using ProxyRouterWpf.Services;

namespace ProxyRouterWpf.ViewModels
{
    public partial class ProxiesViewModel : ObservableObject
    {
        readonly AppServices _svc;

        public ProxiesViewModel(AppServices svc)
        {
            _svc = svc;
            _svc.Manager.StateChanged += OnManagerStateChanged;
            LoadConfigFields();
            ReloadAll();
            _ = RefreshPublicIpAsync();
        }

        /// <summary>Called by the shell to navigate to the Logs tab filtered by a picked source.</summary>
        public Action<ProxySourceVM>? ShowLogForSource { get; set; }

        // ---- Server configure fields (bound two-way) ----
        [ObservableProperty] int startPort = 30000;
        [ObservableProperty] string? proxyUserName;
        [ObservableProperty] string? proxyPassword;
        [ObservableProperty] string? proxySocks4UserId;
        [ObservableProperty] bool isHttpEnabled = true;
        [ObservableProperty] bool isSocks4Enabled;
        [ObservableProperty] bool isSocks5Enabled = true;

        // ---- Runtime ----
        [ObservableProperty] bool isRunning;
        [ObservableProperty] string statusText = "Đã dừng";
        [ObservableProperty] string? publicIpText;
        [ObservableProperty] string outputFormat = "http_socks5";
        [ObservableProperty] string outputPreview = string.Empty;

        public bool CanEdit => !IsRunning;

        public IReadOnlyList<ProxySourceGroupMatchMode> MatchModeValues { get; } = Enum.GetValues<ProxySourceGroupMatchMode>();

        HashSet<Guid> _activeIds = new();

        // ---- Collections ----
        public ObservableCollection<ProxySourceRow> UngroupedSources { get; } = new();
        public ObservableCollection<GroupRow> Groups { get; } = new();
        public ObservableCollection<ProxySourceRow> GroupSources { get; } = new();
        public ObservableCollection<FilterRow> Filters { get; } = new();

        [ObservableProperty] GroupRow? selectedGroup;

        partial void OnSelectedGroupChanged(GroupRow? value) => LoadGroupDetail();
        partial void OnIsRunningChanged(bool value) => OnPropertyChanged(nameof(CanEdit));

        void OnManagerStateChanged() => Application.Current?.Dispatcher.Invoke(RefreshRuntime);

        void LoadConfigFields()
        {
            var c = _svc.Configure.Get();
            StartPort = c.StartPort;
            ProxyUserName = c.ProxyUserName;
            ProxyPassword = c.ProxyPassword;
            ProxySocks4UserId = c.ProxySocks4UserId;
            IsHttpEnabled = c.IsHttpEnabled;
            IsSocks4Enabled = c.IsSocks4Enabled;
            IsSocks5Enabled = c.IsSocks5Enabled;
        }

        public void RefreshRuntime()
        {
            _activeIds = _svc.Manager.GetActiveSourceIds().ToHashSet();
            IsRunning = _svc.Manager.IsRunning;
            StatusText = IsRunning ? $"Đang chạy · {_activeIds.Count} listener" : "Đã dừng";
            ReloadSourceRows();
            BuildOutputPreview();
        }

        public void ReloadAll()
        {
            _activeIds = _svc.Manager.GetActiveSourceIds().ToHashSet();
            IsRunning = _svc.Manager.IsRunning;
            StatusText = IsRunning ? $"Đang chạy · {_activeIds.Count} listener" : "Đã dừng";
            ReloadGroups();
            ReloadSourceRows();
            BuildOutputPreview();
        }

        void ReloadGroups()
        {
            var prevId = SelectedGroup?.Id;
            Groups.Clear();
            var filters = _svc.Filters; // for counts we recompute via snapshots
            foreach (var g in _svc.Groups.List())
            {
                int fc = _svc.Filters.ListByGroup(g.Id).Count;
                int sc = _svc.Sources.ListByGroup(g.Id).Count;
                Groups.Add(new GroupRow(g, fc, sc));
            }
            SelectedGroup = Groups.FirstOrDefault(x => x.Id == prevId) ?? Groups.FirstOrDefault();
            LoadGroupDetail();
        }

        void ReloadSourceRows()
        {
            UngroupedSources.Clear();
            foreach (var s in _svc.Sources.ListByGroup(null))
                UngroupedSources.Add(new ProxySourceRow(s, _activeIds.Contains(s.Id)));
        }

        void LoadGroupDetail()
        {
            GroupSources.Clear();
            Filters.Clear();
            if (SelectedGroup == null) return;
            foreach (var s in _svc.Sources.ListByGroup(SelectedGroup.Id))
                GroupSources.Add(new ProxySourceRow(s, _activeIds.Contains(s.Id)));
            foreach (var f in _svc.Filters.ListByGroup(SelectedGroup.Id))
                Filters.Add(new FilterRow(f));
        }

        // ---------- Config save ----------
        public bool SaveConfig()
        {
            if (StartPort < 10000 || StartPort > 65535)
            {
                MessageBox.Show("Start Port phải trong khoảng 10000..65535.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            _svc.Configure.Update(new UpdateProxyConfigureVM
            {
                StartPort = StartPort,
                ProxyUserName = ProxyUserName,
                ProxyPassword = ProxyPassword,
                ProxySocks4UserId = ProxySocks4UserId,
                IsHttpEnabled = IsHttpEnabled,
                IsSocks4Enabled = IsSocks4Enabled,
                IsSocks5Enabled = IsSocks5Enabled,
            });
            BuildOutputPreview();
            return true;
        }

        [RelayCommand]
        void SaveConfigure()
        {
            if (SaveConfig())
                StatusText = IsRunning ? StatusText : "Đã lưu cấu hình";
        }

        // ---------- Toggle ----------
        [RelayCommand]
        async Task ToggleAsync()
        {
            if (IsRunning)
            {
                await Task.Run(() => _svc.Manager.Stop());
                RefreshRuntime();
                return;
            }

            if (!IsHttpEnabled && !IsSocks4Enabled && !IsSocks5Enabled)
            {
                MessageBox.Show("Bật ít nhất một giao thức (HTTP/SOCKS4/SOCKS5) trước khi khởi động.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!SaveConfig())
                return;
            if (_svc.Sources.ListByGroup(null).Count == 0)
            {
                MessageBox.Show("Chưa có proxy nguồn nào (Ungrouped). Thêm proxy trước khi khởi động.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int started = await Task.Run(() => _svc.Manager.Start());
            RefreshRuntime();
            if (started == 0)
                MessageBox.Show("Không khởi động được listener nào (cổng có thể đang bị chiếm).", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ---------- Public IP + output preview ----------
        async Task RefreshPublicIpAsync()
        {
            var ip = await _svc.PublicIp.GetAsync();
            Application.Current?.Dispatcher.Invoke(() =>
            {
                PublicIpText = ip;
                BuildOutputPreview();
            });
        }

        partial void OnOutputFormatChanged(string value) => BuildOutputPreview();

        void BuildOutputPreview()
        {
            var lines = BuildOutputLines();
            OutputPreview = lines.Count == 0 ? "(chưa có proxy)" : string.Join(Environment.NewLine, lines);
        }

        List<string> BuildOutputLines()
        {
            var ip = string.IsNullOrEmpty(PublicIpText) ? "0.0.0.0" : PublicIpText!;
            int count = IsRunning ? _activeIds.Count : UngroupedSources.Count;
            var result = new List<string>();
            bool socks4 = string.Equals(OutputFormat, "socks4", StringComparison.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                int port = StartPort + i;
                string line;
                if (socks4)
                {
                    line = !string.IsNullOrEmpty(ProxySocks4UserId) ? $"{ip}:{port}:{ProxySocks4UserId}" : $"{ip}:{port}";
                }
                else
                {
                    line = (!string.IsNullOrEmpty(ProxyUserName) && !string.IsNullOrEmpty(ProxyPassword))
                        ? $"{ip}:{port}:{ProxyUserName}:{ProxyPassword}"
                        : $"{ip}:{port}";
                }
                result.Add(line);
            }
            return result;
        }

        [RelayCommand]
        void CopyOutput()
        {
            var lines = BuildOutputLines();
            if (lines.Count == 0)
            {
                MessageBox.Show("Chưa có proxy để copy.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try { Clipboard.SetText(string.Join(Environment.NewLine, lines)); StatusText = $"Đã copy {lines.Count} dòng"; }
            catch { /* clipboard busy */ }
        }

        [RelayCommand]
        void Refresh() => ReloadAll();

        // ---------- Source CRUD (called from view code-behind) ----------
        public void AddSourcesBulk(Guid? groupId, ProxyType proxyType, string lines)
        {
            try
            {
                var r = _svc.Sources.BulkCreate(new BulkCreateProxySourceVM { GroupId = groupId, ProxyType = proxyType, Lines = lines });
                ReloadAll();
                var msg = $"Đã thêm {r.Created}, bỏ qua {r.Skipped}." + (r.Errors.Count > 0 ? "\n\nLỗi:\n" + string.Join("\n", r.Errors.Take(20)) : "");
                MessageBox.Show(msg, "Thêm proxy", MessageBoxButton.OK, r.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        public void UpdateSource(UpdateProxySourceVM model)
        {
            try { _svc.Sources.Update(model); ReloadAll(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void DeleteSources(IReadOnlyList<Guid> ids)
        {
            if (ids.Count == 0) return;
            if (MessageBox.Show($"Xoá {ids.Count} proxy?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { _svc.Sources.BulkDelete(new BulkDeleteProxySourceVM { Ids = ids.ToList() }); ReloadAll(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void AssignGroup(Guid? groupId, IReadOnlyList<Guid> ids)
        {
            if (ids.Count == 0) return;
            try { _svc.Sources.AssignGroup(new AssignGroupProxySourceVM { GroupId = groupId, Ids = ids.ToList() }); ReloadAll(); }
            catch (Exception ex) { ShowError(ex); }
        }

        // ---------- Group CRUD ----------
        public void AddGroup(string name, ProxySourceGroupMatchMode mode)
        {
            try { _svc.Groups.Create(new CreateProxySourceGroupVM { Name = name, MatchMode = mode }); ReloadGroups(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void UpdateGroup(Guid id, string name, ProxySourceGroupMatchMode mode)
        {
            try { _svc.Groups.Update(new UpdateProxySourceGroupVM { Id = id, Name = name, MatchMode = mode }); ReloadGroups(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void DeleteGroup(Guid id)
        {
            if (MessageBox.Show("Xoá group này? (Proxy và filter thuộc group cũng bị xoá)", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try { _svc.Groups.Delete(id); ReloadAll(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void SetGroupMatchMode(Guid id, ProxySourceGroupMatchMode mode)
        {
            try { _svc.Groups.UpdateMatchMode(new UpdateProxySourceGroupMatchModeVM { Id = id, MatchMode = mode }); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void MoveGroup(Guid id, int delta)
        {
            var ids = Groups.Select(x => x.Id).ToList();
            int idx = ids.IndexOf(id);
            if (idx < 0) return;
            int target = idx + delta;
            if (target < 0 || target >= ids.Count) return;
            (ids[idx], ids[target]) = (ids[target], ids[idx]);
            try { _svc.Groups.Reorder(ids); ReloadGroups(); }
            catch (Exception ex) { ShowError(ex); }
        }

        // ---------- Filter CRUD ----------
        public void AddFiltersBulk(Guid groupId, ProxySourceGroupFilterType type, ProxyTrafficDirection? dir, bool isNot, string lines)
        {
            try
            {
                var r = _svc.Filters.BulkCreate(new BulkCreateProxySourceGroupFilterVM { GroupId = groupId, FilterType = type, TrafficDirection = dir, IsNot = isNot, Lines = lines });
                ReloadGroups();
                var msg = $"Đã thêm {r.Created}, bỏ qua {r.Skipped}." + (r.Errors.Count > 0 ? "\n\nLỗi:\n" + string.Join("\n", r.Errors.Take(20)) : "");
                MessageBox.Show(msg, "Thêm filter", MessageBoxButton.OK, r.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
            }
            catch (Exception ex) { ShowError(ex); }
        }

        public void UpdateFilter(UpdateProxySourceGroupFilterVM model)
        {
            try { _svc.Filters.Update(model); ReloadGroups(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void DeleteFilter(Guid id)
        {
            try { _svc.Filters.Delete(id); ReloadGroups(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void SetFilterIsNot(Guid id, bool isNot)
        {
            try { _svc.Filters.UpdateIsNot(new UpdateProxySourceGroupFilterIsNotVM { Id = id, IsNot = isNot }); }
            catch (Exception ex) { ShowError(ex); }
        }

        public IReadOnlyList<ProxySourceGroupVM> AllGroups() => _svc.Groups.List();

        static void ShowError(Exception ex)
            => MessageBox.Show(ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
