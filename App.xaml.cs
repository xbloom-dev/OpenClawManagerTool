using System.Windows;
using System.Windows.Media.Imaging;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Jazyk
        var lang = SettingsService.Current.Language == "EN"
            ? L10n.Language.EN
            : L10n.Language.CS;
        L10n.Apply(lang);

        // Téma — musí být před otevřením oken
        ThemeService.Apply(SettingsService.Current.Theme);

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
    }
}
