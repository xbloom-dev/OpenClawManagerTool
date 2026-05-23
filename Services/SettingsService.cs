using System.IO;
using System.Text.Json;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

/// <summary>
/// Loads and saves application settings for the current application lifetime.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    public string SettingsFilePath { get; }

    public AppSettings Settings { get; private set; }

    public event EventHandler? SettingsChanged;

    public SettingsService()
        : this(GetDefaultSettingsFilePath())
    {
    }

    internal SettingsService(string settingsFilePath)
    {
        SettingsFilePath = settingsFilePath;
        Settings = LoadFromDisk();
    }

    public static string GetDefaultSettingsFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenClawManager",
        "settings.json");

    private AppSettings LoadFromDisk()
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

    public AppSettings MigrateSettings(AppSettings settings, out bool changed)
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

    public bool Save(AppSettings settings)
    {
        try
        {
            settings = MigrateSettings(settings, out _);
            SaveToDisk(settings);

            Settings = settings;
            SettingsChanged?.Invoke(null, EventArgs.Empty);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SaveToDisk(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(settings, options);

        // Atomic write: zapsat do .tmp, pak Replace.
        // Při crash mezi truncate a write zůstane původní settings.json nedotčen.
        var tmpPath = SettingsFilePath + ".tmp";
        File.WriteAllText(tmpPath, json);

        if (File.Exists(SettingsFilePath))
        {
            ReplaceExistingFile(tmpPath, SettingsFilePath);
        }
        else
        {
            // První save — žádný target k nahrazení, jen rename.
            File.Move(tmpPath, SettingsFilePath);
        }
    }

    private static void ReplaceExistingFile(string tmpPath, string targetPath)
    {
        var backupPath = targetPath + ".bak";

        try
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            // File.Replace je atomic (NTFS): cíl bude buď zcela starý, nebo zcela nový.
            File.Replace(tmpPath, targetPath, backupPath);
        }
        catch (UnauthorizedAccessException)
        {
            File.Copy(targetPath, backupPath, overwrite: true);
            File.Move(tmpPath, targetPath, overwrite: true);
        }

        try { File.Delete(backupPath); } catch { /* best effort */ }
    }
}
