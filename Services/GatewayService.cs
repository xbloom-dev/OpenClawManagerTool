using System.Diagnostics;
using System.Management;

namespace OpenClawManager.Services;

/// <summary>
/// Správa OpenClaw Gateway procesu — start, stop, restart, doctor --fix.
///
/// Architektonické rozhodnutí:
/// - Start spouští 'openclaw gateway' v novém PowerShell okně s -NoExit
///   (uživatel vidí logy a případné chyby při startu)
/// - Stop zabije Gateway node.exe proces + uklidí PowerShell wrapper okno
///   (čistý desktop, žádná osamělá okna po Stop)
/// - Stop SE NEDOTÝKÁ Scheduled Task "OpenClaw Gateway" — task zůstává jak ho má
///   uživatel nakonfigurovaný (typicky spouští Gateway při přihlášení Windows).
///   Příkaz 'openclaw gateway stop' jsme zde vědomě NEPOUŽILI, protože:
///     1) Stop tlačítko má dělat co slibuje — zastavit Gateway, ne zakazovat task
///     2) Task se zastaví přirozeně tím, že zabijeme node.exe proces
///     3) Při dalším přihlášení uživatele se Gateway spustí jak má (pokud má task)
/// </summary>
public static class GatewayService
{
    /// <summary>
    /// Spustí openclaw gateway v novém viditelném PowerShell okně.
    /// PowerShell zůstává otevřený (-NoExit) aby uživatel viděl logy Gateway.
    /// </summary>
    /// <returns>Process objekt nového PowerShell okna (ne samotný node.exe).</returns>
    public static Process? Start()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoExit -NoProfile -Command \"& openclaw gateway\"",
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

    /// <summary>
    /// Zastaví Gateway:
    /// 1) Zabije běžící node.exe proces s 'openclaw gateway' v command line
    /// 2) Zabije všechna PowerShell okna která spustila 'openclaw gateway' (wrapper okna)
    ///
    /// Záměrně neovlivňuje Scheduled Task "OpenClaw Gateway" — viz dokumentace třídy.
    /// </summary>
    /// <returns>True pokud byl Gateway zastaven nebo už neběžel.</returns>
    public static bool Stop()
    {
        bool processKilled = false;

        // Krok 1: Zabít běžící Gateway node.exe proces
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
                processKilled = true; // už neběží — to je v pořádku
            }
        }
        catch
        {
            processKilled = false;
        }

        // Krok 2: Zabít všechna PowerShell okna spuštěná s 'openclaw gateway'
        try
        {
            KillPowerShellGatewayWrappers();
        }
        catch
        {
            // selhání úklidu PowerShell oken není fatální — Gateway proces už je zabit
        }

        return processKilled;
    }

    /// <summary>
    /// Najde a zabije všechna powershell.exe okna která mají v command line 'openclaw gateway'.
    /// Tím se uklidí wrapper okno které zůstalo viset po zabití node.exe procesu.
    /// </summary>
    private static void KillPowerShellGatewayWrappers()
    {
        var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='powershell.exe'";
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();

        foreach (ManagementObject psProc in results)
        {
            var commandLine = psProc["CommandLine"]?.ToString() ?? "";

            // Hledáme PowerShell okna spuštěná s "openclaw gateway"
            if (commandLine.Contains("openclaw gateway"))
            {
                try
                {
                    var pid = (uint)psProc["ProcessId"];
                    var proc = Process.GetProcessById((int)pid);
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(2000);
                }
                catch
                {
                    // proces už mezitím skončil nebo nelze zabít, pokračuj
                }
            }
        }
    }

    /// <summary>
    /// Restart = Stop + krátká pauza pro uvolnění portu + Start.
    /// </summary>
    public static Process? Restart()
    {
        Stop();
        Thread.Sleep(2000); // počkáme až se Gateway plně ukončí a uvolní port 18789
        return Start();
    }

    /// <summary>
    /// Spustí openclaw doctor --fix v novém viditelném PowerShell okně.
    /// </summary>
    public static Process? RunDoctorFix()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoExit -NoProfile -Command \"& openclaw doctor --fix\"",
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
