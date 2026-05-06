using System.Diagnostics;
using System.Management;

namespace OpenClawManager.Services;

/// <summary>
/// Měření systémových zdrojů: RAM, CPU, VRAM.
/// Použité metody přesně odpovídají osvědčeným postupům z PowerShell skriptů
/// (Win32_OperatingSystem, Win32_Processor, nvidia-smi).
/// </summary>
public static class ResourceMonitor
{
    /// <summary>
    /// Snapshot všech aktuálních hodnot zdrojů.
    /// </summary>
    public record ResourceSnapshot(
        double RamUsedGb,
        double RamTotalGb,
        int CpuPercent,
        double? VramUsedGb,
        double? VramTotalGb
    );

    /// <summary>
    /// Změří aktuální zdroje. Pokud nějaké měření selže, vrátí jen co se podařilo.
    /// </summary>
    public static ResourceSnapshot Measure()
    {
        var (ramUsed, ramTotal) = MeasureRam();
        var cpu = MeasureCpu();
        var (vramUsed, vramTotal) = MeasureVram();

        return new ResourceSnapshot(ramUsed, ramTotal, cpu, vramUsed, vramTotal);
    }

    /// <summary>
    /// RAM přes Win32_OperatingSystem (TotalVisibleMemorySize, FreePhysicalMemory).
    /// Vrací GB, zaokrouhleno na 1 desetinné místo.
    /// </summary>
    private static (double used, double total) MeasureRam()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            using var results = searcher.Get();

            foreach (ManagementObject os in results)
            {
                // Hodnoty jsou v KB
                var totalKb = Convert.ToDouble(os["TotalVisibleMemorySize"]);
                var freeKb  = Convert.ToDouble(os["FreePhysicalMemory"]);

                var totalGb = Math.Round(totalKb / 1024 / 1024, 1);
                var freeGb  = Math.Round(freeKb  / 1024 / 1024, 1);
                var usedGb  = Math.Round(totalGb - freeGb, 1);

                return (usedGb, totalGb);
            }
        }
        catch
        {
            // ignore — vrátíme nuly níže
        }

        return (0, 0);
    }

    /// <summary>
    /// CPU přes Win32_Processor (LoadPercentage).
    /// Funguje na české i anglické lokalizaci Windows (na rozdíl od Get-Counter).
    /// </summary>
    private static int MeasureCpu()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT LoadPercentage FROM Win32_Processor");
            using var results = searcher.Get();

            var loads = new List<int>();
            foreach (ManagementObject cpu in results)
            {
                var load = cpu["LoadPercentage"];
                if (load != null)
                    loads.Add(Convert.ToInt32(load));
            }

            return loads.Count > 0 ? (int)loads.Average() : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// VRAM přes nvidia-smi. Vrací null pokud GPU nedostupné nebo nvidia-smi chybí.
    /// </summary>
    private static (double? used, double? total) MeasureVram()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=memory.used,memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc == null) return (null, null);

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit(2000);

            // Output format: "1234, 16384"
            var parts = output.Trim().Split(',');
            if (parts.Length != 2) return (null, null);

            var usedMb  = int.Parse(parts[0].Trim());
            var totalMb = int.Parse(parts[1].Trim());

            var usedGb  = Math.Round(usedMb  / 1024.0, 1);
            var totalGb = Math.Round(totalMb / 1024.0, 1);

            return (usedGb, totalGb);
        }
        catch
        {
            // nvidia-smi nedostupné nebo GPU chybí
            return (null, null);
        }
    }
}
