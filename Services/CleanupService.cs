using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenClawManager.Services;

public record CleanupStep(int Number, string Title, string Description);

public record CleanupStepResult(
    int StepNumber,
    int FilesProcessed,
    long BytesProcessed,
    string? ErrorMessage,
    bool Skipped);

/// <summary>
/// Logika čištění OpenClaw souborů — port z cleanup.ps1 (kroky 1-6).
///
/// Bezpečnostní vrstvy:
/// - Dry-run: jen spočítá, nic nemaže
/// - Selhání jednoho souboru neshodí celý krok
/// - Krok 6 vždy vytvoří .bak před zápisem, rollback při chybě
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
        new(6, "sessions.json",        "Pro každého agenta zachovat N nejnovějších sessions"),
        new(7, "Token Manager zálohy", "*.bak soubory v Token Manager složce (opt-in)"),
    };

    /// <summary>
    /// Default jména agentů pro krok 6 (sessions.json cleanup).
    /// </summary>
    public static readonly string[] DefaultAgents = { "main", "researcher", "executive", "safety" };

    /// <summary>
    /// Možné klíče pro timestamp v session JSON objektu.
    /// </summary>
    private static readonly string[] TimestampKeys =
    {
        "createdAt", "timestamp", "updatedAt", "lastModified", "mtime", "created_at"
    };

    public static CleanupStepResult RunStep(
        int stepNumber,
        bool dryRun,
        Action<string> logCallback,
        int keepSessions = 10)
    {
        var settings = OpenClawManager.App.GetService<ISettingsService>().Settings;

        try
        {
            return stepNumber switch
            {
                1 => CleanLogs(settings, dryRun, logCallback),
                2 => CleanBackups(settings, dryRun, logCallback),
                3 => CleanStabilityReports(settings, dryRun, logCallback),
                4 => CleanBrowserCache(settings, dryRun, logCallback),
                5 => CleanSessionLocks(settings, dryRun, logCallback),
                6 => CleanSessionsJson(settings, dryRun, logCallback, keepSessions),
                7 => CleanTokenManagerBackups(settings, dryRun, logCallback),
                _ => new CleanupStepResult(stepNumber, 0, 0, $"Neznámý krok: {stepNumber}", true)
            };
        }
        catch (Exception ex)
        {
            return new CleanupStepResult(stepNumber, 0, 0, ex.Message, false);
        }
    }

    // ==================== KROKY 1-5 ====================

    private static CleanupStepResult CleanLogs(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        log("[1/6] Staré logy...");
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
        log("[2/6] Zálohy konfigurace (ponechat 2 nejnovější)...");
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
        log("[3/6] Stability reporty (> 3 dny)...");
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
        log("[4/6] Browser cache (> 1 den)...");
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
        log("[5/6] Session locky...");
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

    // ==================== KROK 6: sessions.json cleanup ====================

    /// <summary>
    /// Krok 6: pro každého agenta v ~\.openclaw\agents\{agent}\sessions.json
    /// zachovat N nejnovějších sessions, zbytek smazat.
    ///
    /// Bezpečnost:
    /// - Vytvoří sessions.json.bak před zápisem
    /// - Při chybě zápisu rollback ze zálohy
    /// - JSON struktura může být array nebo object — zvládneme oboje
    /// - Sessions bez detekovatelného timestamp se řadí jako nejstarší
    /// </summary>
    private static CleanupStepResult CleanSessionsJson(
        Models.AppSettings settings, bool dryRun, Action<string> log, int keepSessions)
    {
        log($"[6/6] sessions.json cleanup (zachovat {keepSessions} nejnovějších)...");

        var agentsPath = Path.Combine(settings.OpenClawPath, "agents");
        if (!Directory.Exists(agentsPath))
        {
            log($"  → Adresář neexistuje: {agentsPath}");
            return new CleanupStepResult(6, 0, 0, null, true);
        }

        int totalRemoved = 0;
        long totalBytesSaved = 0;
        bool anyError = false;

        foreach (var agent in DefaultAgents)
        {
            var sessionsPath = Path.Combine(agentsPath, agent, "sessions.json");
            if (!File.Exists(sessionsPath))
            {
                log($"  → Agent '{agent}': sessions.json neexistuje, přeskakuji");
                continue;
            }

            var result = CleanupSessionsForAgent(agent, sessionsPath, keepSessions, dryRun, log);
            if (result.Error)
            {
                anyError = true;
                continue;
            }

            totalRemoved += result.Removed;
            totalBytesSaved += result.BytesSaved;
        }

        if (totalRemoved == 0 && !anyError)
        {
            log("  → žádné sessions ke smazání");
        }

        return new CleanupStepResult(6, totalRemoved, totalBytesSaved, null, false);
    }

    private record SessionCleanupResult(int Removed, long BytesSaved, bool Error);

    private static SessionCleanupResult CleanupSessionsForAgent(
        string agent, string sessionsPath, int keep, bool dryRun, Action<string> log)
    {
        long sizeBefore = new FileInfo(sessionsPath).Length;

        JsonNode? root;
        try
        {
            var raw = File.ReadAllText(sessionsPath);
            if (string.IsNullOrWhiteSpace(raw))
            {
                log($"  → Agent '{agent}': prázdný soubor, přeskakuji");
                return new SessionCleanupResult(0, 0, false);
            }
            root = JsonNode.Parse(raw);
        }
        catch (Exception ex)
        {
            log($"  ⚠ Agent '{agent}': JSON parse selhal: {ex.Message}");
            return new SessionCleanupResult(0, 0, true);
        }

        if (root == null)
        {
            log($"  ⚠ Agent '{agent}': prázdný JSON");
            return new SessionCleanupResult(0, 0, false);
        }

        // Sběr (key, session, timestamp) tuplů — zvládá array i object
        var entries = ExtractEntries(root);
        bool isArray = root is JsonArray;

        if (entries.Count <= keep)
        {
            log($"  → Agent '{agent}': {entries.Count} sessions, beze změny");
            return new SessionCleanupResult(0, 0, false);
        }

        // Seřadit nejnovější první (sessions bez timestamp = nejstarší)
        var sorted = entries
            .OrderByDescending(e => e.Timestamp ?? DateTime.MinValue)
            .ToList();

        var kept = sorted.Take(keep).ToList();
        var removedCount = entries.Count - keep;

        log($"  → Agent '{agent}': {entries.Count} sessions → ponechat {keep}, smazat {removedCount}");

        if (dryRun)
        {
            return new SessionCleanupResult(removedCount, 0, false);
        }

        // Záloha PŘED zápisem
        var bakPath = sessionsPath + ".bak";
        try
        {
            File.Copy(sessionsPath, bakPath, overwrite: true);
        }
        catch (Exception ex)
        {
            log($"  ⚠ Agent '{agent}': záloha selhala, NEZAPISUJI: {ex.Message}");
            return new SessionCleanupResult(0, 0, true);
        }

        // Vytvořit nový JSON ve stejné struktuře
        try
        {
            JsonNode newRoot;
            if (isArray)
            {
                // Zachovat původní pořadí v poli — kept jsme přesortovali, vrátit zpět dle Key (int index)
                var newArr = new JsonArray();
                foreach (var e in kept.OrderBy(x => int.TryParse(x.Key, out var i) ? i : 0))
                {
                    newArr.Add(e.Session?.DeepClone());
                }
                newRoot = newArr;
            }
            else
            {
                var newObj = new JsonObject();
                foreach (var e in kept)
                {
                    newObj[e.Key] = e.Session?.DeepClone();
                }
                newRoot = newObj;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = newRoot.ToJsonString(options);
            File.WriteAllText(sessionsPath, newJson);

            long sizeAfter = new FileInfo(sessionsPath).Length;
            long bytesSaved = sizeBefore - sizeAfter;

            log($"  ✓ Agent '{agent}': smazáno {removedCount} sessions, ušetřeno {FormatBytes(bytesSaved)} (záloha: {Path.GetFileName(bakPath)})");

            return new SessionCleanupResult(removedCount, bytesSaved, false);
        }
        catch (Exception ex)
        {
            log($"  ⚠ Agent '{agent}': zápis selhal, OBNOVUJI ze zálohy: {ex.Message}");
            try
            {
                File.Copy(bakPath, sessionsPath, overwrite: true);
                log($"  ✓ Agent '{agent}': obnoveno ze zálohy");
            }
            catch (Exception rollbackEx)
            {
                log($"  ✗ Agent '{agent}': ROLLBACK SELHAL: {rollbackEx.Message}");
            }
            return new SessionCleanupResult(0, 0, true);
        }
    }

    private record SessionEntry(string Key, JsonNode? Session, DateTime? Timestamp);

    /// <summary>
    /// Extrahuje seznam sessions z JSON root (zvládá array i object strukturu).
    /// </summary>
    private static List<SessionEntry> ExtractEntries(JsonNode root)
    {
        var result = new List<SessionEntry>();

        if (root is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var ts = ParseTimestamp(arr[i]);
                result.Add(new SessionEntry(i.ToString(), arr[i], ts));
            }
        }
        else if (root is JsonObject obj)
        {
            foreach (var kvp in obj)
            {
                var ts = ParseTimestamp(kvp.Value);
                result.Add(new SessionEntry(kvp.Key, kvp.Value, ts));
            }
        }

        return result;
    }

    /// <summary>
    /// Zkusí najít timestamp v JSON objektu pomocí různých klíčů a formátů.
    /// Akceptuje: ISO 8601 string, Unix epoch (sec/ms jako int nebo string).
    /// </summary>
    private static DateTime? ParseTimestamp(JsonNode? session)
    {
        if (session is not JsonObject obj) return null;

        foreach (var key in TimestampKeys)
        {
            if (!obj.ContainsKey(key)) continue;
            var val = obj[key];
            if (val == null) continue;

            // Zkusíme nejprve string (ISO 8601 nebo numeric-as-string)
            try
            {
                var str = val.GetValue<string>();
                if (DateTime.TryParse(str, out var dt)) return dt;
                if (long.TryParse(str, out var epoch)) return FromEpoch(epoch);
            }
            catch { /* není string, zkusíme číslo */ }

            // Zkusíme číselný timestamp
            try
            {
                var num = val.GetValue<long>();
                return FromEpoch(num);
            }
            catch { /* neúspěch, zkusíme další klíč */ }

            try
            {
                var num = (long)val.GetValue<double>();
                return FromEpoch(num);
            }
            catch { }
        }

        return null;
    }

    /// <summary>
    /// Konverze Unix epoch (sec nebo ms) na DateTime. Heuristika: > 10^10 = ms, jinak sec.
    /// </summary>
    private static DateTime FromEpoch(long epoch)
    {
        if (epoch > 10_000_000_000L)
            return DateTimeOffset.FromUnixTimeMilliseconds(epoch).LocalDateTime;
        else
            return DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
    }

    // ==================== POMOCNÉ ====================

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

    // ==================== KROK 7 — TOKEN MANAGER ZÁLOHY ====================

    /// <summary>
    /// Krok 7 — smaže *.bak soubory vzniklé z TokenService.RestoreFileInPlace
    /// v Token Manager složce (~/.token-manager/ nebo cesta z nastavení).
    ///
    /// Opt-in (výchozí vypnuto) — zálohy jsou poslední záchrana pro případ
    /// chyby při restore-inplace. Mazat vědomě.
    /// </summary>
    private static CleanupStepResult CleanTokenManagerBackups(
        Models.AppSettings settings, bool dryRun, Action<string> log)
    {
        const int stepNumber = 7;
        log("[7] Token Manager zálohy:");

        // Složka se odvozuje od cesty k vaultu — zálohy jsou vedle secrets.json
        var vaultPath = settings.TokenManagerSecretsPath;
        var vaultDir  = Path.GetDirectoryName(vaultPath);

        if (string.IsNullOrWhiteSpace(vaultDir) || !Directory.Exists(vaultDir))
        {
            log("  → složka Token Manageru neexistuje, přeskočeno.");
            return new CleanupStepResult(stepNumber, 0, 0, null, true);
        }

        var bakFiles = Directory
            .GetFiles(vaultDir, "*.bak", SearchOption.TopDirectoryOnly)
            .Select(f => new FileInfo(f))
            .OrderBy(f => f.Name)
            .ToList();

        if (bakFiles.Count == 0)
        {
            log("  → žádné *.bak soubory nenalezeny.");
            return new CleanupStepResult(stepNumber, 0, 0, null, false);
        }

        var totalSize = bakFiles.Sum(f => f.Length);
        log($"  nalezeno {bakFiles.Count} *.bak souborů ({FormatBytes(totalSize)}):");

        foreach (var f in bakFiles)
            log($"    {f.Name} ({FormatBytes(f.Length)})");

        if (dryRun)
            return new CleanupStepResult(stepNumber, bakFiles.Count, totalSize, null, false);

        int deleted = 0;
        long deletedBytes = 0;

        foreach (var file in bakFiles)
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

}
