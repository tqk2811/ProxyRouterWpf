using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProxyRouterWpf.Helpers;
using ProxyRouterWpf.Models;

namespace ProxyRouterWpf.Views
{
    public partial class LogDetailWindow : Window
    {
        public LogDetailWindow(ProxyTunnelLogVM item)
        {
            InitializeComponent();
            DataContext = item;
            Build(item);
        }

        void Build(ProxyTunnelLogVM x)
        {
            var dur = x.EndAt - x.StartAt;
            AddRow(TunnelFields, "TunnelId", x.TunnelId.ToString());
            AddRow(TunnelFields, "Start", Local(x.StartAt));
            AddRow(TunnelFields, "End", Local(x.EndAt));
            AddRow(TunnelFields, "Duration", dur.ToString(@"hh\:mm\:ss\.fff"));
            AddRow(TunnelFields, "Outcome", x.Outcome.ToString());
            AddRow(TunnelFields, "Client", $"{x.ClientIPAddress}:{x.ClientPort}");
            AddRow(TunnelFields, "Server", $"{x.ServerIPAddress}:{x.ServerPort}");
            AddRow(TunnelFields, "Protocol", x.ClientProtocol?.ToString() ?? "—");
            AddRow(TunnelFields, "Target", x.TargetHost == null ? "—" : $"{x.TargetHost}:{x.TargetPort}");
            AddRow(TunnelFields, "Upload", $"{BytesFormatter.FormatBytes(x.TotalBytesUpload)}  ({x.TotalBytesUpload:N0} B)");
            AddRow(TunnelFields, "Download", $"{BytesFormatter.FormatBytes(x.TotalBytesDownload)}  ({x.TotalBytesDownload:N0} B)");
            if (x.RejectReason.HasValue)
                AddRow(TunnelFields, "RejectReason", x.RejectReason.ToString()!);

            AddRow(AuthFields, "Method", x.AuthMethod?.ToString() ?? "—");
            AddRow(AuthFields, "Username", x.AuthUserName ?? "—");
            if (!string.IsNullOrEmpty(x.AuthPassword))
                AddRow(AuthFields, "Password", x.AuthPassword!, danger: true);

            AddRow(RoutingFields, "Decision", x.RoutingDecision?.ToString() ?? "—");
            AddRow(RoutingFields, "Matched filter", x.MatchedFilterType == null ? "—" : $"[{x.MatchedFilterType}] {x.MatchedFilterPattern}");
            AddRow(RoutingFields, "Group", x.MatchedGroupName ?? "—");
            AddRow(RoutingFields, "Picked source", x.PickedSourceAddress == null ? "Direct (không upstream)" : $"{x.PickedSourceAddress}:{x.PickedSourcePort}");
            AddRow(RoutingFields, "Picked type", x.PickedSourceProxyType?.ToString() ?? "—");
            AddRow(RoutingFields, "Picked user", x.PickedSourceUserName ?? "—");
        }

        static string Local(DateTime dt)
        {
            var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
            return local.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        void AddRow(Panel host, string label, string value, bool danger = false)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var l = new TextBlock { Text = label, FontSize = 12 };
            l.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text.Secondary");
            Grid.SetColumn(l, 0);

            var v = new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap };
            if (danger) v.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Danger");
            Grid.SetColumn(v, 1);

            grid.Children.Add(l);
            grid.Children.Add(v);
            host.Children.Add(grid);
        }

        void Header_Drag(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); }
        void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
