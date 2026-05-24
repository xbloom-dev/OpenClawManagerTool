using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenClawManager.Services;
using OpenClawManager.Views;

namespace OpenClawManager;

public partial class App : Application
{
    private IHost? _host;

    public static IServiceProvider Services =>
        ((App)Current)._host?.Services
        ?? throw new InvalidOperationException("The application service provider is not initialized.");

    public static T GetService<T>() where T : notnull =>
        Services.GetRequiredService<T>();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices(ConfigureServices)
            .Build();
        _host.Start();

        var environment = Services.GetRequiredService<IAppEnvironment>();
        TerminalControl.EnvironmentOverride = environment;
        TokenService.EnvironmentOverride = environment;

        var settingsService = Services.GetRequiredService<ISettingsService>();

        // Jazyk
        var lang = settingsService.Settings.Language == "EN"
            ? L10n.Language.EN
            : L10n.Language.CS;
        L10n.Apply(lang);

        // Téma — musí být před otevřením oken
        ThemeService.Apply(settingsService.Settings.Theme);

        // Favicon pro všechna okna aplikace — registrujeme globální handler
        // který nastaví ikonu při Loaded eventu každého Window.
        // Varianta B: jeden řádek pokryje MainWindow i všechna budoucí okna.
        try
        {
            var favicon = new BitmapImage(
                new Uri("pack://application:,,,/Resources/app-favicon.ico"));

            EventManager.RegisterClassHandler(
                typeof(Window),
                Window.LoadedEvent,
                new RoutedEventHandler((s, _) =>
                {
                    if (s is Window w)
                        w.Icon = favicon;
                }));
        }
        catch
        {
            // Pokud favicon chybí, okna použijí výchozí ikonu — aplikace funguje dál.
        }

        Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOpenClawManagerServices();
        services.AddTransient<MainWindow>();
    }
}
