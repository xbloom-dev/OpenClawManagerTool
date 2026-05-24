using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class LiveLogViewModel : ObservableObject, IDisposable
{
    private readonly string _logPath;
    private readonly int _windowSize;
    private readonly Queue<string> _lines;
    private FileSystemWatcher? _fileWatcher;
    private long _fileOffset;
    private int _newLinesTotal;

    public LiveLogViewModel(string logPath, int windowSize = 20)
    {
        _logPath = logPath;
        _windowSize = windowSize > 0 ? windowSize : 20;
        _lines = new Queue<string>(_windowSize + 1);

        RefreshLocalization();
        LoadInitialLines();
        StartWatcher();
    }

    public event EventHandler? NewLinesAppended;
    public event EventHandler? HighlightClearRequested;
    public event EventHandler? LogChanged;

    [ObservableProperty]
    private string _logContent = "";

    [ObservableProperty]
    private string _logPathInfo = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _windowTitle = "";

    [ObservableProperty]
    private string _copyText = "";

    [ObservableProperty]
    private string _closeText = "";

    [ObservableProperty]
    private string _copyToolTip = "";

    [ObservableProperty]
    private string _closeToolTip = "";

    [ObservableProperty]
    private string _copiedFeedbackText = "";

    [ObservableProperty]
    private string _copyFailedTitle = "";

    [ObservableProperty]
    private string _copyFailedMessagePrefix = "";

    public string CleanContent => string.Join("\n", _lines);

    public void RefreshLocalization()
    {
        var cs = L10n.Current == L10n.Language.CS;

        WindowTitle = cs ? "Živý log" : "Live log";
        CopyText = cs ? "Kopírovat" : "Copy";
        CloseText = cs ? "Zavřít" : "Close";
        CopyToolTip = cs ? "Zkopíruje celý zobrazený log do schránky." : "Copies displayed log to clipboard.";
        CloseToolTip = cs ? "Zavře okno živého logu." : "Closes the live log window.";
        CopiedFeedbackText = "✓ " + (cs ? "Zkopírováno" : "Copied");
        CopyFailedTitle = cs ? "Chyba" : "Error";
        CopyFailedMessagePrefix = cs ? "Kopírování selhalo:" : "Copy failed:";
    }

    public void ReadNewLines()
    {
        if (!File.Exists(_logPath))
            return;

        try
        {
            using var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (_fileOffset > stream.Length)
                _fileOffset = 0;

            stream.Seek(_fileOffset, SeekOrigin.Begin);

            using var reader = new StreamReader(stream);
            var newContent = reader.ReadToEnd();
            _fileOffset = stream.Position;

            if (string.IsNullOrEmpty(newContent))
                return;

            var newLines = newContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (newLines.Length == 0)
                return;

            foreach (var line in newLines)
                EnqueueLine(line);

            _newLinesTotal += newLines.Length;
            RefreshDisplay(newLines.Length);
            UpdateStatus();
        }
        catch
        {
        }
    }

    public void ClearHighlight()
    {
        LogContent = string.Join("\n", _lines);
        NewLinesAppended?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        StopWatcher();
    }

    private void LoadInitialLines()
    {
        if (!File.Exists(_logPath))
        {
            UpdateStatus();
            return;
        }

        try
        {
            string content;
            using (var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using var reader = new StreamReader(stream);
                content = reader.ReadToEnd();
                _fileOffset = stream.Position;
            }

            var allLines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var initialLines = _windowSize > 0 && allLines.Length > _windowSize
                ? allLines.Skip(allLines.Length - _windowSize).ToArray()
                : allLines;

            foreach (var line in initialLines)
                EnqueueLine(line);

            RefreshDisplay(highlightCount: 0);

            var fileInfo = new FileInfo(_logPath);
            var sizeKb = Math.Round(fileInfo.Length / 1024.0, 1);
            var cs = L10n.Current == L10n.Language.CS;

            LogPathInfo = cs
                ? $"Soubor: {_logPath}   |   Velikost: {sizeKb} KB   |   Živé sledování"
                : $"File: {_logPath}   |   Size: {sizeKb} KB   |   Live monitoring";
        }
        catch (Exception ex)
        {
            StatusText = $"Chyba: {ex.Message}";
        }
    }

    private void StartWatcher()
    {
        try
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                var cs = L10n.Current == L10n.Language.CS;
                StatusText = cs
                    ? "Složka logu zatím neexistuje. Spusť Gateway a otevři živý log znovu."
                    : "Log folder does not exist yet. Start Gateway and reopen live log.";
                return;
            }

            _fileWatcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(_logPath),
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnLogChanged;
            _fileWatcher.Created += OnLogChanged;
            _fileWatcher.Renamed += OnLogChanged;
        }
        catch
        {
        }
    }

    private void StopWatcher()
    {
        if (_fileWatcher == null)
            return;

        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Changed -= OnLogChanged;
        _fileWatcher.Created -= OnLogChanged;
        _fileWatcher.Renamed -= OnLogChanged;
        _fileWatcher.Dispose();
        _fileWatcher = null;
    }

    private void OnLogChanged(object sender, FileSystemEventArgs e)
    {
        LogChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnqueueLine(string line)
    {
        if (_lines.Count >= _windowSize)
            _lines.Dequeue();

        _lines.Enqueue(line);
    }

    private void RefreshDisplay(int highlightCount)
    {
        var allLines = _lines.ToArray();

        if (highlightCount > 0)
        {
            var normalLines = allLines.Take(allLines.Length - highlightCount);
            var highlightedLines = allLines.Skip(allLines.Length - highlightCount);

            LogContent =
                string.Join("\n", normalLines) +
                (normalLines.Any() ? "\n" : "") +
                string.Join("\n", highlightedLines.Select(line => "► " + line));

            HighlightClearRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            LogContent = string.Join("\n", allLines);
        }

        NewLinesAppended?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateStatus()
    {
        var cs = L10n.Current == L10n.Language.CS;
        StatusText = cs
            ? $"Sledování aktivní — zobrazeno {_lines.Count} řádků, nových od startu: {_newLinesTotal}"
            : $"Live monitoring — showing {_lines.Count} lines, new since start: {_newLinesTotal}";
    }
}
