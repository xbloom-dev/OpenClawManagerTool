using System.Diagnostics;
using OpenClawManager.Models;
using static OpenClawManager.Services.ResourceMonitor;

namespace OpenClawManager.Services;

public interface IAppEnvironment
{
    string SettingsFilePath { get; }
    string WebView2DataRoot { get; }
    string AppBaseDirectory { get; }
}

public interface ISettingsService
{
    string SettingsFilePath { get; }
    bool IsPortableMode { get; }
    bool IsNewSettingsFile { get; }
    bool IsFirstRunCandidate { get; }
    AppSettings Settings { get; }
    event EventHandler? SettingsChanged;
    bool Save(AppSettings settings);
    AppSettings MigrateSettings(AppSettings settings, out bool changed);
}

public interface IGatewayService
{
    Process? Start();
    Process? StartTui();
    bool Stop();
    bool StopAndCloseTui();
    bool TryValidateOpenClawCommand(string command, out string error);
    string BuildPowerShellArguments(string openClawSubCommand);
    string BuildCmdExeCommand(string openClawSubCommand);
    Task<Process?> RestartAsync();
    Process? RunDoctorFix();
}

public interface IResourceMonitor
{
    Task<ResourceSnapshot> MeasureAsync(CancellationToken cancellationToken = default);
    ResourceSnapshot Measure();
}

public interface IProcessDetector
{
    Process? FindGatewayProcess();
    bool IsGatewayRunning();
    Task<Process?> FindGatewayProcessAsync(CancellationToken ct = default);
    Task<bool> IsGatewayRunningAsync(CancellationToken ct = default);
}

public interface ICleanupService
{
    IReadOnlyList<CleanupStep> AllSteps { get; }
    IReadOnlyList<string> DefaultAgents { get; }
    CleanupStepResult RunStep(
        int stepNum,
        bool dryRun,
        Action<string> log,
        int keepSessions = 10);
    string FormatBytes(long bytes);
}

public interface ITokenService
{
    string GetDefaultVaultPath();
    bool EnsureVaultExists(string path);
    TokenVault LoadVault(string path);
    void SaveVault(string path, TokenVault vault);
    Task ExportVaultAsync(string vaultPath, string filePath, string password);
    Task ImportVaultAsync(string vaultPath, string filePath, string password);
    TokenEntry AddToken(string vaultPath, string id, string value, string description);
    void UpdateToken(string vaultPath, string originalId, string id, string value, string description);
    void RemoveToken(string vaultPath, string id);
    void RotateToken(string vaultPath, string id, string newValue);
    TokenFileOperationResult RedactFile(
        string vaultPath,
        string inputPath,
        string? outputPath = null,
        bool overwrite = false,
        bool dryRun = false);
    TokenFileOperationResult RestoreFile(
        string vaultPath,
        string inputPath,
        string? outputPath = null,
        bool overwrite = false,
        bool dryRun = false);
    TokenFileOperationResult RestoreFileInPlace(string vaultPath, string inputPath);
    TokenVerifyResult VerifyFile(string vaultPath, string inputPath);
    VaultSafetyResult AnalyzeVaultPath(string vaultPath, string openClawPath);
    bool IsVaultEncryptedAtRest(string path);
    bool IsVaultTrackedByGit(string path);
    GitIgnoreResult AddVaultToGitIgnore(string vaultPath);
    string BuildDefaultOutputPath(string inputPath, string marker);
}

public interface IUpdateCheckService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

public sealed record UpdateCheckResult(
    bool Success,
    bool UpdateAvailable,
    string CurrentVersion,
    string? LatestVersion,
    string? ReleaseUrl,
    string? ErrorMessage);
