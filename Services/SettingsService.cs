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
            settings = MigrateSettings(settings ?? new AppSettings(), out var changed);
            if (changed)
                SaveToDisk(settings);

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static AppSettings MigrateSettings(AppSettings settings, out bool changed)
    {
        changed = false;
        var defaults = new AppSettings();

        if (settings.SchemaVersion < 1)
            changed = true;

        if (string.IsNullOrWhiteSpace(settings.OpenClawPath))
        {
            settings.OpenClawPath = defaults.OpenClawPath;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.TempPath))
        {
            settings.TempPath = defaults.TempPath;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.OpenClawCommand))
        {
            settings.OpenClawCommand = defaults.OpenClawCommand;
            changed = true;
        }

        if (settings.PowerShellWorkingDir == null)
        {
            settings.PowerShellWorkingDir = defaults.PowerShellWorkingDir;
            changed = true;
        }

        if (settings.CleanupAgents == null || settings.CleanupAgents.Count == 0)
        {
            settings.CleanupAgents = defaults.CleanupAgents;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.TokenManagerSecretsPath))
        {
            settings.TokenManagerSecretsPath = defaults.TokenManagerSecretsPath;
            changed = true;
        }

        if (!string.Equals(settings.Language, "CS", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(settings.Language, "EN", StringComparison.OrdinalIgnoreCase))
        {
            settings.Language = defaults.Language;
            changed = true;
        }
        else
        {
            var normalizedLanguage = settings.Language.ToUpperInvariant();
            if (settings.Language != normalizedLanguage)
            {
                settings.Language = normalizedLanguage;
                changed = true;
            }
        }

        if (!Enum.IsDefined(settings.Theme))
        {
            settings.Theme = defaults.Theme;
            changed = true;
        }

        if (settings.SchemaVersion < AppSettings.CurrentSchemaVersion)
        {
            settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
            changed = true;
        }

        return settings;
    }

    public static bool Save(AppSettings settings)
    {
        try
        {
            settings = MigrateSettings(settings, out _);
            SaveToDisk(settings);

            Current = settings;
            SettingsChanged?.Invoke(null, EventArgs.Empty);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void SaveToDisk(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(SettingsFilePath, json);
    }
}
