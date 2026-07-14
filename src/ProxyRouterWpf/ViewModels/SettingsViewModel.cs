using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Themes;

namespace ProxyRouterWpf.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        readonly AppServices _svc;

        public SettingsViewModel(AppServices svc)
        {
            _svc = svc;
            var s = _svc.Config.Config.Settings;
            logCapacity = s.LogCapacity;
            selectedTheme = ThemeManager.Parse(s.Theme);
        }

        public List<ThemeMode> ThemeOptions { get; } = new() { ThemeMode.System, ThemeMode.Light, ThemeMode.Dark };

        [ObservableProperty] ThemeMode selectedTheme;
        [ObservableProperty] int logCapacity;

        partial void OnSelectedThemeChanged(ThemeMode value)
        {
            ThemeManager.Apply(value);
            _svc.Config.Config.Settings.Theme = value.ToString();
            _svc.Config.Save();
            ThemeChanged?.Invoke();
        }

        /// <summary>Raised after the theme changes so the shell can refresh its glyph.</summary>
        public Action? ThemeChanged { get; set; }

        [RelayCommand]
        void Save()
        {
            int cap = LogCapacity < 100 ? 100 : (LogCapacity > 200000 ? 200000 : LogCapacity);
            LogCapacity = cap;
            _svc.LogStore.SetCapacity(cap);
            var s = _svc.Config.Config.Settings;
            s.LogCapacity = cap;
            _svc.Config.Save();
            MessageBox.Show("Đã lưu cài đặt.", "ProxyRouter", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
