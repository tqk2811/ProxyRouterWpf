using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class FilterEditWindow : Window
    {
        public ProxySourceGroupFilterType FilterType { get; private set; }
        public ProxyTrafficDirection? TrafficDirection { get; private set; }
        public string Filter { get; private set; } = string.Empty;
        public bool IsNot { get; private set; }

        public FilterEditWindow(ProxySourceGroupFilterVM existing)
        {
            InitializeComponent();
            TypeBox.ItemsSource = Enum.GetValues<ProxySourceGroupFilterType>();
            DirBox.ItemsSource = Enum.GetValues<ProxyTrafficDirection>();
            TypeBox.SelectedItem = existing.FilterType;
            DirBox.SelectedItem = existing.TrafficDirection ?? ProxyTrafficDirection.Both;
            FilterBox.Text = existing.Filter;
            IsNotBox.IsChecked = existing.IsNot;
            UpdateDirVisibility();
        }

        void UpdateDirVisibility()
        {
            bool total = (ProxySourceGroupFilterType?)TypeBox.SelectedItem == ProxySourceGroupFilterType.TotalBytes;
            DirPanel.IsEnabled = total;
            FilterLabel.Content = total ? "Số bytes ngưỡng (>=)" : "Host pattern";
        }

        void TypeBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateDirVisibility();
        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FilterBox.Text))
            {
                MessageBox.Show("Nhập nội dung filter.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FilterType = (ProxySourceGroupFilterType)(TypeBox.SelectedItem ?? ProxySourceGroupFilterType.Wildcard);
            TrafficDirection = FilterType == ProxySourceGroupFilterType.TotalBytes
                ? (ProxyTrafficDirection)(DirBox.SelectedItem ?? ProxyTrafficDirection.Both)
                : null;
            Filter = FilterBox.Text.Trim();
            IsNot = IsNotBox.IsChecked == true;
            DialogResult = true;
        }
    }
}
