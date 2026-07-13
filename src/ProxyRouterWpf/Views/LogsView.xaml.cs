using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;
using ProxyRouterWpf.ViewModels;

namespace ProxyRouterWpf.Views
{
    public partial class LogsView : UserControl
    {
        public LogsView()
        {
            InitializeComponent();
        }

        LogsViewModel Vm => (LogsViewModel)DataContext;

        void LogGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true; // server-side sort; suppress the default in-place sort
            if (Enum.TryParse<ProxyTunnelLogSortBy>(e.Column.SortMemberPath, out var sortBy))
                Vm.SetSort(sortBy);
        }

        void ViewDetail_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (((System.Windows.FrameworkElement)sender).DataContext is ProxyTunnelLogListItemVM item)
                OpenDetail(item.Id);
        }

        void LogGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (LogGrid.SelectedItem is ProxyTunnelLogListItemVM item)
                OpenDetail(item.Id);
        }

        void OpenDetail(long id)
        {
            var detail = Vm.GetDetail(id);
            if (detail == null) return;
            var win = new LogDetailWindow(detail) { Owner = System.Windows.Window.GetWindow(this) };
            win.ShowDialog();
        }
    }
}
