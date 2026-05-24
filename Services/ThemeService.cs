// Services/ThemeService.cs
// Centrální správa vizuálních témat (v0.5+)
// ═══════════════════════════════════════════════════════════════════════════
// Architektura pro N témat:
//
//   AppTheme enum       → identifikátor tématu (AppSettings.Theme)
//   ThemeService        → Apply(), ThemeChanged event, ResourceDictionary swap
//   Resources/Themes/   → Theme.Legacy.xaml, Theme.Modern.xaml, ...
//   Resources/Icons/    → Legacy/ (prázdno), Modern/*.png, ...
//
// Přidání nového tématu:
//   1. Přidat hodnotu do AppTheme enum (AppSettings.cs)
//   2. Vytvořit Theme.{Název}.xaml v Resources/Themes/
//   3. Přidat složku Resources/Icons/{Název}/ s ikonami
//   4. Registrovat v _themeResourcePaths níže
// ═══════════════════════════════════════════════════════════════════════════

using System.Windows;
using System.Windows.Media;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

public static class ThemeService
{
    public sealed record ThemeMetadata(
        string Name,
        string Variant,
        string IconSet,
        string PaletteFamily,
        string ButtonInteraction);

    // ── Event — přihlásit se v MainWindow ────────────────────────────────────
    /// <summary>
    /// Emitováno po úspěšném přepnutí tématu.
    /// MainWindow se přihlašuje v konstruktoru: ThemeService.ThemeChanged += ApplyThemeToUi
    /// </summary>
    public static event Action<AppTheme>? ThemeChanged;

    // ── Registr ResourceDictionary cest ──────────────────────────────────────
    // Přidat nové téma sem + vytvořit odpovídající .xaml soubor.
    private static readonly Dictionary<AppTheme, string> _themeResourcePaths = new()
    {
        { AppTheme.Legacy, "/Resources/Themes/Theme.Legacy.xaml" },
        { AppTheme.Modern, "/Resources/Themes/Theme.Modern.xaml" },
        { AppTheme.StandardDark, "/Resources/Themes/Theme.StandardDark.xaml" },
        { AppTheme.Dark, "/Resources/Themes/Theme.Dark.xaml" },
        { AppTheme.ModernLight, "/Resources/Themes/Theme.ModernLight.xaml" },
        { AppTheme.HighContrast, "/Resources/Themes/Theme.HighContrast.xaml" },
        { AppTheme.CrabCute, "/Resources/Themes/Theme.CrabCute.xaml" },
    };

    // ── Registr složek ikon ───────────────────────────────────────────────────
    // PNG ikony pro každé téma — Resources/Icons/{složka}/
    private static readonly Dictionary<AppTheme, string> _iconFolderNames = new()
    {
        { AppTheme.Legacy, "" },         // Legacy nemá PNG ikony (používá emoji)
        { AppTheme.Modern, "Modern" },
        { AppTheme.StandardDark, "Modern" },
        { AppTheme.Dark, "ModernDark" },
        { AppTheme.ModernLight, "ModernLight" },
        { AppTheme.HighContrast, "Modern" },
        { AppTheme.CrabCute, "CrabCute" },
    };

    private static readonly Dictionary<AppTheme, Dictionary<string, string>> _iconFileNames = new()
    {
        {
            AppTheme.Dark,
            new Dictionary<string, string>
            {
                { "start", "Start.png" },
                { "stop", "Stop.png" },
                { "restart", "Restart.png" },
                { "powershell", "PowerShell.png" },
                { "gateway-log", "GatewayLog.png" },
                { "cleaning-tool", "CleaningTool.png" },
                { "token-manager", "TokenManager.png" },
                { "doctor-fix", "DoctorFix.png" },
                { "tui", "StartTUI.png" },
            }
        },
        {
            AppTheme.ModernLight,
            new Dictionary<string, string>
            {
                { "start", "Start.png" },
                { "stop", "Stop.png" },
                { "restart", "Restart.png" },
                { "powershell", "PowerShell.png" },
                { "gateway-log", "GatewayLog.png" },
                { "cleaning-tool", "CleaningTool.png" },
                { "token-manager", "TokenManager.png" },
                { "doctor-fix", "DoctorFix.png" },
                { "tui", "StartTUI.png" },
            }
        },
        {
            AppTheme.CrabCute,
            new Dictionary<string, string>
            {
                { "start", "Button_GatewayStart.png" },
                { "stop", "Button_GatewayStop.png" },
                { "restart", "Button_GatewayRestart.png" },
                { "powershell", "Button_PowerShell.png" },
                { "gateway-log", "Button_GatewayLog.png" },
                { "cleaning-tool", "Button_CleaningTool.png" },
                { "token-manager", "Button_TokenManager.png" },
                { "doctor-fix", "Button_Fix.png" },
                { "tui", "Button_TUI_1.png" },
                { "menu-powershell", "Menu-PowerShell.png" },
                { "menu-settings", "Menu-Settings.png" },
                { "menu-status", "Menu-Status.png" },
                { "log", "Log.png" },
            }
        }
    };

