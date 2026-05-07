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
        var openclawCmd = SettingsService.Current.OpenClawCommand;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -Command \"& '{openclawCmd}' gateway\"",
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
        var openclawCmd = SettingsService.Current.OpenClawCommand;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -Command \"& '{openclawCmd}' tui\"",
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
        var openclawCmd = SettingsService.Current.OpenClawCommand;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -NoProfile -Command \"& '{openclawCmd}' doctor --fix\"",
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
