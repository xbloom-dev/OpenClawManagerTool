using System.Diagnostics;
using OpenClawManager.Models;
using static OpenClawManager.Services.ResourceMonitor;

namespace OpenClawManager.Services;

internal sealed class GatewayServiceAdapter : IGatewayService
{
    public Process? Start() => GatewayService.Start();
    public Process? StartTui() => GatewayService.StartTui();
    public bool Stop() => GatewayService.Stop();
    public bool StopAndCloseTui() => GatewayService.StopAndCloseTui();
    public bool TryValidateOpenClawCommand(string command, out string error) =>
        GatewayService.TryValidateOpenClawCommand(command, out error);
    public string BuildPowerShellArguments(string openClawSubCommand) =>
        GatewayService.BuildPowerShellArguments(openClawSubCommand);
    public string BuildCmdExeCommand(string openClawSubCommand) =>
        GatewayService.BuildCmdExeCommand(openClawSubCommand);
    public Task<Process?> RestartAsync() => GatewayService.RestartAsync();
    public Process? RunDoctorFix() => GatewayService.RunDoctorFix();
}

internal sealed class ResourceMonitorAdapter : IResourceMonitor
{
    public Task<ResourceSnapshot> MeasureAsync(CancellationToken cancellationToken = default) =>
        ResourceMonitor.MeasureAsync(cancellationToken);

    public ResourceSnapshot Measure() => ResourceMonitor.Measure();
}

internal sealed class ProcessDetectorAdapter : IProcessDetector
{
    public Process? FindGatewayProcess() => ProcessDetector.FindGatewayProcess();
    public bool IsGatewayRunning() => ProcessDetector.IsGatewayRunning();
}

internal sealed class CleanupServiceAdapter : ICleanupService
{
    public IReadOnlyList<CleanupStep> AllSteps => CleanupService.AllSteps;
    public IReadOnlyList<string> DefaultAgents => CleanupService.DefaultAgents;

    public CleanupStepResult RunStep(
        int stepNum,
        bool dryRun,
        Action<string> log,
        int keepSessions = 10) =>
        CleanupService.RunStep(stepNum, dryRun, log, keepSessions);

    public string FormatBytes(long bytes) => CleanupService.FormatBytes(bytes);
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
