namespace OpenClawManager.Services;

/// <summary>
/// Statistiky latencí pro UI sekce "Měření latence".
/// </summary>
public record LatencyStats(
    int? LastMs,
    double? AvgMs,
    int? MaxMs,
    int Count);

/// <summary>
/// Sledování latencí Gateway requestů.
/// Načítá záznamy z Gateway logu přes LogMonitor.ParseAllResEntries
/// a počítá:
/// - Poslední latence
/// - Klouzavý průměr posledních 10 záznamů
/// - Maximum všech záznamů v aktuální Gateway session
/// - Celkový počet requestů
///
/// Resetuje se při restartu Gateway (nový log soubor → nové statistiky).
/// </summary>
public static class LatencyTracker
{
    /// <summary>
    /// Vrátí aktuální statistiky latencí na základě Gateway logu.
    /// Pokud log neexistuje nebo neobsahuje žádné res záznamy, vrátí null hodnoty.
    /// </summary>
    public static LatencyStats GetStats()
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();
        var entries = LogMonitor.ParseAllResEntries(logPath);

        if (entries.Count == 0)
        {
            return new LatencyStats(null, null, null, 0);
        }

        var lastMs = entries[entries.Count - 1].LatencyMs;
        var maxMs = entries.Max(e => e.LatencyMs);

        // Průměr posledních 10 záznamů
        var lastTen = entries.Skip(Math.Max(0, entries.Count - 10)).ToList();
        var avgMs = lastTen.Average(e => (double)e.LatencyMs);

        return new LatencyStats(lastMs, avgMs, maxMs, entries.Count);
    }
}
