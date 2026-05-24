using System.Diagnostics;
using OpenClawManager.Models;

namespace OpenClawManager.Services;

internal sealed class CleanupServiceAdapter : ICleanupService
{
    private readonly CleanupService _inner;

    public CleanupServiceAdapter()
    {
        _inner = new CleanupService(new SettingsService());
    }

    public IReadOnlyList<CleanupStep> AllSteps => CleanupService.AllSteps;
    public IReadOnlyList<string> DefaultAgents => CleanupService.DefaultAgents;

    public CleanupStepResult RunStep(
        int stepNum,
        bool dryRun,
        Action<string> log,
        int keepSessions = 10) =>
        _inner.RunStep(stepNum, dryRun, log, keepSessions);

    public string FormatBytes(long bytes) => _inner.FormatBytes(bytes);
}

internal sealed class TokenServiceAdapter : ITokenService
{
    public string GetDefaultVaultPath() => TokenService.GetDefaultVaultPath();
    public bool EnsureVaultExists(string path) => TokenService.EnsureVaultExists(path);
    public TokenVault LoadVault(string path) => TokenService.LoadVault(path);
    public void SaveVault(string path, TokenVault vault) => TokenService.SaveVault(path, vault);
    public Task ExportVaultAsync(string vaultPath, string filePath, string password) =>
        TokenService.ExportVaultAsync(vaultPath, filePath, password);
    public Task ImportVaultAsync(string vaultPath, string filePath, string password) =>
        TokenService.ImportVaultAsync(vaultPath, filePath, password);
    public TokenEntry AddToken(string vaultPath, string id, string value, string description) =>
        TokenService.AddToken(vaultPath, id, value, description);
    public void UpdateToken(string vaultPath, string originalId, string id, string value, string description) =>
        TokenService.UpdateToken(vaultPath, originalId, id, value, description);
    public void RemoveToken(string vaultPath, string id) => TokenService.RemoveToken(vaultPath, id);
    public void RotateToken(string vaultPath, string id, string newValue) =>
        TokenService.RotateToken(vaultPath, id, newValue);
    public TokenFileOperationResult RedactFile(
        string vaultPath,
        string inputPath,
        string? outputPath = null,
        bool overwrite = false,
        bool dryRun = false) =>
        TokenService.RedactFile(vaultPath, inputPath, outputPath, overwrite, dryRun);
    public TokenFileOperationResult RestoreFile(
        string vaultPath,
        string inputPath,
        string? outputPath = null,
        bool overwrite = false,
        bool dryRun = false) =>
        TokenService.RestoreFile(vaultPath, inputPath, outputPath, overwrite, dryRun);
    public TokenFileOperationResult RestoreFileInPlace(string vaultPath, string inputPath) =>
        TokenService.RestoreFileInPlace(vaultPath, inputPath);
    public TokenVerifyResult VerifyFile(string vaultPath, string inputPath) =>
        TokenService.VerifyFile(vaultPath, inputPath);
    public VaultSafetyResult AnalyzeVaultPath(string vaultPath, string openClawPath) =>
        TokenService.AnalyzeVaultPath(vaultPath, openClawPath);
    public bool IsVaultEncryptedAtRest(string path) => TokenService.IsVaultEncryptedAtRest(path);
    public bool IsVaultTrackedByGit(string path) => TokenService.IsVaultTrackedByGit(path);
    public GitIgnoreResult AddVaultToGitIgnore(string vaultPath) =>
        TokenService.AddVaultToGitIgnore(vaultPath);
    public string BuildDefaultOutputPath(string inputPath, string marker) =>
        TokenService.BuildDefaultOutputPath(inputPath, marker);
}
