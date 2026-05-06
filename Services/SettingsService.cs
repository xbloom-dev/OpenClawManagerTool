using System.IO;
using System.Text.Json;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

/// <summary>
/// Načítání a ukládání AppSettings z/do JSON souboru.
/// Lokalizace: %APPDATA%\OpenClawManager\settings.json
///
/// Pokud soubor neexistuje, vrací se nová instance AppSettings s defaultními hodnotami.
/// Při chybě parsování JSON se taky vrací defaulty (žádný crash aplikace kvůli rozbitému config).
/// </summary>
public static class SettingsService
{
    /// <summary>
    /// Cesta k settings souboru: %APPDATA%\OpenClawManager\settings.json
    /// </summary>
    public static string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenClawManager",
        "settings.json");

    /// <summary>
    /// Načte settings ze souboru. Pokud soubor neexistuje nebo je rozbitý, vrátí defaulty.
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            return settings ?? new AppSettings();
        }
        catch
        {
            // JSON je rozbitý nebo nečitelný — vrátíme defaulty místo pádu aplikace
            return new AppSettings();
        }
    }

    /// <summary>
    /// Uloží settings do souboru. Vytvoří složku %APPDATA%\OpenClawManager pokud neexistuje.
    /// </summary>
    /// <returns>True při úspěchu, False při chybě.</returns>
    public static bool Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true   // pretty-print pro čitelnost při ručním editu
            };

            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
