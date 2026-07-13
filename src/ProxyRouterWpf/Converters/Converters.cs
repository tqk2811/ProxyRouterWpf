using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ProxyRouterWpf.Enums;
using ProxyRouterWpf.Helpers;

namespace ProxyRouterWpf.Converters
{
    /// <summary>long / long? bytes -> "1.50 M".</summary>
    public sealed class BytesConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            long bytes = value switch
            {
                long l => l,
                int i => i,
                _ => 0,
            };
            return BytesFormatter.FormatBytes(bytes);
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    /// <summary>Bytes/second rate -> "1.50 MB/s".</summary>
    public sealed class RateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => BytesFormatter.FormatRate(value is long l ? l : 0);
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    /// <summary>null/empty -> "—".</summary>
    public sealed class DashConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var s = value?.ToString();
            return string.IsNullOrEmpty(s) ? "—" : s;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    /// <summary>UTC DateTime -> local "yyyy-MM-dd HH:mm:ss".</summary>
    public sealed class LocalTimeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                var local = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
                var fmt = parameter as string ?? "yyyy-MM-dd HH:mm:ss";
                return local.ToString(fmt, CultureInfo.InvariantCulture);
            }
            return string.Empty;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    /// <summary>ProxyTunnelOutcome -> status brush.</summary>
    public sealed class OutcomeBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string key = value is ProxyTunnelOutcome o
                ? o switch
                {
                    ProxyTunnelOutcome.Resolved => "Brush.Success",
                    ProxyTunnelOutcome.ClientRejected => "Brush.Danger",
                    ProxyTunnelOutcome.AuthRejected => "Brush.Danger",
                    ProxyTunnelOutcome.RequestFailed => "Brush.Warning",
                    ProxyTunnelOutcome.RouteFailed => "Brush.Warning",
                    _ => "Brush.Text.Secondary",
                }
                : "Brush.Text.Secondary";
            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public sealed class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool empty = value is null || (value is string s && string.IsNullOrEmpty(s));
            bool visible = Invert ? empty : !empty;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool b = value is bool v && v;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public sealed class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !(value is bool b && b);
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !(value is bool b && b);
    }
}
