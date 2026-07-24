using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Localization;
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

        // Only the host list is frozen while running: its listeners are already bound to ports and
        // each session captured its own source at Start. Routing groups and the unassigned pool are
        // re-read per request (see ProxySession.MyProxyServerHandler), so they stay editable.
        bool BlockIfRunning()
        {
            if (Vm.IsRunning)
            {
                MessageBox.Show(Loc.S("Str.Proxies.StopBeforeEdit"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            return false;
        }

        /// <summary>Blocks only when a move would add to or remove from the host list while running.</summary>
        bool BlockIfTouchesHosts(params Guid?[] buckets)
            => buckets.Any(b => b == ProxySourceGroups.HostGroupId) && BlockIfRunning();

        static List<Guid> SelectedIds(DataGrid grid)
            => grid.SelectedItems.Cast<ProxySourceRow>().Select(r => r.Id).ToList();

        // Right-clicking a row selects it (unless it is already part of a multi-selection) so the
        // context menu acts on the row under the cursor.
        void Row_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && !row.IsSelected)
            {
                if (ItemsControl.ItemsControlFromItemContainer(row) is DataGrid grid)
                    grid.UnselectAll();
                row.IsSelected = true;
            }
        }

        // ---------------- Host sources ----------------
        void AddHost_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var dlg = new BulkAddSourcesWindow { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddSourcesBulk(ProxySourceGroups.HostGroupId, dlg.ProxyType, dlg.Lines);
        }

        void EditHost_Click(object sender, RoutedEventArgs e) => EditSource(HostsGrid);

        void HostToUngrouped_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var ids = SelectedIds(HostsGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            Vm.AssignGroup(null, ids);
        }

        void AssignHost_Click(object sender, RoutedEventArgs e) => AssignToGroup(HostsGrid);

        void DeleteHost_Click(object sender, RoutedEventArgs e) => DeleteSelected(HostsGrid);

        // ---------------- Ungrouped sources ----------------
        void AddUngrouped_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new BulkAddSourcesWindow { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddSourcesBulk(null, dlg.ProxyType, dlg.Lines);
        }

        void EditUngrouped_Click(object sender, RoutedEventArgs e) => EditSource(UngroupedGrid);

        void UngroupedToHost_Click(object sender, RoutedEventArgs e)
        {
            if (BlockIfRunning()) return;
            var ids = SelectedIds(UngroupedGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            Vm.AssignGroup(ProxySourceGroups.HostGroupId, ids);
        }

        void AssignUngrouped_Click(object sender, RoutedEventArgs e) => AssignToGroup(UngroupedGrid);

        void DeleteUngrouped_Click(object sender, RoutedEventArgs e) => DeleteSelected(UngroupedGrid);

        void AssignToGroup(DataGrid grid)
        {
            if (grid == HostsGrid && BlockIfRunning()) return;
            var ids = SelectedIds(grid);
            if (ids.Count == 0) { WarnSelect(); return; }
            var dlg = new AssignGroupWindow(Vm.AllGroups(), ids.Count) { Owner = Owner };
            if (dlg.ShowDialog() != true) return;
            if (BlockIfTouchesHosts(dlg.GroupId)) return;
            Vm.AssignGroup(dlg.GroupId, ids);
        }

        void DeleteSelected(DataGrid grid)
        {
            if (grid == HostsGrid && BlockIfRunning()) return;
            var ids = SelectedIds(grid);
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
            if (grid == HostsGrid && BlockIfRunning()) return;
            if (grid.SelectedItem is not ProxySourceRow row) { WarnSelect(); return; }
            var dlg = new SourceEditWindow(Vm.AllGroups(), row.Source) { Owner = Owner };
            if (dlg.ShowDialog() != true) return;
            if (BlockIfTouchesHosts(dlg.GroupId)) return; // the dialog can move a row into Hosts
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
            if (Vm.SelectedGroup is not GroupRow g) { MessageBox.Show(Loc.S("Str.Proxies.SelectGroupFirst"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information); return; }
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
            if (Vm.SelectedGroup is not GroupRow g) { MessageBox.Show(Loc.S("Str.Proxies.SelectGroupFirst"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var dlg = new BulkAddSourcesWindow(g.Name) { Owner = Owner };
            if (dlg.ShowDialog() == true)
                Vm.AddSourcesBulk(g.Id, dlg.ProxyType, dlg.Lines);
        }

        void UngroupSource_Click(object sender, RoutedEventArgs e)
        {
            var ids = SelectedIds(GroupSourcesGrid);
            if (ids.Count == 0) { WarnSelect(); return; }
            Vm.AssignGroup(null, ids);
        }

        void DeleteGroupSource_Click(object sender, RoutedEventArgs e) => DeleteSelected(GroupSourcesGrid);

        static void WarnSelect()
            => MessageBox.Show(Loc.S("Str.Proxies.SelectRow"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);

        // ================= Drag & drop =================
        // Reorder rows within a grid, move proxies onto a group (assign) or back to ungrouped.

        Point _dragStart;
        DragKind? _candidateKind;
        List<Guid> _candidateIds = new();
        Guid? _candidateGroupId;
        DataGridRow? _mouseDownRow;
        bool _suppressCollapse;
        bool _dragging;
        DropAdorner? _adorner;

        void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _candidateKind = null;
            _suppressCollapse = false;
            _mouseDownRow = null;

            var grid = (DataGrid)sender;
            var src = e.OriginalSource as DependencyObject;

            // Don't start a drag from interactive cell content (Log button, MatchMode combo, edit box).
            if (FindAncestor<ButtonBase>(src) != null || FindAncestor<ComboBox>(src) != null || FindAncestor<TextBox>(src) != null)
                return;

            var row = FindAncestor<DataGridRow>(src);
            if (row == null) return; // header / empty area

            _dragStart = e.GetPosition(null);
            _mouseDownRow = row;

            if (grid == GroupsGrid)
            {
                if (row.Item is not GroupRow g) return;
                _candidateKind = DragKind.Group;
                _candidateIds = new List<Guid> { g.Id };
                _candidateGroupId = null;
                return;
            }

            var ids = grid.SelectedItems.Cast<ProxySourceRow>().Select(r => r.Id).ToList();
            if (row.Item is ProxySourceRow clicked && !ids.Contains(clicked.Id))
                ids = new List<Guid> { clicked.Id };
            _candidateIds = ids;

            if (grid == HostsGrid)
            {
                _candidateKind = DragKind.HostSource;
                _candidateGroupId = ProxySourceGroups.HostGroupId;
            }
            else if (grid == UngroupedGrid)
            {
                _candidateKind = DragKind.UngroupedSource;
                _candidateGroupId = null;
            }
            else
            {
                _candidateKind = DragKind.GroupSource;
                _candidateGroupId = Vm.SelectedGroup?.Id;
            }

            // The host list is the running configuration: selectable while running, but not draggable.
            if (grid == HostsGrid && Vm.IsRunning)
                _candidateKind = null;

            // Keep an existing multi-selection when clicking one of its rows, so the whole set can be dragged.
            if (row.IsSelected && grid.SelectedItems.Count > 1
                && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == 0)
            {
                _suppressCollapse = true;
                e.Handled = true;
            }
        }

        void Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // A plain click (no drag) on a preserved multi-selection collapses it to the clicked row.
            if (_suppressCollapse && _mouseDownRow != null)
            {
                var grid = (DataGrid)sender;
                grid.UnselectAll();
                _mouseDownRow.IsSelected = true;
            }
            _suppressCollapse = false;
            _candidateKind = null;
            _mouseDownRow = null;
        }

        void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging || _candidateKind == null || e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            var grid = (DataGrid)sender;

            // For a normal (non-preserved) selection, honor the current selection at drag time.
            if (!_suppressCollapse && _candidateKind != DragKind.Group)
            {
                var ids = grid.SelectedItems.Cast<ProxySourceRow>().Select(r => r.Id).ToList();
                if (ids.Count > 0) _candidateIds = ids;
            }
            if (_candidateIds.Count == 0) { _candidateKind = null; return; }

            grid.CommitEdit(DataGridEditingUnit.Row, true);

            var data = new DataObject(ProxyDragData.Format, new ProxyDragData(_candidateKind.Value, _candidateIds, _candidateGroupId));
            _dragging = true;
            try { DragDrop.DoDragDrop(grid, data, DragDropEffects.Move); }
            finally { _dragging = false; _candidateKind = null; _suppressCollapse = false; ClearAdorner(); }
        }

        void Grid_DragOver(object sender, DragEventArgs e)
        {
            var grid = (DataGrid)sender;
            e.Handled = true;

            if (e.Data.GetData(ProxyDragData.Format) is not ProxyDragData d
                || (grid == HostsGrid && Vm.IsRunning))
            {
                e.Effects = DragDropEffects.None;
                ClearAdorner();
                return;
            }

            if (IsReorderTarget(grid, d))
            {
                Ensure(grid).SetLine(LineY(grid, ItemsFor(grid), e));
                e.Effects = DragDropEffects.Move;
            }
            else if (grid == GroupsGrid && IsProxyKind(d))
            {
                var row = RowUnderMouse(grid, e);
                if (row?.Item is GroupRow g && g.Id != d.SourceGroupId)
                {
                    var tl = row.TranslatePoint(new Point(0, 0), grid);
                    Ensure(grid).SetHighlight(new Rect(tl, new Size(grid.ActualWidth, row.ActualHeight)));
                    e.Effects = DragDropEffects.Move;
                }
                else { ClearAdorner(); e.Effects = DragDropEffects.None; }
            }
            else if (IsMoveTarget(grid) && IsProxyKind(d))
            {
                // Accepting a proxy from another list moves it into this one.
                Ensure(grid).SetHighlight(new Rect(0, 0, grid.ActualWidth, grid.ActualHeight));
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                ClearAdorner();
                e.Effects = DragDropEffects.None;
            }
        }

        void Grid_DragLeave(object sender, DragEventArgs e) => ClearAdorner();

        void Grid_Drop(object sender, DragEventArgs e)
        {
            ClearAdorner();
            e.Handled = true;
            if (e.Data.GetData(ProxyDragData.Format) is not ProxyDragData d) return;
            var grid = (DataGrid)sender;

            if (grid == HostsGrid) DropOnHosts(d, e);
            else if (grid == UngroupedGrid) DropOnUngrouped(d, e);
            else if (grid == GroupSourcesGrid) DropOnGroupSources(d, e);
            else if (grid == GroupsGrid) DropOnGroups(d, e);
        }

        void DropOnHosts(ProxyDragData d, DragEventArgs e)
        {
            if (Vm.IsRunning) return; // DragOver already rejected it; guard against a stale drop
            if (d.Kind == DragKind.HostSource)
            {
                var current = Vm.HostSources.Select(r => r.Id).ToList();
                var order = BuildReorder(current, d.Ids.ToHashSet(), InsertIndex(HostsGrid, e, Vm.HostSources));
                if (order.SequenceEqual(current)) return;
                Vm.ReorderSources(ProxySourceGroups.HostGroupId, order);
            }
            else if (IsProxyKind(d))
            {
                Vm.AssignGroup(ProxySourceGroups.HostGroupId, d.Ids);
            }
        }

        void DropOnUngrouped(ProxyDragData d, DragEventArgs e)
        {
            if (d.Kind == DragKind.UngroupedSource)
            {
                var current = Vm.UngroupedSources.Select(r => r.Id).ToList();
                var order = BuildReorder(current, d.Ids.ToHashSet(), InsertIndex(UngroupedGrid, e, Vm.UngroupedSources));
                if (order.SequenceEqual(current)) return;
                Vm.ReorderSources(null, order);
            }
            else if (IsProxyKind(d))
            {
                if (BlockIfTouchesHosts(d.SourceGroupId)) return;
                Vm.AssignGroup(null, d.Ids);
            }
        }

        void DropOnGroupSources(ProxyDragData d, DragEventArgs e)
        {
            if (Vm.SelectedGroup is not GroupRow sel) return;

            if (d.Kind == DragKind.GroupSource && d.SourceGroupId == sel.Id)
            {
                var current = Vm.GroupSources.Select(r => r.Id).ToList();
                var order = BuildReorder(current, d.Ids.ToHashSet(), InsertIndex(GroupSourcesGrid, e, Vm.GroupSources));
                if (order.SequenceEqual(current)) return;
                Vm.ReorderSources(sel.Id, order);
            }
            else if (IsProxyKind(d))
            {
                if (BlockIfTouchesHosts(d.SourceGroupId)) return;
                Vm.AssignGroup(sel.Id, d.Ids);
            }
        }

        void DropOnGroups(ProxyDragData d, DragEventArgs e)
        {
            if (d.Kind == DragKind.Group)
            {
                var current = Vm.Groups.Select(g => g.Id).ToList();
                var order = BuildReorder(current, d.Ids.ToHashSet(), InsertIndex(GroupsGrid, e, Vm.Groups));
                if (order.SequenceEqual(current)) return;
                Vm.ReorderGroups(order);
            }
            else if (IsProxyKind(d))
            {
                if (RowUnderMouse(GroupsGrid, e)?.Item is not GroupRow g) return;
                if (g.Id == d.SourceGroupId) return;
                if (BlockIfTouchesHosts(d.SourceGroupId)) return;
                Vm.AssignGroup(g.Id, d.Ids);
            }
        }

        // ---- drag-drop helpers ----
        static bool IsProxyKind(ProxyDragData d)
            => d.Kind is DragKind.HostSource or DragKind.UngroupedSource or DragKind.GroupSource;

        /// <summary>Grids that accept a proxy dropped anywhere on them, moving it into their bucket.</summary>
        bool IsMoveTarget(DataGrid grid)
            => grid == UngroupedGrid
            || grid == HostsGrid
            || (grid == GroupSourcesGrid && Vm.SelectedGroup != null);

        bool IsReorderTarget(DataGrid grid, ProxyDragData d)
        {
            if (grid == HostsGrid) return d.Kind == DragKind.HostSource;
            if (grid == UngroupedGrid) return d.Kind == DragKind.UngroupedSource;
            if (grid == GroupSourcesGrid) return d.Kind == DragKind.GroupSource && Vm.SelectedGroup != null && d.SourceGroupId == Vm.SelectedGroup.Id;
            if (grid == GroupsGrid) return d.Kind == DragKind.Group;
            return false;
        }

        static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject
        {
            while (d != null && d is not T)
                d = d is Visual || d is System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
            return d as T;
        }

        static DataGridRow? RowUnderMouse(DataGrid grid, DragEventArgs e)
            => FindAncestor<DataGridRow>(grid.InputHitTest(e.GetPosition(grid)) as DependencyObject);

        /// <summary>The bound collection backing a grid — used so reorder geometry matches the visible rows.</summary>
        System.Collections.IList ItemsFor(DataGrid grid)
            => grid == HostsGrid ? Vm.HostSources
             : grid == UngroupedGrid ? Vm.UngroupedSources
             : grid == GroupSourcesGrid ? (System.Collections.IList)Vm.GroupSources
             : Vm.Groups;

        // Insert position (0..Count) for a reorder, from the cursor's Y against each row's midpoint.
        // Purely geometric (not hit-test based) so dropping at the very top/bottom edge resolves
        // correctly and always agrees with the insertion line drawn by LineY.
        static int InsertIndex(DataGrid grid, DragEventArgs e, System.Collections.IList items)
        {
            double y = e.GetPosition(grid).Y;
            for (int i = 0; i < items.Count; i++)
            {
                if (grid.ItemContainerGenerator.ContainerFromIndex(i) is DataGridRow r)
                {
                    double top = r.TranslatePoint(new Point(0, 0), grid).Y;
                    if (y < top + r.ActualHeight / 2) return i;
                }
            }
            return items.Count;
        }

        static List<Guid> BuildReorder(IReadOnlyList<Guid> current, HashSet<Guid> dragged, int insertIndex)
        {
            Guid? anchor = insertIndex < current.Count ? current[insertIndex] : null;
            var result = current.Where(id => !dragged.Contains(id)).ToList();
            int target = anchor is Guid a ? result.IndexOf(a) : result.Count;
            if (target < 0) target = result.Count;
            result.InsertRange(target, current.Where(dragged.Contains));
            return result;
        }

        // Draws the insertion line at the same boundary InsertIndex would pick, so what the user
        // sees is exactly where the drop lands.
        static double LineY(DataGrid grid, System.Collections.IList items, DragEventArgs e)
        {
            int idx = InsertIndex(grid, e, items);
            if (idx < items.Count && grid.ItemContainerGenerator.ContainerFromIndex(idx) is DataGridRow r)
                return r.TranslatePoint(new Point(0, 0), grid).Y;
            for (int i = items.Count - 1; i >= 0; i--)
                if (grid.ItemContainerGenerator.ContainerFromIndex(i) is DataGridRow last)
                    return last.TranslatePoint(new Point(0, last.ActualHeight), grid).Y;
            return 0;
        }

        DropAdorner Ensure(DataGrid grid)
        {
            if (_adorner != null && !ReferenceEquals(_adorner.AdornedElement, grid))
                ClearAdorner();
            if (_adorner == null)
            {
                _adorner = new DropAdorner(grid);
                AdornerLayer.GetAdornerLayer(grid)?.Add(_adorner);
            }
            return _adorner;
        }

        void ClearAdorner()
        {
            if (_adorner != null)
            {
                AdornerLayer.GetAdornerLayer((UIElement)_adorner.AdornedElement)?.Remove(_adorner);
                _adorner = null;
            }
        }
    }
}
