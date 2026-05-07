using System.IO;
using System.Text.Json;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

/// <summary>
/// Singleton služba pro načítání a ukládání AppSettings.
///
/// Architektura: SettingsService.Current vrací aktuální AppSettings instanci pro
/// celou aplikaci. Všechny services i Views si berou nastavení odsud (žádné
/// předávání přes parametry).
/// </summary>
public static class SettingsService
{
    public static string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenClawManager",
        "settings.json");

    public static AppSettings Current
    {
        get
        {
            if (_current == null)
                _current = LoadFromDisk();
            return _current;
        }
        private set => _current = value;
    }
    private static AppSettings? _current;

    public static event EventHandler? SettingsChanged;

    private static AppSettings LoadFromDisk()
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
            return new AppSettings();
        }
    }

    public static bool Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);

            Current = settings;
            SettingsChanged?.Invoke(null, EventArgs.Empty);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
