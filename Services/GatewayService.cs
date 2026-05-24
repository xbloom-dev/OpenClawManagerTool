using System.Diagnostics;
using System.Management;
using System.Security.Principal;

namespace OpenClawManager.Services;

/// <summary>
/// Manages the OpenClaw Gateway process: start, stop, restart, doctor --fix, and TUI startup.
///
/// The OpenClaw command path is read from ISettingsService.
/// Stop does not touch the scheduled task; it stops naturally when the node.exe process is killed.
/// </summary>
public sealed class GatewayService : IGatewayService
{
    private readonly ISettingsService _settingsService;
    private readonly IProcessDetector _processDetector;

    public GatewayService(ISettingsService settingsService, IProcessDetector processDetector)
    {
        _settingsService = settingsService;
        _processDetector = processDetector;
    }

    public Process? Start()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = BuildPowerShellArguments("gateway"),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            return Process.Start(psi);
        }
        catch
        {
            return null;
        }
    }

    public Process? StartTui()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = BuildPowerShellArguments("tui"),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            return Process.Start(psi);
        }
        catch
        {
            return null;
        }
    }

    public bool Stop()
    {
        bool processKilled = false;

        try
        {
            var gateway = _processDetector.FindGatewayProcess();
            if (gateway != null)
            {
                gateway.Kill(entireProcessTree: true);
                gateway.WaitForExit(5000);
                processKilled = true;
            }
            else
            {
                processKilled = true;
            }
        }
        catch
        {
            processKilled = false;
        }

        try
        {
            KillPowerShellGatewayWrappers();
        }
        catch
        {
            // PowerShell wrapper cleanup failure is not fatal.
        }

        return processKilled;
    }

    /// <summary>
    /// Stops Gateway and also closes all TUI windows.
    /// Called from the Stop Gateway dialog when the user chooses to close TUI windows too.
    /// </summary>
    public bool StopAndCloseTui()
    {
        var ok = Stop();

        try
        {
            KillPowerShellTuiWrappers();
        }
        catch { }

        return ok;
    }


    public bool TryValidateOpenClawCommand(string command, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(command))
        {
            error = "OpenClaw command cannot be empty.";
            return false;
        }

        char[] forbidden = ['\r', '\n', '"', '\'', ';', '&', '|', '`', '<', '>', '%', '^'];
        var found = command.IndexOfAny(forbidden);
        if (found >= 0)
        {
            error = "The path or command contains disallowed special characters (<, >, %, ^, &, |).";
            return false;
        }

        return true;
    }

    public string BuildPowerShellArguments(string openClawSubCommand)
    {
        var openclawCmd = GetValidatedOpenClawCommand();
        return $"-NoExit -NoProfile -Command \"& '{EscapePowerShellSingleQuotedString(openclawCmd)}' {openClawSubCommand}\"";
    }

    public string BuildCmdExeCommand(string openClawSubCommand)
    {
        var openclawCmd = GetValidatedOpenClawCommand();
        return $"cmd.exe /c \"\"{openclawCmd}\" {openClawSubCommand}\"";
    }

    private string GetValidatedOpenClawCommand()
    {
        var command = _settingsService.Settings.OpenClawCommand;
        if (!TryValidateOpenClawCommand(command, out var error))
            throw new InvalidOperationException(error);
        return command;
    }

    private static string EscapePowerShellSingleQuotedString(string value) => value.Replace("'", "''");

    private void KillPowerShellGatewayWrappers()
    {
        var currentUserSid = GetCurrentUserSid();
        if (currentUserSid == null) return;

        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
        {
            using (psProc)
            {
                var ownerSid = GetProcessOwnerSid(psProc);
                if (ownerSid != currentUserSid) continue;

                var commandLine = psProc["CommandLine"]?.ToString() ?? "";

                if (commandLine.Contains("openclaw") && commandLine.Contains("gateway"))
                {
                    try
                    {
                        var pid = (uint)psProc["ProcessId"];
                        using var proc = Process.GetProcessById((int)pid);
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(2000);
                    }
                    catch { }
                }
            }
        }
    }

    private void KillPowerShellTuiWrappers()
    {
        var currentUserSid = GetCurrentUserSid();
        if (currentUserSid == null) return;

        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
        {
            using (psProc)
            {
                var ownerSid = GetProcessOwnerSid(psProc);
                if (ownerSid != currentUserSid) continue;

                var commandLine = psProc["CommandLine"]?.ToString() ?? "";

                if (commandLine.Contains("openclaw") &&
                    commandLine.Contains(" tui") &&
                    !commandLine.Contains("gateway"))
                {
                    try
                    {
                        var pid = (uint)psProc["ProcessId"];
                        using var proc = Process.GetProcessById((int)pid);
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(2000);
                    }
                    catch { }
                }
            }
        }
    }

    /// <summary>
    /// Returns the current user's SID, or null when it cannot be read.
    /// </summary>
    private static string? GetCurrentUserSid()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the process owner SID through WMI GetOwnerSid.
    /// Returns null when the process no longer exists or access is denied.
    /// </summary>
    private static string? GetProcessOwnerSid(ManagementObject process)
    {
        try
        {
            var args = new object[] { string.Empty };
            var result = process.InvokeMethod("GetOwnerSid", args);
            if (result is uint ret && ret == 0)
                return args[0] as string;
        }
        catch
        {
            // The process no longer exists or access is denied.
        }
        return null;
    }

    public async Task<Process?> RestartAsync()
    {
        Stop();
        await Task.Delay(2000);
        return Start();
    }

    public Process? RunDoctorFix()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = BuildPowerShellArguments("doctor --fix"),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            return Process.Start(psi);
        }
        catch
        {
            return null;
        }
    }
}
