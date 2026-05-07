using System.IO;
using System.Text.RegularExpressions;

namespace OpenClawManager.Services;

/// <summary>
/// Sledování Gateway log souboru.
/// Používá se pro:
/// - Detekci "gateway ready" během SPUSTIT TUI sekvence
/// - Parsing latencí z 'res' záznamů (pro LatencyTracker)
///
/// Přístup k souboru je vždy přes FileShare.ReadWrite — Gateway aktivně zapisuje
/// do logu zatímco aplikace čte.
/// </summary>
public static class LogMonitor
{
    /// <summary>
    /// Regex pro res záznamy v Gateway logu.
    /// Příklady:
    ///   res ✓ sessions.list 12ms
    ///   res ✓ chat.send 1234ms
    ///   res ✗ method.name 5678ms (chyba)
    ///
    /// Capture groups:
    ///   1: status (✓ nebo ✗)
    ///   2: method name
    ///   3: latence v ms
    /// </summary>
    private static readonly Regex ResRecordPattern = new Regex(
        @"res\s+([✓✗])\s+([\w\.]+)\s+(\d+)ms",
        RegexOptions.Compiled);

    /// <summary>
    /// Smaže Gateway log soubor pokud existuje.
    /// </summary>
    public static void DeleteLogIfExists(string logPath)
    {
        try
        {
            if (File.Exists(logPath))
            {
                File.Delete(logPath);
            }
        }
        catch
        {
            // Pokud nelze smazat, pokračuj — detekce ready bude i tak fungovat
        }
    }

    /// <summary>
    /// Čeká na řetězec "gateway ready" v logu.
    /// </summary>
    public static async Task<bool> WaitForGatewayReady(
        string logPath,
        int timeoutSeconds = 180,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.Now.AddSeconds(timeoutSeconds);

        while (DateTime.Now < deadline)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            if (File.Exists(logPath))
            {
                try
                {
                    using var stream = new FileStream(logPath, FileMode.Open,
                                                      FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync(cancellationToken);

                    if (content.Contains("\"gateway ready\""))
                        return true;
                }
                catch { }
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    /// <summary>
    /// Záznam z parsování Gateway logu — jeden res záznam.
    /// </summary>
    public record LogEntry(string Method, int LatencyMs, bool Success);

    /// <summary>
    /// Načte všechny res záznamy z aktuálního Gateway logu.
    /// Vrací pole v pořadí jak jsou v logu (nejstarší první).
    /// </summary>
    public static List<LogEntry> ParseAllResEntries(string logPath)
    {
        var result = new List<LogEntry>();

        if (!File.Exists(logPath)) return result;

        try
        {
            using var stream = new FileStream(logPath, FileMode.Open,
                                              FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            var matches = ResRecordPattern.Matches(content);
            foreach (Match m in matches)
            {
                var status = m.Groups[1].Value;
                var method = m.Groups[2].Value;
                if (int.TryParse(m.Groups[3].Value, out var ms))
                {
                    result.Add(new LogEntry(method, ms, status == "✓"));
                }
            }
        }
        catch { }

        return result;
    }
}
