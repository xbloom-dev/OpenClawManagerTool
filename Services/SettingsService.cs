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

    public bool IsPortableMode { get; }

    public bool IsNewSettingsFile { get; private set; }

    public bool IsFirstRunCandidate => IsNewSettingsFile && Settings.Theme == AppTheme.Legacy;

    public AppSettings Settings { get; private set; }

    public event EventHandler? SettingsChanged;

    public SettingsService()
        : this(new AppEnvironment())
    {
    }

    public SettingsService(IAppEnvironment env)
        : this(ResolveSettingsPath(env))
    {
    }

    internal SettingsService(string settingsFilePath)
        : this(settingsFilePath, isPortableMode: false)
    {
    }

    private SettingsService(SettingsPathInfo settingsPath)
        : this(settingsPath.Path, settingsPath.IsPortable)
    {
    }

    private SettingsService(string settingsFilePath, bool isPortableMode)
    {
        SettingsFilePath = settingsFilePath;
        IsPortableMode = isPortableMode;
        Settings = LoadFromDisk();
    }

    public static string GetDefaultSettingsFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenClawManager",
        "settings.json");

    public static string GetPortableSettingsFilePath(string appBaseDirectory) =>
        Path.Combine(appBaseDirectory, "settings.json");

    private static SettingsPathInfo ResolveSettingsPath(IAppEnvironment env)
    {
        var portablePath = GetPortableSettingsFilePath(env.AppBaseDirectory);
        return File.Exists(portablePath)
            ? new SettingsPathInfo(portablePath, true)
            : new SettingsPathInfo(env.SettingsFilePath, false);
    }

    private AppSettings LoadFromDisk()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                IsNewSettingsFile = true;
                return new AppSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            IsNewSettingsFile = IsFirstRunJson(json);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

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

#if LITE_BUILD
        if (settings.Theme != AppTheme.Legacy)
        {
            settings.Theme = AppTheme.Legacy;
            changed = true;
        }
#endif

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
            IsNewSettingsFile = false;
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

    private static bool IsFirstRunJson(string json) =>
        string.IsNullOrWhiteSpace(json) || json.Trim() == "{}";

    private readonly record struct SettingsPathInfo(string Path, bool IsPortable);
}
