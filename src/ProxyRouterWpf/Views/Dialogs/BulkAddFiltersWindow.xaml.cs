using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Localization;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class BulkAddFiltersWindow : Window
    {
        public ProxySourceGroupFilterType FilterType { get; private set; }
        public ProxyTrafficDirection? TrafficDirection { get; private set; }
        public bool IsNot { get; private set; }
        public string Lines { get; private set; } = string.Empty;

        public BulkAddFiltersWindow()
        {
            InitializeComponent();
            TypeBox.ItemsSource = Enum.GetValues<ProxySourceGroupFilterType>();
            DirBox.ItemsSource = Enum.GetValues<ProxyTrafficDirection>();
            TypeBox.SelectedItem = ProxySourceGroupFilterType.Wildcard;
            DirBox.SelectedItem = ProxyTrafficDirection.Both;
            UpdateDir();
        }

        void UpdateDir()
        {
            bool total = (ProxySourceGroupFilterType?)TypeBox.SelectedItem == ProxySourceGroupFilterType.TotalBytes;
            DirPanel.IsEnabled = total;
        }

        void TypeBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateDir();
        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LinesBox.Text))
            {
                MessageBox.Show(Loc.S("Str.Dialog.BulkSource.LinesRequired"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            FilterType = (ProxySourceGroupFilterType)(TypeBox.SelectedItem ?? ProxySourceGroupFilterType.Wildcard);
            TrafficDirection = FilterType == ProxySourceGroupFilterType.TotalBytes
                ? (ProxyTrafficDirection)(DirBox.SelectedItem ?? ProxyTrafficDirection.Both)
                : null;
            IsNot = IsNotBox.IsChecked == true;
            Lines = LinesBox.Text;
            DialogResult = true;
        }
    }
}
