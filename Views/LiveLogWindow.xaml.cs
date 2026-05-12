using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Non-modální okno pro živé sledování Gateway log souboru.
///
/// Chování:
/// - Otevře se s posledními N řádky (N = počet vybraný v GatewayLogWindow)
/// - FileSystemWatcher sleduje soubor — při každém zápisu přidá nové řádky
/// - Sliding window: když přibyde nový řádek, nejstarší zmizí
/// - Nové řádky jsou zvýrazněny žlutě (~2s, pak zblednou na normální barvu)
/// - Tlačítko Kopírovat s 2s feedback animací
/// - Non-modální — aplikace zůstane plně použitelná
/// </summary>
public partial class LiveLogWindow : Window
{
    private readonly string _logPath;
    private readonly int _windowSize;

    // Sliding window — drží posledních N řádků
    private readonly Queue<string> _lines;

    // Sledování souboru
    private FileSystemWatcher? _watcher;
    private long _fileOffset = 0;

    // Highlight timer pro nové řádky
    private DispatcherTimer? _highlightClearTimer;
    private DispatcherTimer? _feedbackTimer;

    // Počítadlo nových řádků pro status
    private int _newLinesTotal = 0;

    public LiveLogWindow(string logPath, int windowSize = 20)
    {
        InitializeComponent();
        _logPath    = logPath;
        _windowSize = windowSize > 0 ? windowSize : 20;
        _lines      = new Queue<string>(_windowSize + 1);

        BtnClose.Click += (_, _) => Close();
        BtnCopy.Click  += BtnCopy_Click;

        Closed += (_, _) =>
        {
            StopWatcher();
            _highlightClearTimer?.Stop();
            _feedbackTimer?.Stop();
        };

        ApplyLocalization();
        LoadInitialLines();
        StartWatcher();
    }

    private void ApplyLocalization()
    {
        bool cs = L10n.Current == L10n.Language.CS;
        Title              = cs ? "OpenClaw Manager — Živý log" : "OpenClaw Manager — Live log";
        BtnCopy.Content    = cs ? "Kopírovat" : "Copy";
        BtnClose.Content   = cs ? "Zavřít" : "Close";
        BtnCopy.ToolTip    = cs ? "Zkopíruje celý zobrazený log do schránky." : "Copies displayed log to clipboard.";
        TxtCopiedFeedback.Text = "✓ " + (cs ? "Zkopírováno" : "Copied");
    }

    // ==================== Inicializace ====================

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
            using (var fs = new FileStream(_logPath, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite))
            {
                using var reader = new StreamReader(fs);
                content = reader.ReadToEnd();
                _fileOffset = fs.Position;
            }

            var all = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var initial = _windowSize > 0 && all.Length > _windowSize
                ? all.Skip(all.Length - _windowSize).ToArray()
                : all;

            foreach (var line in initial)
            {
                if (_lines.Count >= _windowSize) _lines.Dequeue();
                _lines.Enqueue(line);
            }

            RefreshDisplay(highlightCount: 0);

