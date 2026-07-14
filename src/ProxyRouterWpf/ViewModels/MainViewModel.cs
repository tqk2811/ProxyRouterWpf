using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Localization;
using ProxyRouterWpf.Themes;

namespace ProxyRouterWpf.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        readonly AppServices _svc;

        public MainViewModel(AppServices svc)
        {
            _svc = svc;
            Proxies = new ProxiesViewModel(svc);
            Logs = new LogsViewModel(svc);
            Bandwidth = new BandwidthViewModel(svc);
            Settings = new SettingsViewModel(svc);

            Proxies.ShowLogForSource = src =>
            {
                Logs.FilterByPickedSource(src.Address, src.Port, src.ProxyType);
                SelectedTabIndex = 1;
            };
            Settings.ThemeChanged = UpdateThemeGlyph;

            LocalizationManager.LanguageChanged += OnLanguageChanged;

            UpdateThemeGlyph();
            Bandwidth.Start();
        }

        void OnLanguageChanged()
        {
            UpdateThemeGlyph();
            Proxies.OnLanguageChanged();
            Logs.OnLanguageChanged();
            Bandwidth.OnLanguageChanged();
        }

        public ProxiesViewModel Proxies { get; }
        public LogsViewModel Logs { get; }
        public BandwidthViewModel Bandwidth { get; }
        public SettingsViewModel Settings { get; }

        [ObservableProperty] int selectedTabIndex;
        [ObservableProperty] string themeGlyph = char.ConvertFromUtf32(0xE713);
        [ObservableProperty] string themeTooltip = Loc.S("Str.Theme.System");

        [RelayCommand]
        void ToggleTheme()
        {
            var mode = ThemeManager.Cycle();
            _svc.Config.Config.Settings.Theme = mode.ToString();
            _svc.Config.Save();
            Settings.SelectedTheme = mode;
            UpdateThemeGlyph();
        }

        void UpdateThemeGlyph()
        {
            // Segoe MDL2 Assets: E713 = Settings (System), E706 = Brightness (Light), E708 = QuietHours (Dark).
            (int code, string tip) = ThemeManager.CurrentMode switch
            {
                ThemeMode.Light => (0xE706, Loc.S("Str.Theme.Light")),
                ThemeMode.Dark => (0xE708, Loc.S("Str.Theme.Dark")),
                _ => (0xE713, Loc.S("Str.Theme.System")),
            };
            ThemeGlyph = char.ConvertFromUtf32(code);
            ThemeTooltip = tip;
        }
    }
}
