using System.Diagnostics;
using System.Management;

namespace OpenClawManager.Services;

/// <summary>
/// Správa OpenClaw Gateway procesu — start, stop, restart, doctor --fix, start TUI.
///
/// Cesta k openclaw příkazu se čerpá z SettingsService.Current.OpenClawCommand.
/// Stop SE NEDOTÝKÁ Scheduled Task — task se zastaví přirozeně tím že zabijeme node.exe.
/// </summary>
public static class GatewayService
{
    public static Process? Start()
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

    public static Process? StartTui()
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

    public static bool Stop()
    {
        bool processKilled = false;

        try
        {
            var gateway = ProcessDetector.FindGatewayProcess();
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
            // selhání úklidu PowerShell oken není fatální
        }

        return processKilled;
    }

    /// <summary>
    /// Zastaví Gateway a navíc zavře všechna TUI okna.
    /// Voláno z dialogu Stop Gateway když uživatel zaškrtl "Zavřít také TUI okna".
    /// </summary>
    public static bool StopAndCloseTui()
    {
        var ok = Stop();

        try
        {
            KillPowerShellTuiWrappers();
        }
        catch { }

        return ok;
    }


    public static bool TryValidateOpenClawCommand(string command, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(command))
        {
            error = "OpenClaw command cannot be empty.";
            return false;
        }

        char[] forbidden = ['\r', '\n', '"', '\'', ';', '&', '|', '`'];
        var found = command.IndexOfAny(forbidden);
        if (found >= 0)
        {
            error = $"OpenClaw command contains an unsafe character: {command[found]}";
            return false;
        }

        return true;
    }

    public static string BuildPowerShellArguments(string openClawSubCommand)
    {
        var openclawCmd = GetValidatedOpenClawCommand();
        return $"-NoExit -NoProfile -Command \"& '{EscapePowerShellSingleQuotedString(openclawCmd)}' {openClawSubCommand}\"";
    }

    public static string BuildCmdExeCommand(string openClawSubCommand)
    {
        var openclawCmd = GetValidatedOpenClawCommand();
        return $"cmd.exe /c \"\"{openclawCmd}\" {openClawSubCommand}\"";
    }

    private static string GetValidatedOpenClawCommand()
    {
        var command = SettingsService.Current.OpenClawCommand;
        if (!TryValidateOpenClawCommand(command, out var error))
            throw new InvalidOperationException(error);
        return command;
    }

    private static string EscapePowerShellSingleQuotedString(string value) => value.Replace("'", "''");

    private static void KillPowerShellGatewayWrappers()
    {
        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
        {
            var commandLine = psProc["CommandLine"]?.ToString() ?? "";

            if (commandLine.Contains("openclaw") && commandLine.Contains("gateway"))
            {
                try
                {
                    var pid = (uint)psProc["ProcessId"];
                    var proc = Process.GetProcessById((int)pid);
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(2000);
                }
                catch { }
            }
        }
    }

    private static void KillPowerShellTuiWrappers()
    {
        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
        {
            var commandLine = psProc["CommandLine"]?.ToString() ?? "";

            if (commandLine.Contains("openclaw") &&
                commandLine.Contains(" tui") &&
                !commandLine.Contains("gateway"))
            {
                try
                {
                    var pid = (uint)psProc["ProcessId"];
                    var proc = Process.GetProcessById((int)pid);
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(2000);
                }
                catch { }
            }
        }
    }

    public static Process? Restart()
    {
        Stop();
        Thread.Sleep(2000);
        return Start();
    }

    public static Process? RunDoctorFix()
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