    // ── Veřejné API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Aplikuje téma: swapuje ResourceDictionary + emituje ThemeChanged.
    /// Bezpečné volat z konstruktoru i za běhu.
    /// </summary>
    public static void Apply(AppTheme theme)
    {
        SwapResourceDictionary(theme);
        ThemeChanged?.Invoke(theme);
    }

    /// <summary>
    /// Vrátí název složky ikon pro dané téma (prázdný string = žádné ikony).
    /// Příklad: GetIconFolder(AppTheme.Modern) → "Modern"
    /// </summary>
    public static string GetIconFolder(AppTheme theme)
    {
        if (theme == OpenClawManager.App.GetService<ISettingsService>().Settings.Theme)
        {
            var iconSet = GetString("Theme.Meta.IconSet", "");
            if (!string.IsNullOrWhiteSpace(iconSet)) return iconSet;
        }

        return _iconFolderNames.TryGetValue(theme, out var folder) ? folder : "";
    }

    public static bool IsModernPaletteTheme(AppTheme theme)
    {
        return theme is AppTheme.StandardDark or AppTheme.Dark or AppTheme.ModernLight;
    }

    public static ThemeMetadata GetCurrentMetadata()
    {
        return new ThemeMetadata(
            GetString("Theme.Meta.Name", ""),
            GetString("Theme.Meta.Variant", ""),
            GetString("Theme.Meta.IconSet", GetIconFolder(OpenClawManager.App.GetService<ISettingsService>().Settings.Theme)),
            GetString("Theme.Meta.PaletteFamily", ""),
            GetString("Theme.Meta.ButtonInteraction", "HoverScanline"));
    }

    public static string GetCurrentVariant() => GetCurrentMetadata().Variant;

    public static string GetCurrentButtonInteraction() => GetCurrentMetadata().ButtonInteraction;

    /// <summary>
    /// Vrátí pack:// URI pro ikonu daného tématu a jména.
    /// Příklad: GetIconUri(AppTheme.Modern, "start") →
    ///          "pack://application:,,,/Resources/Icons/Modern/start.png"
    /// Vrátí null pokud téma nemá složku ikon.
    /// </summary>
    public static Uri? GetIconUri(AppTheme theme, string iconName)
    {
        var folder = GetIconFolder(theme);
        if (string.IsNullOrEmpty(folder)) return null;

        var fileName = _iconFileNames.TryGetValue(theme, out var names) &&
                       names.TryGetValue(iconName, out var mapped)
            ? mapped
            : $"{iconName}.png";

        return new Uri($"pack://application:,,,/Resources/Icons/{folder}/{fileName}");
    }

    public static Brush GetBrush(string key, Color fallback)
    {
        return Application.Current?.TryFindResource(key) as Brush
            ?? new SolidColorBrush(fallback);
    }

    private static string GetString(string key, string fallback)
    {
        return Application.Current?.TryFindResource(key) as string ?? fallback;
    }

    // ── Interní: swap ResourceDictionary ─────────────────────────────────────
    private static void SwapResourceDictionary(AppTheme theme)
    {
        if (!_themeResourcePaths.TryGetValue(theme, out var path)) return;

        var app = Application.Current;
        if (app == null) return;

        var merged = app.Resources.MergedDictionaries;

        // Odebrat existující Theme.*.xaml dictionary
        var toRemove = merged
            .Where(d => d.Source?.OriginalString.Contains("/Resources/Themes/Theme.") == true)
            .ToList();
        foreach (var d in toRemove) merged.Remove(d);

        // Přidat nový
        var newDict = new ResourceDictionary
        {
            Source = new Uri(path, UriKind.Relative)
        };
        merged.Add(newDict);
    }
}
