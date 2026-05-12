using System.IO;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Okno pro zobrazení Gateway log souboru.
/// Zachovává původní funkcionalitu (dropdown, Aktualizovat, StatusBar).
/// Nově: Kopírovat (s 2s fade feedback) + Živá data (otevře LiveLogWindow, toto zavře).
/// </summary>
public partial class GatewayLogWindow : Window
{
    private readonly string _logPath;
    private DispatcherTimer? _feedbackTimer;

    public GatewayLogWindow(string logPath, int defaultLines = 20)
    {
        InitializeComponent();
        _logPath = logPath;

        SelectLineCount(defaultLines);

        BtnRefresh.Click += (_, _) => LoadLog();
        BtnClose.Click += (_, _) => Close();
        BtnCopy.Click += BtnCopy_Click;
        BtnLiveLog.Click += BtnLiveLog_Click;
        CmbLineCount.SelectionChanged += (_, _) => LoadLog();

        ApplyLocalization();
        LoadLog();
    }

    private void ApplyLocalization()
    {
        bool cs = L10n.Current == L10n.Language.CS;
        BtnRefresh.Content  = cs ? "Aktualizovat" : "Refresh";
        BtnCopy.Content     = cs ? "Kopírovat" : "Copy";
        BtnClose.Content    = cs ? "Zavřít" : "Close";
        BtnLiveLog.Content  = cs ? "Živá data" : "Live log";
        BtnRefresh.ToolTip  = cs ? "Znovu načte obsah logu ze souboru." : "Reloads log content from file.";
        BtnCopy.ToolTip     = cs ? "Zkopíruje celý zobrazený log do schránky." : "Copies displayed log to clipboard.";
        BtnLiveLog.ToolTip  = cs
            ? "Otevře okno pro živé sledování logu. Toto okno se zavře."
            : "Opens live log monitoring window. This window will close.";
        TxtCopiedFeedback.Text = "✓ " + (cs ? "Zkopírováno" : "Copied");
    }

    private void SelectLineCount(int lines)
    {
        foreach (System.Windows.Controls.ComboBoxItem item in CmbLineCount.Items)
        {
            if (item.Tag is string tag && tag == lines.ToString())
            {
                CmbLineCount.SelectedItem = item;
                return;
            }
        }
        if (CmbLineCount.Items.Count > 1)
            CmbLineCount.SelectedIndex = 1; // výchozí: 20 řádků
    }

    private int GetSelectedLineCount()
    {
        if (CmbLineCount.SelectedItem is System.Windows.Controls.ComboBoxItem item &&
            item.Tag is string tag && int.TryParse(tag, out var n))
            return n;
        return 20;
    }

    private void LoadLog()
    {
        var lines = GetSelectedLineCount();

        if (!File.Exists(_logPath))
        {
            bool cs2 = L10n.Current == L10n.Language.CS;
            TxtLogPath.Text   = $"{(cs2 ? "Soubor" : "File")}: {_logPath}";
            TxtLogContent.Text = cs2 ? "(soubor neexistuje)" : "(file not found)";
            TxtStatus.Text    = cs2 ? "Soubor nenalezen." : "File not found.";
            return;
        }

        try
        {
            string content;
            using (var fs = new FileStream(_logPath, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
                content = reader.ReadToEnd();

            var allLines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var display  = lines > 0 && allLines.Length > lines
                ? allLines.Skip(allLines.Length - lines).ToArray()
                : allLines;

            TxtLogContent.Text = string.Join("\n", display);
            TxtLogContent.ScrollToEnd();

            var fi    = new FileInfo(_logPath);
            var sizeKb = Math.Round(fi.Length / 1024.0, 1);
            bool cs   = L10n.Current == L10n.Language.CS;

            TxtLogPath.Text = cs
                ? $"Soubor: {_logPath}   |   Velikost: {sizeKb} KB   |   Zobrazeno řádků: {display.Length}"
                : $"File: {_logPath}   |   Size: {sizeKb} KB   |   Lines shown: {display.Length}";

            TxtStatus.Text = cs
                ? (lines == 0 ? $"Zobrazen celý log ({display.Length} řádků)."
                              : $"Zobrazeno posledních {display.Length} řádků.")
                : (lines == 0 ? $"Showing entire log ({display.Length} lines)."
                              : $"Showing last {display.Length} lines.");
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Chyba: {ex.Message}";
        }
    }

    private void BtnCopy_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(TxtLogContent.Text);
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
            new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(200) });
        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1800) };
        _feedbackTimer.Tick += (_, _) =>
        {
            _feedbackTimer?.Stop();
            TxtCopiedFeedback.BeginAnimation(OpacityProperty,
                new DoubleAnimation { From = 1, To = 0, Duration = TimeSpan.FromMilliseconds(200) });
        };
        _feedbackTimer.Start();
    }

    private void BtnLiveLog_Click(object? sender, RoutedEventArgs e)
    {
        var lines = GetSelectedLineCount();
        var live = new LiveLogWindow(_logPath, lines);
        live.Show();
        Close();
    }
}
