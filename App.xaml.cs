using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Aplikovat jazyk z nastavení před otevřením hlavního okna
        var lang = SettingsService.Current.Language == "EN"
            ? L10n.Language.EN
            : L10n.Language.CS;

        L10n.Apply(lang);
    }
}
