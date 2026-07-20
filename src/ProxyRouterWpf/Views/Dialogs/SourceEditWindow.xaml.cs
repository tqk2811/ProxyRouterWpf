using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Localization;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class SourceEditWindow : Window
    {
        public Guid? GroupId { get; private set; }
        public ProxyType ProxyType { get; private set; }
        public string Address { get; private set; } = string.Empty;
        public int Port { get; private set; }
        public string? UserName { get; private set; }
        public string? Password { get; private set; }

        public SourceEditWindow(IReadOnlyList<ProxySourceGroupVM> groups, ProxySourceVM existing)
        {
            InitializeComponent();

            var choices = new List<GroupChoice>
            {
                new() { Label = Loc.S("Str.Dialog.Source.Hosts"), Id = ProxySourceGroups.HostGroupId },
                new() { Label = Loc.S("Str.Dialog.Source.Ungrouped"), Id = null },
            };
            foreach (var g in groups)
                choices.Add(new GroupChoice { Label = g.Name, Id = g.Id });
            GroupBox.ItemsSource = choices;
            GroupBox.SelectedItem = choices.FirstOrDefault(c => c.Id == existing.GroupId)
                ?? choices.First(c => c.Id == null);

            TypeBox.ItemsSource = Enum.GetValues<ProxyType>();
            TypeBox.SelectedItem = existing.ProxyType;

            AddressBox.Text = existing.Address;
            PortBox.Text = existing.Port.ToString();
            UserBox.Text = existing.UserName ?? string.Empty;
            PassBox.Text = existing.Password ?? string.Empty;
        }

        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortBox.Text.Trim(), out var port) || port < 1 || port > 65535)
            {
                MessageBox.Show(Loc.S("Str.Dialog.Source.PortInvalid"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(AddressBox.Text))
            {
                MessageBox.Show(Loc.S("Str.Dialog.Source.AddressRequired"), "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            GroupId = (GroupBox.SelectedItem as GroupChoice)?.Id;
            ProxyType = (ProxyType)(TypeBox.SelectedItem ?? ProxyType.Http);
            Address = AddressBox.Text.Trim();
            Port = port;
            UserName = string.IsNullOrWhiteSpace(UserBox.Text) ? null : UserBox.Text.Trim();
            Password = string.IsNullOrWhiteSpace(PassBox.Text) ? null : PassBox.Text;
            DialogResult = true;
        }
    }
}