            var fi     = new FileInfo(_logPath);
            var sizeKb = Math.Round(fi.Length / 1024.0, 1);
            bool cs    = L10n.Current == L10n.Language.CS;
            TxtLogPath.Text = cs
                ? $"Soubor: {_logPath}   |   Velikost: {sizeKb} KB   |   Živé sledování"
                : $"File: {_logPath}   |   Size: {sizeKb} KB   |   Live monitoring";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Chyba: {ex.Message}";
        }
    }

    // ==================== FileSystemWatcher ====================

    private void StartWatcher()
    {
        try
        {
            var dir  = Path.GetDirectoryName(_logPath);
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                bool cs = L10n.Current == L10n.Language.CS;
                TxtStatus.Text = cs
                    ? "Složka logu zatím neexistuje. Spusť Gateway a otevři živý log znovu."
                    : "Log folder does not exist yet. Start Gateway and reopen live log.";
                return;
            }

            var file = Path.GetFileName(_logPath);

            _watcher = new FileSystemWatcher(dir)
            {
                Filter              = file,
                NotifyFilter        = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnLogChanged;
            _watcher.Created += OnLogChanged;
            _watcher.Renamed += OnLogChanged;
        }
        catch { }
    }

    private void StopWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private void OnLogChanged(object sender, FileSystemEventArgs e)
    {
        // Přepnout na UI thread
        Dispatcher.BeginInvoke(ReadNewLines);
    }

    private void ReadNewLines()
    {
        if (!File.Exists(_logPath)) return;

        try
        {
            using var fs = new FileStream(_logPath, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite);

            if (_fileOffset > fs.Length)
                _fileOffset = 0; // log byl smazán/rotován

            fs.Seek(_fileOffset, SeekOrigin.Begin);

            using var reader = new StreamReader(fs);
            var newContent = reader.ReadToEnd();
            _fileOffset = fs.Position;

            if (string.IsNullOrEmpty(newContent)) return;

            var newLines = newContent.Split('\n',
                StringSplitOptions.RemoveEmptyEntries);

            if (newLines.Length == 0) return;

            foreach (var line in newLines)
            {
                if (_lines.Count >= _windowSize) _lines.Dequeue();
                _lines.Enqueue(line);
            }

            _newLinesTotal += newLines.Length;
            RefreshDisplay(highlightCount: newLines.Length);
            UpdateStatus();
        }
        catch { }
    }

    // ==================== Zobrazení ====================

    /// <summary>
    /// Překreslí TextBox. Posledních `highlightCount` řádků se zobrazí žlutě.
    /// Po 2s se zvýraznění odebere (fade na normální barvu).
    /// </summary>
    private void RefreshDisplay(int highlightCount)
    {
        var allLines = _lines.ToArray();

        // TextBox nepodporuje per-line barvy — použijeme RichTextBox přístup
        // Pro jednoduchost: normální řádky šedě (#DCDCDC), nové žlutě
        // Implementace: sestavit celý text, ale nové řádky označit vizuálně
        // pomocí prefixu "► " a po 2s obnovit bez prefixu

        if (highlightCount > 0)
        {
            var normalLines = allLines.Take(allLines.Length - highlightCount);
            var highlighted = allLines.Skip(allLines.Length - highlightCount);

            TxtLogContent.Text =
                string.Join("\n", normalLines) +
                (normalLines.Any() ? "\n" : "") +
                string.Join("\n", highlighted.Select(l => "► " + l));

            _highlightClearTimer ??= new DispatcherTimer
                { Interval = TimeSpan.FromSeconds(2) };
            _highlightClearTimer.Stop();
            _highlightClearTimer.Tick -= HighlightClearTimer_Tick;
            _highlightClearTimer.Tick += HighlightClearTimer_Tick;
            _highlightClearTimer.Start();
        }
        else
        {
            TxtLogContent.Text = string.Join("\n", allLines);
        }

        TxtLogContent.ScrollToEnd();
    }


    private void HighlightClearTimer_Tick(object? sender, EventArgs e)
    {
        _highlightClearTimer?.Stop();
        TxtLogContent.Text = string.Join("\n", _lines);
        TxtLogContent.ScrollToEnd();
    }

    private void UpdateStatus()
    {
        bool cs = L10n.Current == L10n.Language.CS;
        TxtStatus.Text = cs
            ? $"Sledování aktivní — zobrazeno {_lines.Count} řádků, nových od startu: {_newLinesTotal}"
            : $"Live monitoring — showing {_lines.Count} lines, new since start: {_newLinesTotal}";
    }

    // ==================== Kopírovat ====================

    private void BtnCopy_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Kopírovat čistý obsah (bez "► " prefixů)
            var clean = string.Join("\n", _lines);
            Clipboard.SetText(clean);
            ShowCopiedFeedback();
        }
        catch (Exception ex)
        {
            bool cs = L10n.Current == L10n.Language.CS;
            MessageBox.Show(
                cs ? $"Kopírování selhalo:\n{ex.Message}" : $"Copy failed:\n{ex.Message}",
                cs ? "Chyba" : "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowCopiedFeedback()
    {
        _feedbackTimer?.Stop();
        TxtCopiedFeedback.BeginAnimation(OpacityProperty,
            new DoubleAnimation { From = 0, To = 1,
                Duration = TimeSpan.FromMilliseconds(200) });
        _feedbackTimer = new DispatcherTimer
            { Interval = TimeSpan.FromMilliseconds(1800) };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer?.Stop();
            TxtCopiedFeedback.BeginAnimation(OpacityProperty,
                new DoubleAnimation { From = 1, To = 0,
                    Duration = TimeSpan.FromMilliseconds(200) });
        };
        _feedbackTimer.Start();
    }
}
