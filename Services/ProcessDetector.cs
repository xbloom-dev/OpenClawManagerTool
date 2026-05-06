using System.Diagnostics;
using System.Management;

namespace OpenClawManager.Services;

/// <summary>
/// Detekce běžících OpenClaw procesů přes WMI/CIM.
/// Používá Win32_Process dotazy stejně jako cleanup.ps1.
/// </summary>
public static class ProcessDetector
{
    /// <summary>
    /// Najde běžící openclaw gateway proces.
    /// </summary>
    /// <returns>Process objekt pokud Gateway běží, jinak null.</returns>
    public static Process? FindGatewayProcess()
    {
        try
        {
            // WMI dotaz: hledáme node.exe procesy s "openclaw gateway" v command line
            var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='node.exe'";

            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();

            foreach (ManagementObject process in results)
            {
                var commandLine = process["CommandLine"]?.ToString() ?? "";

                if (commandLine.Contains("openclaw") && commandLine.Contains("gateway"))
                {
                    var pid = (uint)process["ProcessId"];

                    try
                    {
                        return Process.GetProcessById((int)pid);
                    }
                    catch
                    {
                        // Process už mezitím skončil
                        continue;
                    }
                }
            }
        }
        catch
        {
            // WMI selhání — vracíme null (Gateway "neběží" z pohledu aplikace)
        }

        return null;
    }

    /// <summary>
    /// Rychlá kontrola zda Gateway běží.
    /// </summary>
    public static bool IsGatewayRunning() => FindGatewayProcess() != null;
}
