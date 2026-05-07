using System.IO;

namespace OpenClawManager.Services;

/// <summary>
/// Reprezentace jednoho kroku Cleaning Toolu.
/// </summary>
public record CleanupStep(int Number, string Title, string Description);

/// <summary>
/// Výsledek běhu jednoho kroku.
/// </summary>
public record CleanupStepResult(
    int StepNumber,
    int FilesProcessed,
    long BytesProcessed,
    string? ErrorMessage,
    bool Skipped);

/// <summary>
/// Logika čištění OpenClaw souborů — port z cleanup.ps1 (kroky 1-5).
///
/// Bezpečnostní vrstvy:
/// - Dry-run: jen spočítá, nic nemaže
/// - Selhání jednoho souboru neshodí celý krok
/// - Logging každé akce přes callback
///
/// Záměrně bez Recycle Bin — přidáváme minimální závislosti.
/// Dry-run je dostatečná bezpečnostní vrstva pro power usera.
/// </summary>
public static class CleanupService
{
    public static readonly CleanupStep[] AllSteps =
    {
        new(1, "Staré logy",           "Gateway log soubory starší než dnešek"),
        new(2, "Zálohy konfigurace",   "openclaw.json.bak* — ponechat 2 nejnovější"),
        new(3, "Stability reporty",    "Soubory v logs/stability/ starší než 3 dny"),
        new(4, "Browser cache",        "Obsah browser-data/ starší než 1 den"),
        new(5, "Session locky",        "Všechny *.lock soubory v agents/*/sessions/"),
    };

    public static CleanupStepResult RunStep(
        int stepNumber,
        bool dryRun,
        Action<string> logCallback)
    {
        var settings = SettingsService.Current;

        try
        {
            return stepNumber switch
            {
                1 => CleanLogs(settings, dryRun, logCallback),
                2 => CleanBackups(settings, dryRun, logCallback),
                3 => CleanStabilityReports(settings, dryRun, logCallback),
                4 => CleanBrowserCache(settings, dryRun, logCallback),
                5 => CleanSessionLocks(settings, dryRun, logCallback),
                _ => new CleanupStepResult(stepNumber, 0, 0, $"Neznámý krok: {stepNumber}", true)
            };
        }
        catch (Exception ex)
        {
            return new CleanupStepResult(stepNumber, 0, 0, ex.Message, false);
        }
    }

    private static CleanupStepResult CleanLogs(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        log("[1/5] Staré logy...");

        if (!Directory.Exists(settings.TempPath))
        {
            log($"  → Temp složka neexistuje: {settings.TempPath}");
            return new CleanupStepResult(1, 0, 0, null, true);
        }

        var today = DateTime.Today;
        var files = Directory.GetFiles(settings.TempPath, "openclaw-*.log")
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTime < today)
            .ToList();

        return DeleteFiles(1, files, dryRun, log);
    }

    private static CleanupStepResult CleanBackups(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        log("[2/5] Zálohy konfigurace (ponechat 2 nejnovější)...");

        if (!Directory.Exists(settings.OpenClawPath))
        {
            log($"  → OpenClaw složka neexistuje: {settings.OpenClawPath}");
            return new CleanupStepResult(2, 0, 0, null, true);
        }

        var files = Directory.GetFiles(settings.OpenClawPath, "openclaw.json.bak*")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .Skip(2)
            .ToList();

        return DeleteFiles(2, files, dryRun, log);
    }

    private static CleanupStepResult CleanStabilityReports(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        log("[3/5] Stability reporty (> 3 dny)...");

        var stabPath = Path.Combine(settings.OpenClawPath, "logs", "stability");
        if (!Directory.Exists(stabPath))
        {
            log($"  → Adresář neexistuje: {stabPath}");
            return new CleanupStepResult(3, 0, 0, null, true);
        }

        var limit = DateTime.Now.AddDays(-3);
        var files = Directory.GetFiles(stabPath)
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTime < limit)
            .ToList();

        return DeleteFiles(3, files, dryRun, log);
    }

    private static CleanupStepResult CleanBrowserCache(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        log("[4/5] Browser cache (> 1 den)...");

        var cachePath = Path.Combine(settings.OpenClawPath, "browser-data");
        if (!Directory.Exists(cachePath))
        {
            log($"  → Adresář neexistuje: {cachePath}");
            return new CleanupStepResult(4, 0, 0, null, true);
        }

        var limit = DateTime.Now.AddDays(-1);
        var files = Directory.GetFiles(cachePath, "*", SearchOption.AllDirectories)
            .Select(f => new FileInfo(f))
            .Where(f => f.LastWriteTime < limit)
            .ToList();

        return DeleteFiles(4, files, dryRun, log);
    }

    private static CleanupStepResult CleanSessionLocks(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        log("[5/5] Session locky...");

        var agentsPath = Path.Combine(settings.OpenClawPath, "agents");
        if (!Directory.Exists(agentsPath))
        {
            log($"  → Adresář neexistuje: {agentsPath}");
            return new CleanupStepResult(5, 0, 0, null, true);
        }

        var allLocks = new List<FileInfo>();
        foreach (var agentDir in Directory.GetDirectories(agentsPath))
        {
            var sessionsDir = Path.Combine(agentDir, "sessions");
            if (Directory.Exists(sessionsDir))
            {
                allLocks.AddRange(
                    Directory.GetFiles(sessionsDir, "*.lock").Select(f => new FileInfo(f)));
            }
        }

        return DeleteFiles(5, allLocks, dryRun, log);
    }

    private static CleanupStepResult DeleteFiles(
        int stepNumber, List<FileInfo> files, bool dryRun, Action<string> log)
    {
        if (files.Count == 0)
        {
            log("  → žádné soubory ke smazání");
            return new CleanupStepResult(stepNumber, 0, 0, null, false);
        }

        long totalBytes = files.Sum(f => f.Length);
        log($"  → nalezeno {files.Count} souborů ({FormatBytes(totalBytes)})");

        if (dryRun)
        {
            log("  → DRY-RUN: nic se nemaže");
            return new CleanupStepResult(stepNumber, files.Count, totalBytes, null, false);
        }

        int deleted = 0;
        long deletedBytes = 0;

        foreach (var file in files)
        {
            try
            {
                long size = file.Length;
                file.Delete();
                deleted++;
                deletedBytes += size;
            }
            catch (Exception ex)
            {
                log($"  ⚠ Selhalo mazání '{file.Name}': {ex.Message}");
            }
        }

        log($"  → smazáno: {deleted} souborů ({FormatBytes(deletedBytes)})");
        return new CleanupStepResult(stepNumber, deleted, deletedBytes, null, false);
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
