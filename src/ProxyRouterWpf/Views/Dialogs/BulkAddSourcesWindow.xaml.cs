using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class BulkAddSourcesWindow : Window
    {
        public ProxyType ProxyType { get; private set; }
        public string Lines { get; private set; } = string.Empty;

        public BulkAddSourcesWindow(string? targetGroupName = null)
        {
            InitializeComponent();
            TypeBox.ItemsSource = Enum.GetValues<ProxyType>();
            TypeBox.SelectedItem = ProxyType.Http;
            if (targetGroupName != null)
                TitleText.Text = $"Thêm proxy vào nhóm: {targetGroupName}";
        }

        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LinesBox.Text))
            {
                MessageBox.Show("Nhập ít nhất một dòng.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ProxyType = (ProxyType)(TypeBox.SelectedItem ?? ProxyType.Http);
            Lines = LinesBox.Text;
            DialogResult = true;
        }
    }
}
