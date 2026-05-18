using System.IO;
using OpenClawManager.Services;
using System.Text.Json.Serialization;

namespace OpenClawManager.Models;

/// <summary>
/// Vizuální téma aplikace (v0.5+).
/// Legacy = původní tmavé WPF téma s ASCII ART splashem.
/// Modern = standardní Modern paleta (C) s video/PNG splash overlay.
/// Dark = moderní tmavé téma.
/// ModernLight = moderní světlé téma odvozené z Dark layoutu.
/// </summary>
public enum AppTheme
{
    Legacy,
    Modern,
    Dark,
    ModernLight,
    HighContrast,
    CrabCute,
    Compact = CrabCute
}

/// <summary>
/// Persistentní nastavení aplikace — ukládá se do %APPDATA%\OpenClawManager\settings.json.
/// </summary>
public class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>Verze schématu nastavení pro budoucí migrace.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Cesta k OpenClaw konfiguraci (~\.openclaw)</summary>
    public string OpenClawPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".openclaw");

    /// <summary>Cesta k OpenClaw temp složce (Gateway logy)</summary>
    public string TempPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Temp", "openclaw");

    /// <summary>Příkaz pro spuštění openclaw (default: openclaw = PATH lookup)</summary>
    public string OpenClawCommand { get; set; } = "openclaw";

    /// <summary>Pracovní adresář pro PowerShell (prázdný = výchozí)</summary>
    public string PowerShellWorkingDir { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "Roaming", "npm");

    /// <summary>Agenti pro sessions.json cleanup v Cleaning Tool</summary>
    public List<string> CleanupAgents { get; set; } =
        new() { "main", "researcher", "executive", "safety" };

    /// <summary>Cesta k Token Manager secrets.json vaultu</summary>
    public string TokenManagerSecretsPath { get; set; } = GetDefaultTokenManagerSecretsPath();

    /// <summary>Jazyk UI — "CS" nebo "EN"</summary>
    public string Language { get; set; } = "CS";

    /// <summary>Auto-scroll v Log aplikace</summary>
    public bool AutoScrollAppLog { get; set; } = true;

    // ════════════════════════════════════════════════════════════════════════
    // v0.5 — Vzhled
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vizuální téma aplikace. Výchozí: Legacy (zachování původního chování v0.4).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppTheme Theme { get; set; } = AppTheme.Legacy;

    /// <summary>
    /// Zobrazit splash screen video při startu (jen v Modern theme).
    /// Pokud false nebo splash.mp4 chybí — zobrazí se splash.png fallback.
    /// </summary>
    public bool UseSplashVideo { get; set; } = true;

    /// <summary>
    /// Volitelny scanline efekt pri stisku tlacitek v modernich tematech.
    /// Legacy tema zustava bez zasahu.
    /// </summary>
    public bool UseButtonScanlineEffect { get; set; } = false;

    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Vrátí cestu k dnešnímu Gateway log souboru.
    /// </summary>
    public string GetTodayGatewayLogPath()
    {
        var fileName = $"openclaw-{DateTime.Now:yyyy-MM-dd}.log";
        return Path.Combine(TempPath, fileName);
    }

    private static string GetDefaultTokenManagerSecretsPath() => TokenService.GetDefaultVaultPath();
}
