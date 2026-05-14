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
    private static readonly SemaphoreSlim MeasureLock = new(1, 1);
    private static readonly TimeSpan WmiCacheDuration = TimeSpan.FromSeconds(4);
    private static ResourceSnapshot? _lastSnapshot;
    private static DateTime _lastWmiMeasureUtc = DateTime.MinValue;

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
    public static async Task<ResourceSnapshot> MeasureAsync(CancellationToken cancellationToken = default)
    {
        await MeasureLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTime.UtcNow;
            var useWmiCache = _lastSnapshot != null &&
                (now - _lastWmiMeasureUtc) < WmiCacheDuration;

            double ramUsed;
            double ramTotal;
            int cpu;

            if (useWmiCache)
            {
                ramUsed = _lastSnapshot!.RamUsedGb;
                ramTotal = _lastSnapshot.RamTotalGb;
                cpu = _lastSnapshot.CpuPercent;
            }
            else
            {
                (ramUsed, ramTotal) = MeasureRam();
                cpu = MeasureCpu();
                _lastWmiMeasureUtc = now;
            }

            var (vramUsed, vramTotal) = await MeasureVramAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = new ResourceSnapshot(ramUsed, ramTotal, cpu, vramUsed, vramTotal);
            _lastSnapshot = snapshot;

            return snapshot;
        }
        finally
        {
            MeasureLock.Release();
        }
    }

    /// <summary>
    /// Synchronní kompatibilní wrapper. UI má používat MeasureAsync().
    /// </summary>
    public static ResourceSnapshot Measure()
    {
        return MeasureAsync().GetAwaiter().GetResult();
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
    private static async Task<(double? used, double? total)> MeasureVramAsync(CancellationToken cancellationToken)
    {
        Process? proc = null;
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

            proc = Process.Start(psi);
            if (proc == null) return (null, null);

            var outputTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = proc.StandardError.ReadToEndAsync(cancellationToken);
            var exitTask = proc.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var completed = await Task.WhenAny(exitTask, timeoutTask).ConfigureAwait(false);
            if (completed != exitTask)
            {
                KillProcessTree(proc);
                return (null, null);
            }

            await exitTask.ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            _ = await errorTask.ConfigureAwait(false);

            // Output format: "1234, 16384"
            var parts = output.Trim().Split(',');
            if (parts.Length != 2) return (null, null);

            if (!int.TryParse(parts[0].Trim(), out var usedMb) ||
                !int.TryParse(parts[1].Trim(), out var totalMb))
                return (null, null);

            var usedGb  = Math.Round(usedMb  / 1024.0, 1);
            var totalGb = Math.Round(totalMb / 1024.0, 1);

            return (usedGb, totalGb);
        }
        catch
        {
            // nvidia-smi nedostupné nebo GPU chybí
            return (null, null);
        }
        finally
        {
            proc?.Dispose();
        }
    }

    private static void KillProcessTree(Process proc)
    {
        try
        {
            if (!proc.HasExited)
                proc.Kill(entireProcessTree: true);
        }
        catch
        {
            // best effort only
        }
    }
}
