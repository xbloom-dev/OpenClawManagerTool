using System.Diagnostics;
using OpenClawManager.Models;
using static OpenClawManager.Services.ResourceMonitor;

namespace OpenClawManager.Services;

internal sealed class ResourceMonitorAdapter : IResourceMonitor
{
    public Task<ResourceSnapshot> MeasureAsync(CancellationToken cancellationToken = default) =>
        ResourceMonitor.MeasureAsync(cancellationToken);

    public ResourceSnapshot Measure() => ResourceMonitor.Measure();
}

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
}
