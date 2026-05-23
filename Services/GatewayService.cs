using System.Diagnostics;
using System.Management;
using System.Security.Principal;

namespace OpenClawManager.Services;

/// <summary>
/// Správa OpenClaw Gateway procesu — start, stop, restart, doctor --fix, start TUI.
///
/// Cesta k openclaw příkazu se čerpá z ISettingsService.
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

        char[] forbidden = ['\r', '\n', '"', '\'', ';', '&', '|', '`', '<', '>', '%', '^'];
        var found = command.IndexOfAny(forbidden);
        if (found >= 0)
        {
            error = "The path or command contains disallowed special characters (<, >, %, ^, &, |).";
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
        var command = OpenClawManager.App.GetService<ISettingsService>().Settings.OpenClawCommand;
        if (!TryValidateOpenClawCommand(command, out var error))
            throw new InvalidOperationException(error);
        return command;
    }

    private static string EscapePowerShellSingleQuotedString(string value) => value.Replace("'", "''");

    private static void KillPowerShellGatewayWrappers()
    {
        var currentUserSid = GetCurrentUserSid();
        if (currentUserSid == null) return; // bezpečně neudělat nic

        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
        {
            // Vlastník procesu — killovat jen vlastní procesy
            var ownerSid = GetProcessOwnerSid(psProc);
            if (ownerSid != currentUserSid) continue;

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
        var currentUserSid = GetCurrentUserSid();
        if (currentUserSid == null) return;

        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
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
                    var proc = Process.GetProcessById((int)pid);
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(2000);
                }
                catch { }
            }
        }
    }

    // ── Helper metody pro identifikaci vlastníka procesu ──────────────────────

    /// <summary>
    /// Vrátí SID aktuálního uživatele (přihlášený user pod kterým běží aplikace).
    /// Vrátí null pokud SID nelze získat.
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
    /// Získá SID vlastníka procesu přes WMI metodu GetOwnerSid.
    /// Vrátí null pokud SID nelze získat (proces už neexistuje, access denied atd.).
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
            // proces už neexistuje nebo access denied
        }
        return null;
    }

    public static async Task<Process?> RestartAsync()
    {
        Stop();
        await Task.Delay(2000);
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
