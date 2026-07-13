using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProxyRouterWpf.Configuration;
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

            UpdateThemeGlyph();
            Bandwidth.Start();
        }

        public ProxiesViewModel Proxies { get; }
        public LogsViewModel Logs { get; }
        public BandwidthViewModel Bandwidth { get; }
        public SettingsViewModel Settings { get; }

        [ObservableProperty] int selectedTabIndex;
        [ObservableProperty] string themeGlyph = char.ConvertFromUtf32(0xE713);
        [ObservableProperty] string themeTooltip = "Theme: System";

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
                ThemeMode.Light => (0xE706, "Theme: Light (bấm để đổi)"),
                ThemeMode.Dark => (0xE708, "Theme: Dark (bấm để đổi)"),
                _ => (0xE713, "Theme: System (bấm để đổi)"),
            };
            ThemeGlyph = char.ConvertFromUtf32(code);
            ThemeTooltip = tip;
        }
    }
}
