using System.Globalization;
using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Helpers;
using ProxyRouterWpf.Localization;
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
            UnitBox.ItemsSource = ByteThreshold.Units;
            TypeBox.SelectedItem = existing.FilterType;
            DirBox.SelectedItem = existing.TrafficDirection ?? ProxyTrafficDirection.Both;

            if (existing.FilterType == ProxySourceGroupFilterType.TotalBytes
                && long.TryParse(existing.Filter, NumberStyles.Integer, CultureInfo.InvariantCulture, out long bytes))
            {
                var (text, unit) = ByteThreshold.Split(bytes);
                BytesBox.Text = text;
                UnitBox.SelectedItem = unit;
            }
            else
            {
                FilterBox.Text = existing.Filter;
                UnitBox.SelectedIndex = 0;
            }

            IsNotBox.IsChecked = existing.IsNot;
            UpdateTypeVisibility();
        }

        /// <summary>TotalBytes swaps the free-text pattern box for the direction + threshold inputs.</summary>
        void UpdateTypeVisibility()
        {
            bool total = (ProxySourceGroupFilterType?)TypeBox.SelectedItem == ProxySourceGroupFilterType.TotalBytes;
            DirPanel.Visibility = total ? Visibility.Visible : Visibility.Collapsed;
            BytesPanel.Visibility = total ? Visibility.Visible : Visibility.Collapsed;
            FilterPanel.Visibility = total ? Visibility.Collapsed : Visibility.Visible;
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
                Filter = bytes.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(FilterBox.Text))
                {
                    MessageBox.Show(Loc.S("Str.Dialog.Filter.ContentRequired"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FilterBox.Focus();
                    return;
                }
                TrafficDirection = null;
                Filter = FilterBox.Text.Trim();
            }
            IsNot = IsNotBox.IsChecked == true;
            DialogResult = true;
        }
    }
}
