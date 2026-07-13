using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class GroupEditWindow : Window
    {
        public string GroupName { get; private set; } = string.Empty;
        public ProxySourceGroupMatchMode MatchMode { get; private set; } = ProxySourceGroupMatchMode.Or;

        public GroupEditWindow(string? name = null, ProxySourceGroupMatchMode mode = ProxySourceGroupMatchMode.Or)
        {
            InitializeComponent();
            MatchModeBox.ItemsSource = Enum.GetValues<ProxySourceGroupMatchMode>();
            MatchModeBox.SelectedItem = mode;
            NameBox.Text = name ?? string.Empty;
            TitleText.Text = name == null ? "Thêm nhóm" : "Sửa nhóm";
        }

        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                MessageBox.Show("Nhập tên nhóm.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            GroupName = NameBox.Text.Trim();
            MatchMode = (ProxySourceGroupMatchMode)(MatchModeBox.SelectedItem ?? ProxySourceGroupMatchMode.Or);
            DialogResult = true;
        }
    }
}
