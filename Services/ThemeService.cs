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
using OpenClawManager.Models;

namespace OpenClawManager.Services;

public static class ThemeService
{
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
        // Budoucí témata:
        // { AppTheme.Dark,          "/Resources/Themes/Theme.Dark.xaml" },
        // { AppTheme.HighContrast,  "/Resources/Themes/Theme.HighContrast.xaml" },
        // { AppTheme.Compact,       "/Resources/Themes/Theme.Compact.xaml" },
    };

    // ── Registr složek ikon ───────────────────────────────────────────────────
    // PNG ikony pro každé téma — Resources/Icons/{složka}/
    private static readonly Dictionary<AppTheme, string> _iconFolderNames = new()
    {
        { AppTheme.Legacy, "" },         // Legacy nemá PNG ikony (používá emoji)
        { AppTheme.Modern, "Modern" },
        // { AppTheme.Dark,         "Dark" },
        // { AppTheme.HighContrast, "HighContrast" },
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
        return _iconFolderNames.TryGetValue(theme, out var folder) ? folder : "";
    }

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
        return new Uri($"pack://application:,,,/Resources/Icons/{folder}/{iconName}.png");
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
