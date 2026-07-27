using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Helpers;
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
            UnitBox.ItemsSource = ByteThreshold.Units;
            TypeBox.SelectedItem = ProxySourceGroupFilterType.Wildcard;
            DirBox.SelectedItem = ProxyTrafficDirection.Both;
            UnitBox.SelectedIndex = 0;
            UpdateTypeVisibility();
        }

        /// <summary>TotalBytes is a single numeric threshold, so it replaces the one-per-line box.</summary>
        void UpdateTypeVisibility()
        {
            bool total = (ProxySourceGroupFilterType?)TypeBox.SelectedItem == ProxySourceGroupFilterType.TotalBytes;
            DirPanel.Visibility = total ? Visibility.Visible : Visibility.Collapsed;
            DirColumn.Width = total ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            BytesPanel.Visibility = total ? Visibility.Visible : Visibility.Collapsed;
            LinesPanel.Visibility = total ? Visibility.Collapsed : Visibility.Visible;
        }

        void TypeBox_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => UpdateTypeVisibility();
        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            FilterType = (ProxySourceGroupFilterType)(TypeBox.SelectedItem ?? ProxySourceGroupFilterType.Wildcard);
            if (FilterType == ProxySourceGroupFilterType.TotalBytes)
            {
                if (!ByteThreshold.TryCompose(BytesBox.Text, UnitBox.SelectedItem as ByteUnit, out long bytes))
                {
                    MessageBox.Show(Loc.S("Str.Dialog.Filter.BytesRequired"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BytesBox.Focus();
                    return;
                }
                TrafficDirection = (ProxyTrafficDirection)(DirBox.SelectedItem ?? ProxyTrafficDirection.Both);
                Lines = bytes.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(LinesBox.Text))
                {
                    MessageBox.Show(Loc.S("Str.Dialog.BulkSource.LinesRequired"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                    LinesBox.Focus();
                    return;
                }
                TrafficDirection = null;
                Lines = LinesBox.Text;
            }
            IsNot = IsNotBox.IsChecked == true;
            DialogResult = true;
        }
    }
}
