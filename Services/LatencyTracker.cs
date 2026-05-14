namespace OpenClawManager.Services;

/// <summary>
/// Sleduje latence z Gateway logu — udržuje klouzavý průměr posledních 10 requestů.
///
/// Volá se z MainWindow.UpdateStatus() každé 2 sekundy.
/// Čte pouze nové řádky logu od posledního čtení (fileOffset tracking).
///
/// Reset() se volá při restartu Gateway (logPath se změní, offset = 0).
/// </summary>
public static class LatencyTracker
{
    private const int WindowSize = 10;

    private static readonly Queue<int> _window = new();
    private static int? _lastMs   = null; // O(1) last — Queue.Last() je O(n)
    private static int? _maxMs    = null;
    private static int  _totalCount = 0;
    private static long _fileOffset = 0;
    private static string? _currentLogPath = null;

    /// <summary>
    /// Načte nové záznamy z logu a aktualizuje statistiky.
    /// Volej periodicky (každé 2s) z UI timeru.
    /// </summary>
    public static void Poll(string logPath)
    {
        // Pokud se změnil log soubor (po restartu Gateway), resetovat offset
        if (_currentLogPath != logPath)
        {
            _fileOffset = 0;
            _currentLogPath = logPath;
        }

        var entries = LogMonitor.ParseNewLatencyEntries(logPath, ref _fileOffset);

        foreach (var (_, ms) in entries)
        {
            _lastMs = ms;
            _window.Enqueue(ms);
            if (_window.Count > WindowSize)
                _window.Dequeue();

            if (!_maxMs.HasValue || ms > _maxMs)
                _maxMs = ms;

            _totalCount++;
        }
    }

    /// <summary>
    /// Resetuje statistiky (volat při restartu Gateway).
    /// </summary>
    public static void Reset()
    {
        _window.Clear();
        _lastMs    = null;
        _maxMs     = null;
        _totalCount = 0;
        _fileOffset = 0;
        _currentLogPath = null;
    }

    /// <summary>
    /// Vrátí aktuální statistiky.
    /// </summary>
    public static LatencyStats GetStats()
    {
        if (_totalCount == 0)
            return new LatencyStats(null, null, null, 0);

        var avg = _window.Count > 0
            ? (int?)Math.Round(_window.Average())
            : null;

        return new LatencyStats(_lastMs, avg, _maxMs, _totalCount);
    }
}

public record LatencyStats(int? LastMs, int? AvgMs, int? MaxMs, int Count);
