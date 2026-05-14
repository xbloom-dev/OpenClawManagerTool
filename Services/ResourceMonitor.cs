using System.Diagnostics;
using System.Management;

namespace OpenClawManager.Services;

/// <summary>
/// Měření systémových zdrojů: RAM, CPU, VRAM.
///
/// Measure() je synchronní a blokující — volat výhradně z background tasku.
/// MeasureAsync() je správná entry-point pro UI timer (neblokuje UI thread).
///
/// RAM/CPU jsou cachované na 4s — WMI dotazy trvají 50–200ms každý,
/// spouštět je každé 2s bylo příčinou UI lag a sekání tlačítek.
/// VRAM se měří každé 2s (nvidia-smi je rychlý a hodnotnější čerstvý).
/// </summary>
public static class ResourceMonitor
{
    public record ResourceSnapshot(
        double RamUsedGb,
        double RamTotalGb,
        int    CpuPercent,
        double? VramUsedGb,
        double? VramTotalGb);

    // Cache pro pomalé WMI dotazy (RAM, CPU) — obnovuje se každé 4s
    private static ResourceSnapshot? _lastSnapshot;
    private static DateTime          _lastMeasureTime = DateTime.MinValue;
    private static readonly TimeSpan  CacheInterval = TimeSpan.FromSeconds(4);

    // Zámek — Measure() může být volán z více threadů (Task.Run)
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Asynchronní entry-point pro UI timer.
    /// Spouští Measure() na thread pool — UI thread není blokován.
    /// </summary>
    public static Task<ResourceSnapshot> MeasureAsync()
        => Task.Run(Measure);

    /// <summary>
    /// Synchronní měření. NEPOUŽÍVAT přímo z UI threadu.
    /// </summary>
    public static ResourceSnapshot Measure()
    {
        _lock.Wait();
        try
        {
            var now = DateTime.UtcNow;
            var useCache = _lastSnapshot != null && (now - _lastMeasureTime) < CacheInterval;

            double ramUsed, ramTotal;
            int cpu;

            if (useCache)
            {
                ramUsed  = _lastSnapshot!.RamUsedGb;
                ramTotal = _lastSnapshot!.RamTotalGb;
                cpu      = _lastSnapshot!.CpuPercent;
            }
            else
            {
                (ramUsed, ramTotal) = MeasureRam();
                cpu = MeasureCpu();
                _lastMeasureTime = now;
            }

            // VRAM se měří vždy — nvidia-smi je rychlý a hodnota se mění rychle
            var (vramUsed, vramTotal) = MeasureVram();

            var snap = new ResourceSnapshot(ramUsed, ramTotal, cpu, vramUsed, vramTotal);
            _lastSnapshot = snap;
            return snap;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static (double used, double total) MeasureRam()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            using var results = searcher.Get();

            foreach (ManagementObject os in results)
            {
                var totalKb = Convert.ToDouble(os["TotalVisibleMemorySize"]);
                var freeKb  = Convert.ToDouble(os["FreePhysicalMemory"]);
                var totalGb = Math.Round(totalKb / 1024 / 1024, 1);
                var freeGb  = Math.Round(freeKb  / 1024 / 1024, 1);
                return (Math.Round(totalGb - freeGb, 1), totalGb);
            }
        }
        catch { }
        return (0, 0);
    }

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
        catch { return 0; }
    }

    private static (double? used, double? total) MeasureVram()
    {
        Process? proc = null;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = "nvidia-smi",
                Arguments              = "--query-gpu=memory.used,memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            proc = Process.Start(psi);
            if (proc == null) return (null, null);

            var output = proc.StandardOutput.ReadToEnd();

            // Kill pokud nvidia-smi visí — timeout 2s
            if (!proc.WaitForExit(2000))
            {
                proc.Kill();
                return (null, null);
            }

            var parts = output.Trim().Split(',');
            if (parts.Length != 2) return (null, null);

            if (!int.TryParse(parts[0].Trim(), out var usedMb)  ||
                !int.TryParse(parts[1].Trim(), out var totalMb))
                return (null, null);

            return (Math.Round(usedMb / 1024.0, 1), Math.Round(totalMb / 1024.0, 1));
        }
        catch { return (null, null); }
        finally { proc?.Dispose(); }
    }
}
