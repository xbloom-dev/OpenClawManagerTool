using System.Diagnostics;

namespace OpenClawManager.Services;

/// <summary>
/// Správa Windows Scheduled Task "OpenClaw Gateway" přes schtasks.exe.
///
/// Většina operací (Disable/Enable) vyžaduje admin práva.
/// Query (Exists/IsEnabled) funguje bez admin.
/// </summary>
public static class ScheduledTaskService
{
    private const string TaskName = "OpenClaw Gateway";

    /// <summary>
    /// True pokud Scheduled Task existuje (lze ho zobrazit i bez admin).
    /// </summary>
    public static bool Exists()
    {
        var result = RunSchtasks($"/Query /TN \"{TaskName}\"");
        return result.ExitCode == 0;
    }

    /// <summary>
    /// True pokud task existuje A je zapnutý (Ready / Running stav).
    /// </summary>
    public static bool IsEnabled()
    {
        var result = RunSchtasks($"/Query /TN \"{TaskName}\" /FO LIST");
        if (result.ExitCode != 0) return false;

        // schtasks vypisuje "Status: Ready" nebo "Status: Disabled"
        // (lokalizace: na české Windows "Stav: Připraveno" / "Stav: Zakázáno")
        return result.Output.Contains("Ready", StringComparison.OrdinalIgnoreCase)
            || result.Output.Contains("Running", StringComparison.OrdinalIgnoreCase)
            || result.Output.Contains("Připraveno", StringComparison.OrdinalIgnoreCase)
            || result.Output.Contains("Spuštěno", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Vypne Scheduled Task. Vyžaduje admin práva.
    /// </summary>
    /// <returns>True pokud úspěch, jinak false (typicky kvůli chybějícím admin právům)</returns>
    public static bool Disable()
    {
        var result = RunSchtasks($"/Change /TN \"{TaskName}\" /DISABLE");
        return result.ExitCode == 0;
    }

    /// <summary>
    /// Zapne Scheduled Task. Vyžaduje admin práva.
    /// </summary>
    public static bool Enable()
    {
        var result = RunSchtasks($"/Change /TN \"{TaskName}\" /ENABLE");
        return result.ExitCode == 0;
    }

    private record SchtasksResult(int ExitCode, string Output, string Error);

    private static SchtasksResult RunSchtasks(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null)
                return new SchtasksResult(-1, "", "Failed to start schtasks.exe");

            var output = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            proc.WaitForExit(5000);

            return new SchtasksResult(proc.ExitCode, output, error);
        }
        catch (Exception ex)
        {
            return new SchtasksResult(-1, "", ex.Message);
        }
    }
}
