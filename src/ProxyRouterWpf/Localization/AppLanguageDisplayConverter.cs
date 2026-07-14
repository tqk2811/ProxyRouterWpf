using System.Globalization;
using System.Windows.Data;

namespace ProxyRouterWpf.Localization
{
    /// <summary>
    /// Display text for an <see cref="AppLanguage"/> option: each language in its own native name,
    /// and <see cref="AppLanguage.System"/> as "Auto (&lt;resolved language name&gt;)".
    /// </summary>
    public sealed class AppLanguageDisplayConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is AppLanguage lang
                ? lang switch
                {
                    AppLanguage.English => "English",
                    AppLanguage.Vietnamese => "Tiếng Việt",
                    _ => $"Auto ({LocalizationManager.SystemLanguageName})",
                }
                : value?.ToString() ?? string.Empty;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
