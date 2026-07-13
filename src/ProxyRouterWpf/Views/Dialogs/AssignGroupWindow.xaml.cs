using System.Windows;
using System.Windows.Input;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Views.Dialogs
{
    public partial class AssignGroupWindow : Window
    {
        public Guid? GroupId { get; private set; }

        public AssignGroupWindow(IReadOnlyList<ProxySourceGroupVM> groups, int count)
        {
            InitializeComponent();
            var choices = new List<GroupChoice> { new() { Label = "(Ungrouped)", Id = null } };
            foreach (var g in groups)
                choices.Add(new GroupChoice { Label = g.Name, Id = g.Id });
            GroupBox.ItemsSource = choices;
            GroupBox.SelectedItem = choices[0];
            CountLabel.Content = $"Gán {count} proxy vào nhóm:";
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
