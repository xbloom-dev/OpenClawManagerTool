namespace OpenClawManager.Services;

/// <summary>
/// Tracks Gateway latency and keeps a moving average of the last requests.
/// </summary>
public static class LatencyTracker
{
    private const int WindowSize = 10;

    private static readonly Queue<int> _window = new();
    private static int? _lastMs = null;
    private static int? _maxMs = null;
    private static int _totalCount = 0;
    private static long _fileOffset = 0;
    private static string? _currentLogPath = null;

    /// <summary>
    /// Loads new log entries and updates latency stats.
    /// </summary>
    public static void Poll(string logPath)
    {
        if (_currentLogPath != logPath)
        {
            _fileOffset = 0;
            _currentLogPath = logPath;
        }

        var entries = LogMonitor.ParseNewLatencyEntries(logPath, ref _fileOffset);

        AddEntries(entries);
    }

    public static async Task PollAsync(string logPath)
    {
        if (_currentLogPath != logPath)
        {
            _fileOffset = 0;
            _currentLogPath = logPath;
        }

        var (entries, newOffset) = await LogMonitor.ParseNewLatencyEntriesAsync(logPath, _fileOffset);
        _fileOffset = newOffset;

        AddEntries(entries);
    }

    /// <summary>
    /// Resets stats after Gateway restart or log rotation.
    /// </summary>
    public static void Reset()
    {
        _window.Clear();
        _lastMs = null;
        _maxMs = null;
        _totalCount = 0;
        _fileOffset = 0;
        _currentLogPath = null;
    }

    /// <summary>
    /// Returns the current latency stats.
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

    private static void AddEntries(List<(string Method, int Ms)> entries)
    {
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
}

public record LatencyStats(int? LastMs, int? AvgMs, int? MaxMs, int Count);
