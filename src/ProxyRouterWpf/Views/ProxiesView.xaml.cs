using System.Windows;
using System.Windows.Controls;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;
using ProxyRouterWpf.ViewModels;
using ProxyRouterWpf.Views.Dialogs;

namespace ProxyRouterWpf.Views
{
    public partial class ProxiesView : UserControl
    {
        public ProxiesView()
        {
            InitializeComponent();
        }

        ProxiesViewModel Vm => (ProxiesViewModel)DataContext;
        Window? Owner => Window.GetWindow(this);

        bool BlockIfRunning()
        {
            if (Vm.IsRunning)
            {
                MessageBox.Show("Dừng proxy trước khi chỉnh sửa danh sách proxy nguồn.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            return false;
        }

        static List<Guid> SelectedIds(DataGrid grid)
            => grid.SelectedItems.Cast<ProxySourceRow>().Select(r => r.Id).ToList();

        // ---------------- Ungrouped sources ----------------
        void AddUngrouped_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var dlg = new BulkAddSourcesWindow { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddSourcesBulk(null, dlg.ProxyType, dlg.Lines);
        }

        void EditUngrouped_Click(object sender, RoutedEventArgs e) => EditSource(UngroupedGrid);

        void AssignUngrouped_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var ids = SelectedIds(UngroupedGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            var dlg = new AssignGroupWindow(Vm.AllGroups(), ids.Count) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AssignGroup(dlg.GroupId, ids);
        }

        void DeleteUngrouped_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var ids = SelectedIds(UngroupedGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            Vm.DeleteSources(ids);
        }

        void LogForRow_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is ProxySourceRow row)
                Vm.ShowLogForSource?.Invoke(row.Source);
        }

        void EditSource(DataGrid grid)
        {
            if (BlockIfRunning()) return;
            if (grid.SelectedItem is not ProxySourceRow row) { WarnSelect(); return; }
            var dlg = new SourceEditWindow(Vm.AllGroups(), row.Source) { Owner = Owner };
            if (dlg.ShowDialog() == true)
            {
                Vm.UpdateSource(new UpdateProxySourceVM
                {
                    Id = row.Id,
                    GroupId = dlg.GroupId,
                    ProxyType = dlg.ProxyType,
                    Address = dlg.Address,
                    Port = dlg.Port,
                    UserName = dlg.UserName,
                    Password = dlg.Password,
                });
            }
        }

        // ---------------- Groups ----------------
        void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new GroupEditWindow { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddGroup(dlg.GroupName, dlg.MatchMode);
        }

        void EditGroup_Click(object sender, RoutedEventArgs e)
        {
            if (Vm.SelectedGroup is not GroupRow g) { WarnSelect(); return; }
            var dlg = new GroupEditWindow(g.Name, g.MatchMode) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.UpdateGroup(g.Id, dlg.GroupName, dlg.MatchMode);
        }

        void GroupUp_Click(object sender, RoutedEventArgs e)
        {
            if (Vm.SelectedGroup is GroupRow g) Vm.MoveGroup(g.Id, -1);
        }

        void GroupDown_Click(object sender, RoutedEventArgs e)
        {
            if (Vm.SelectedGroup is GroupRow g) Vm.MoveGroup(g.Id, +1);
        }

        void DeleteGroup_Click(object sender, RoutedEventArgs e)
        {
            if (Vm.SelectedGroup is GroupRow g) Vm.DeleteGroup(g.Id);
            else WarnSelect();
        }

        void MatchMode_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cb && cb.DataContext is GroupRow row && cb.SelectedItem is ProxySourceGroupMatchMode mode)
            {
                if (mode != row.MatchMode)
                    Vm.SetGroupMatchMode(row.Id, mode);
            }
        }

        // ---------------- Filters ----------------
        void AddFilter_Click(object sender, RoutedEventArgs e)
        {
            if (Vm.SelectedGroup is not GroupRow g) { MessageBox.Show("Chọn một nhóm trước.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var dlg = new BulkAddFiltersWindow { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddFiltersBulk(g.Id, dlg.FilterType, dlg.TrafficDirection, dlg.IsNot, dlg.Lines);
        }

        void EditFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FiltersGrid.SelectedItem is not FilterRow row) { WarnSelect(); return; }
            var dlg = new FilterEditWindow(row.Filter) { Owner = Owner };
            if (dlg.ShowDialog() == true)
            {
                Vm.UpdateFilter(new UpdateProxySourceGroupFilterVM
                {
                    Id = row.Id,
                    FilterType = dlg.FilterType,
                    TrafficDirection = dlg.TrafficDirection,
                    Filter = dlg.Filter,
                    IsNot = dlg.IsNot,
                });
            }
        }

        void DeleteFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FiltersGrid.SelectedItem is FilterRow row) Vm.DeleteFilter(row.Id);
            else WarnSelect();
        }

        void FilterIsNot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is FilterRow row)
                Vm.SetFilterIsNot(row.Id, cb.IsChecked == true);
        }

        // ---------------- Group sources ----------------
        void AddGroupSource_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            if (Vm.SelectedGroup is not GroupRow g) { MessageBox.Show("Chọn một nhóm trước.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var dlg = new BulkAddSourcesWindow(g.Name) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddSourcesBulk(g.Id, dlg.ProxyType, dlg.Lines);
        }

        void UngroupSource_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var ids = SelectedIds(GroupSourcesGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            Vm.AssignGroup(null, ids);
        }

        void DeleteGroupSource_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var ids = SelectedIds(GroupSourcesGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            Vm.DeleteSources(ids);
        }

        static void WarnSelect()
            => MessageBox.Show("Chọn dòng cần thao tác.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
