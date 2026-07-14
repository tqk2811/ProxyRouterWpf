using System.Windows;
using ProxyRouterWpf.Configuration;
using ProxyRouterWpf.Localization;
using ProxyRouterWpf.Themes;
using ProxyRouterWpf.ViewModels;

namespace ProxyRouterWpf
{
    public partial class App : Application
    {
        AppServices? _services;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _services = new AppServices();
            _services.StartBackground();

            LocalizationManager.Apply(LocalizationManager.Parse(_services.Config.Config.Settings.Language));
            ThemeManager.Apply(ThemeManager.Parse(_services.Config.Config.Settings.Theme));

            var vm = new MainViewModel(_services);
            var window = new MainWindow { DataContext = vm };
            MainWindow = window;
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try { _services?.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
            catch { /* best effort */ }
            base.OnExit(e);
        }
    }
}
