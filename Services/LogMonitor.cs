using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OpenClawManager.Services;

/// <summary>
/// Sledování Gateway log souboru.
///
/// Dvě funkce:
/// 1. WaitForGatewayReady — polling dokud se neobjeví "gateway ready" v logu
/// 2. ParseLatencyEntries — parsování res ✓ záznamů pro LatencyTracker
///
/// Formát logu: každý řádek je JSON objekt s polem "message".
/// Zprávy obsahují ANSI escape sekvence — musí se stripovat před regex matchem.
///
/// Příklad res záznamu (po JSON parse a ANSI strip):
///   "⇄ res ✓ agents.list 145ms conn=db8d5be5…c09a id=e2670228…7d13"
/// </summary>
public static class LogMonitor
{
    // Regex pro ANSI escape sekvence (ESC + [ + ... + finální písmeno)
    private static readonly Regex AnsiRegex = new(@"\x1b\[[0-9;]*[mGKHFJA-Za-z]",
        RegexOptions.Compiled);

    // Regex pro latency záznamy — po ANSI strippingu
    // Příklad: "⇄ res ✓ agents.list 145ms conn=..."
    // Hledáme: "res" + whitespace + "✓" + whitespace + method + whitespace + číslo + "ms"
    private static readonly Regex LatencyRegex = new(@"res\s+✓\s+([\w\.]+)\s+(\d+)ms",
        RegexOptions.Compiled);

    /// <summary>
    /// Čeká na "gateway ready" v logu (polling každých 500ms).
    /// </summary>
    public static async Task<bool> WaitForGatewayReady(string logPath, int timeoutSeconds = 180)
    {
        var deadline = DateTime.Now.AddSeconds(timeoutSeconds);

        while (DateTime.Now < deadline)
        {
            if (File.Exists(logPath))
            {
                try
                {
                    var content = await ReadLogSafeAsync(logPath);
                    if (content.Contains("\"gateway ready\"") ||
                        ContainsGatewayReadyMessage(content))
                        return true;
                }
                catch { /* soubor se právě zapisuje, zkusíme znovu */ }
            }

            await Task.Delay(500);
        }

        return false;
    }

    private static bool ContainsGatewayReadyMessage(string rawContent)
    {
        // Projít každý řádek jako JSON a zkontrolovat message field
        foreach (var line in rawContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var msg = ExtractMessageField(line);
            if (msg != null && msg.Contains("gateway ready"))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Smaže log soubor pokud existuje (před novým startem Gateway).
    /// </summary>
    public static void DeleteLogIfExists(string logPath)
    {
        try
        {
            if (File.Exists(logPath))
                File.Delete(logPath);
        }
        catch { }
    }

    /// <summary>
    /// Parsuje latency záznamy z logu pro LatencyTracker.
    ///
    /// Algoritmus:
    /// 1. Přečíst nové řádky od posledního čtení (sledujeme offset)
    /// 2. Pro každý řádek: parse JSON → číst "message" field → strip ANSI → regex match
    /// 3. Vrátit seznam (method, ms) tuplů
    /// </summary>
    public static List<(string Method, int Ms)> ParseNewLatencyEntries(
        string logPath, ref long fileOffset)
    {
        var results = new List<(string, int)>();

        if (!File.Exists(logPath))
            return results;

        try
        {
            using var fs = new FileStream(logPath, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);

            // Přeskočit na pozici kde jsme skončili minule
            if (fileOffset > fs.Length)
                fileOffset = 0; // log byl smazán/rotován

            fs.Seek(fileOffset, SeekOrigin.Begin);

            using var reader = new StreamReader(fs);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var entry = TryParseLatencyLine(line);
                if (entry.HasValue)
                    results.Add(entry.Value);
            }

            fileOffset = fs.Position;
        }
        catch { }

        return results;
    }

    public static async Task<(List<(string Method, int Ms)> Entries, long NewOffset)> ParseNewLatencyEntriesAsync(
        string logPath, long fileOffset)
    {
        var results = new List<(string, int)>();

        if (!File.Exists(logPath))
            return (results, fileOffset);

        try
        {
            await using var fs = new FileStream(
                logPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 4096,
                options: FileOptions.Asynchronous);

            // Resume from the last parsed byte offset.
            if (fileOffset > fs.Length)
                fileOffset = 0; // The log was deleted or rotated.

            fs.Seek(fileOffset, SeekOrigin.Begin);

            using var reader = new StreamReader(fs);
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var entry = TryParseLatencyLine(line);
                if (entry.HasValue)
                    results.Add(entry.Value);
            }

            fileOffset = fs.Position;
        }
        catch { }

        return (results, fileOffset);
    }

    private static (string Method, int Ms)? TryParseLatencyLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        if (!line.Contains("res")) return null; // rychlý pre-filter

        // Extrahovat message field z JSON
        var message = ExtractMessageField(line);
        if (message == null) return null;

        // Stripovat ANSI escape sekvence
        var clean = AnsiRegex.Replace(message, "");

        // Aplikovat latency regex
        var match = LatencyRegex.Match(clean);
        if (!match.Success) return null;

        var method = match.Groups[1].Value;
        if (!int.TryParse(match.Groups[2].Value, out var ms)) return null;

        return (method, ms);
    }

    /// <summary>
    /// Extrahuje "message" field z JSON řádku.
    /// Používá JsonDocument pro robustní parsing (ne string search).
    /// </summary>
    private static string? ExtractMessageField(string jsonLine)
    {
        if (string.IsNullOrWhiteSpace(jsonLine)) return null;
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                return msgProp.GetString();
        }
        catch { }
        return null;
    }

    private static async Task<string> ReadLogSafeAsync(string path)
    {
        await using var fs = new FileStream(path, FileMode.Open,
            FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        return await reader.ReadToEndAsync();
    }
}
