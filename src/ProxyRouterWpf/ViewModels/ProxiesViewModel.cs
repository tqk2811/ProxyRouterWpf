using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Localization;
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
            LoadLocalIps();
            LoadListenAddresses();
            ReloadAll();
        }

        /// <summary>Called by the shell to navigate to the Logs tab filtered by a picked source.</summary>
        public Action<ProxySourceVM>? ShowLogForSource { get; set; }

        // ---- Server configure fields (bound two-way) ----
        [ObservableProperty] int startPort = 30000;
        [ObservableProperty] string listenAddress = "0.0.0.0";
        [ObservableProperty] string? proxyUserName;
        [ObservableProperty] string? proxyPassword;
        [ObservableProperty] string? proxySocks4UserId;
        [ObservableProperty] bool isHttpEnabled = true;
        [ObservableProperty] bool isSocks4Enabled;
        [ObservableProperty] bool isSocks5Enabled = true;

        // ---- Runtime ----
        [ObservableProperty] bool isRunning;
        [ObservableProperty] string statusText = Loc.S("Str.Proxies.Stopped");
        [ObservableProperty] string? selectedIp;
        [ObservableProperty] string outputFormat = "http_socks5";
        [ObservableProperty] string outputPreview = string.Empty;

        /// <summary>All IPv4 addresses bound to this machine (for building the copy-to-clipboard output).</summary>
        public ObservableCollection<string> LocalIps { get; } = new();

        /// <summary>Addresses the listeners can bind to: "0.0.0.0" (all) + each LAN/host IPv4.</summary>
        public ObservableCollection<string> ListenAddresses { get; } = new();

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

        /// <summary>Rebuild localized rows and status text after a UI language change.</summary>
        public void OnLanguageChanged() => ReloadAll();

        void LoadConfigFields()
        {
            var c = _svc.Configure.Get();
            StartPort = c.StartPort;
            ListenAddress = string.IsNullOrWhiteSpace(c.ListenAddress) ? "0.0.0.0" : c.ListenAddress;
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
            StatusText = IsRunning ? Loc.F("Str.Proxies.Running", _activeIds.Count) : Loc.S("Str.Proxies.Stopped");
            ReloadSourceRows();
            BuildOutputPreview();
        }

        public void ReloadAll()
        {
            _activeIds = _svc.Manager.GetActiveSourceIds().ToHashSet();
            IsRunning = _svc.Manager.IsRunning;
            StatusText = IsRunning ? Loc.F("Str.Proxies.Running", _activeIds.Count) : Loc.S("Str.Proxies.Stopped");
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
                MessageBox.Show(Loc.S("Str.Proxies.StartPortRange"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            _svc.Configure.Update(new UpdateProxyConfigureVM
            {
                StartPort = StartPort,
                ListenAddress = string.IsNullOrWhiteSpace(ListenAddress) ? "0.0.0.0" : ListenAddress,
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
                StatusText = IsRunning ? StatusText : Loc.S("Str.Proxies.ConfigSaved");
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
                MessageBox.Show(Loc.S("Str.Proxies.EnableProtocol"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!SaveConfig())
                return;
            if (_svc.Sources.ListByGroup(null).Count == 0)
            {
                MessageBox.Show(Loc.S("Str.Proxies.NoUngrouped"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int started = await Task.Run(() => _svc.Manager.Start());
            RefreshRuntime();
            if (started == 0)
                MessageBox.Show(Loc.S("Str.Proxies.NoListenerStarted"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ---------- Local IP + output preview ----------
        partial void OnOutputFormatChanged(string value) => BuildOutputPreview();
        partial void OnSelectedIpChanged(string? value) => BuildOutputPreview();

        void LoadLocalIps()
        {
            var current = SelectedIp;
            LocalIps.Clear();
            foreach (var ip in _svc.LocalIp.GetAll())
                LocalIps.Add(ip);
            SelectedIp = (current != null && LocalIps.Contains(current)) ? current : LocalIps.FirstOrDefault();
        }

        void LoadListenAddresses()
        {
            var current = ListenAddress;
            ListenAddresses.Clear();
            ListenAddresses.Add("0.0.0.0"); // bind all interfaces
            foreach (var ip in _svc.LocalIp.GetAll())
                if (!ListenAddresses.Contains(ip))
                    ListenAddresses.Add(ip);
            // keep the configured address selectable even if that NIC is currently gone
            if (!string.IsNullOrEmpty(current) && !ListenAddresses.Contains(current))
                ListenAddresses.Add(current);
            ListenAddress = (!string.IsNullOrEmpty(current) && ListenAddresses.Contains(current)) ? current : "0.0.0.0";
        }

        void BuildOutputPreview()
        {
            var lines = BuildOutputLines();
            OutputPreview = lines.Count == 0 ? Loc.S("Str.Proxies.NoProxyOutput") : string.Join(Environment.NewLine, lines);
        }

        List<string> BuildOutputLines()
        {
            var ip = string.IsNullOrEmpty(SelectedIp) ? "0.0.0.0" : SelectedIp!;
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
                MessageBox.Show(Loc.S("Str.Proxies.NoProxyToCopy"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            try { Clipboard.SetText(string.Join(Environment.NewLine, lines)); StatusText = Loc.F("Str.Proxies.Copied", lines.Count); }
            catch { /* clipboard busy */ }
        }

        [RelayCommand]
        void Refresh() { LoadLocalIps(); LoadListenAddresses(); ReloadAll(); }

        // ---------- Source CRUD (called from view code-behind) ----------
        public void AddSourcesBulk(Guid? groupId, ProxyType proxyType, string lines)
        {
            try
            {
                var r = _svc.Sources.BulkCreate(new BulkCreateProxySourceVM { GroupId = groupId, ProxyType = proxyType, Lines = lines });
                ReloadAll();
                var msg = Loc.F("Str.Proxies.AddedSkipped", r.Created, r.Skipped) + (r.Errors.Count > 0 ? Loc.S("Str.Proxies.ErrorsPrefix") + string.Join("\n", r.Errors.Take(20)) : "");
                MessageBox.Show(msg, Loc.S("Str.Proxies.AddProxyCaption"), MessageBoxButton.OK, r.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
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
            if (MessageBox.Show(Loc.F("Str.Proxies.DeleteProxiesConfirm", ids.Count), Loc.S("Str.Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try { _svc.Sources.BulkDelete(new BulkDeleteProxySourceVM { Ids = ids.ToList() }); ReloadAll(); }
            catch (Exception ex) { ShowError(ex); }
        }

        public void AssignGroup(Guid? groupId, IReadOnlyList<Guid> ids)
        {
            if (ids.Count == 0) return;
            try { _svc.Sources.AssignGroup(new AssignGroupProxySourceVM { GroupId = groupId, Ids = ids.ToList() }); ReloadAll(); }
            catch (Exception ex) { ShowError(ex); }
        }

        /// <summary>Persist a drag-drop reorder of proxy sources within one group (null = ungrouped).</summary>
        public void ReorderSources(Guid? groupId, IReadOnlyList<Guid> orderedIds)
        {
            if (orderedIds.Count == 0) return;
            try { _svc.Sources.Reorder(groupId, orderedIds); ReloadAll(); }
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
            if (MessageBox.Show(Loc.S("Str.Proxies.DeleteGroupConfirm"), Loc.S("Str.Common.Confirm"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
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

        /// <summary>Persist a drag-drop reorder of the whole group list (updates priorities).</summary>
        public void ReorderGroups(IReadOnlyList<Guid> orderedIds)
        {
            if (orderedIds.Count == 0) return;
            try { _svc.Groups.Reorder(orderedIds); ReloadGroups(); }
            catch (Exception ex) { ShowError(ex); }
        }

        // ---------- Filter CRUD ----------
        public void AddFiltersBulk(Guid groupId, ProxySourceGroupFilterType type, ProxyTrafficDirection? dir, bool isNot, string lines)
        {
            try
            {
                var r = _svc.Filters.BulkCreate(new BulkCreateProxySourceGroupFilterVM { GroupId = groupId, FilterType = type, TrafficDirection = dir, IsNot = isNot, Lines = lines });
                ReloadGroups();
                var msg = Loc.F("Str.Proxies.AddedSkipped", r.Created, r.Skipped) + (r.Errors.Count > 0 ? Loc.S("Str.Proxies.ErrorsPrefix") + string.Join("\n", r.Errors.Take(20)) : "");
                MessageBox.Show(msg, Loc.S("Str.Proxies.AddFilterCaption"), MessageBoxButton.OK, r.Errors.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
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
            => MessageBox.Show(ex.Message, Loc.S("Str.Common.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
