using System.Globalization;
using System.Windows.Data;

namespace ProxyRouterWpf.Localization
{
    /// <summary>Binds an enum (or null) to its localized display text. Null -> "—".</summary>
    public sealed class EnumLocalizeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => LocalizationManager.EnumText(value);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
