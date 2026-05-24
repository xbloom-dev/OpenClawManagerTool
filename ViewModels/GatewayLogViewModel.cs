using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class GatewayLogViewModel : ObservableObject
{
    private readonly string _logPath;

    public GatewayLogViewModel(string logPath, int defaultLines = 20)
    {
        _logPath = logPath;
        DefaultLines = defaultLines;
        RefreshLocalization();
    }

    public int DefaultLines { get; }
    public string LogPath => _logPath;

    [ObservableProperty]
    private string _logContent = "";

    [ObservableProperty]
    private string _logPathInfo = "";

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _windowTitle = "";

    [ObservableProperty]
    private string _refreshText = "";

    [ObservableProperty]
    private string _copyText = "";

    [ObservableProperty]
    private string _liveLogText = "";

    [ObservableProperty]
    private string _closeText = "";

    [ObservableProperty]
    private string _refreshToolTip = "";

    [ObservableProperty]
    private string _copyToolTip = "";

    [ObservableProperty]
    private string _liveLogToolTip = "";

    [ObservableProperty]
    private string _closeToolTip = "";

    [ObservableProperty]
    private string _copiedFeedbackText = "";

    [ObservableProperty]
    private string _copyFailedTitle = "";

    [ObservableProperty]
    private string _copyFailedMessagePrefix = "";

    public void RefreshLocalization()
    {
        var cs = L10n.Current == L10n.Language.CS;

        WindowTitle = "Gateway Log";
        RefreshText = cs ? "Aktualizovat" : "Refresh";
        CopyText = cs ? "Kopírovat" : "Copy";
        LiveLogText = cs ? "Živá data" : "Live log";
        CloseText = cs ? "Zavřít" : "Close";
        RefreshToolTip = cs ? "Znovu načte obsah logu ze souboru." : "Reloads log content from file.";
        CopyToolTip = cs ? "Zkopíruje celý zobrazený log do schránky." : "Copies displayed log to clipboard.";
        LiveLogToolTip = cs
            ? "Otevře okno pro živé sledování logu. Toto okno se zavře."
            : "Opens live log monitoring window. This window will close.";
        CloseToolTip = cs ? "Zavře okno s logem." : "Closes the log window.";
        CopiedFeedbackText = "✓ " + (cs ? "Zkopírováno" : "Copied");
        CopyFailedTitle = cs ? "Chyba" : "Error";
        CopyFailedMessagePrefix = cs ? "Kopírování selhalo:" : "Copy failed:";

        if (string.IsNullOrEmpty(StatusText) || StatusText is "Připraveno." or "Ready.")
            StatusText = cs ? "Připraveno." : "Ready.";
    }

    public void LoadLog(int selectedLines)
    {
        var cs = L10n.Current == L10n.Language.CS;

        if (!File.Exists(_logPath))
        {
            LogPathInfo = $"{(cs ? "Soubor" : "File")}: {_logPath}";
            LogContent = cs ? "(soubor neexistuje)" : "(file not found)";
            StatusText = cs ? "Soubor nenalezen." : "File not found.";
            return;
        }

        try
        {
            string content;
            using (var fs = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                content = reader.ReadToEnd();
            }

            var allLines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var display = selectedLines > 0 && allLines.Length > selectedLines
                ? allLines.Skip(allLines.Length - selectedLines).ToArray()
                : allLines;

            LogContent = string.Join("\n", display);

            var fileInfo = new FileInfo(_logPath);
            var sizeKb = Math.Round(fileInfo.Length / 1024.0, 1);

            LogPathInfo = cs
                ? $"Soubor: {_logPath}   |   Velikost: {sizeKb} KB   |   Zobrazeno řádků: {display.Length}"
                : $"File: {_logPath}   |   Size: {sizeKb} KB   |   Lines shown: {display.Length}";

            StatusText = cs
                ? (selectedLines == 0 ? $"Zobrazen celý log ({display.Length} řádků)." : $"Zobrazeno posledních {display.Length} řádků.")
                : (selectedLines == 0 ? $"Showing entire log ({display.Length} lines)." : $"Showing last {display.Length} lines.");
        }
        catch (Exception ex)
        {
            StatusText = cs ? $"Chyba: {ex.Message}" : $"Error: {ex.Message}";
        }
    }
}
