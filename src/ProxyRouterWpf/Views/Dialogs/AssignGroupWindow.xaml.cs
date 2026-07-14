using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Localization;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class AssignGroupWindow : Window
    {
        public Guid? GroupId { get; private set; }

        public AssignGroupWindow(IReadOnlyList<ProxySourceGroupVM> groups, int count)
        {
            InitializeComponent();
            var choices = new List<GroupChoice> { new() { Label = Loc.S("Str.Dialog.Source.Ungrouped"), Id = null } };
            foreach (var g in groups)
                choices.Add(new GroupChoice { Label = g.Name, Id = g.Id });
            GroupBox.ItemsSource = choices;
            GroupBox.SelectedItem = choices[0];
            CountLabel.Content = Loc.F("Str.Dialog.Assign.Count", count);
        }

        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        void Ok_Click(object sender, RoutedEventArgs e)
        {
            GroupId = (GroupBox.SelectedItem as GroupChoice)?.Id;
            DialogResult = true;
        }
    }
}
