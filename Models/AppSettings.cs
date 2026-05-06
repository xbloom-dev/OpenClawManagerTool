using System.IO;

namespace OpenClawManager.Models;

/// <summary>
/// Konfigurovatelná nastavení aplikace.
/// Persistuje se jako JSON v %APPDATA%\OpenClawManager\settings.json.
///
/// Default hodnoty:
/// - Cesty k OpenClaw složkám se odvozují z aktuálního uživatelského profilu (USERPROFILE)
/// - 'openclaw' bez plné cesty = PATH lookup (npm globální instalace)
/// - PowerShell pracovní adresář: E:\OpenClaw (kde běžně OpenClaw setup leží)
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Hlavní složka OpenClaw setupu uživatele.
    /// Default: %USERPROFILE%\.openclaw
    /// </summary>
    public string OpenClawPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".openclaw");

    /// <summary>
    /// Temp složka pro Gateway logy (denní log soubory).
    /// Default: %LOCALAPPDATA%\Temp\openclaw
    /// </summary>
    public string TempPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "Temp", "openclaw");

    /// <summary>
    /// Cesta k openclaw příkazu.
    /// Default: "openclaw" — předpokládá PATH lookup (npm global bin v PATH).
    /// Pokud uživatel chce explicitní cestu, např. C:\Users\test\AppData\Roaming\npm\openclaw.cmd
    /// </summary>
    public string OpenClawCommand { get; set; } = "openclaw";

    /// <summary>
    /// Pracovní adresář pro otevíraný PowerShell.
    /// Default: E:\OpenClaw (typické umístění OpenClaw setupu na BlackStation)
    /// </summary>
    public string PowerShellWorkingDir { get; set; } = @"E:\OpenClaw";

    /// <summary>
    /// Vrátí cestu k aktuálnímu Gateway log souboru pro dnešní datum.
    /// Formát: openclaw-YYYY-MM-DD.log
    /// </summary>
    public string GetTodayGatewayLogPath() =>
        Path.Combine(TempPath, $"openclaw-{DateTime.Now:yyyy-MM-dd}.log");
}
