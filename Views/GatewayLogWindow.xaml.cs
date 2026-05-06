using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog pro zobrazení Gateway logu s volbou kolik řádků zobrazit.
/// Otevírá se z menu Soubor → Otevřít Gateway log.
///
/// Načítání logu:
/// - Soubor je otevřen v share-readwrite módu (může běžet Gateway současně)
/// - Pokud uživatel zvolí "Celý log" a soubor je velký, načte se vše bez varování
///   (typicky max ~5-10 MB v Gateway logu, není problém)
/// </summary>
public partial class GatewayLogWindow : Window
{
    private readonly string _logFilePath;

    public GatewayLogWindow(string logFilePath)
    {
        InitializeComponent();
        _logFilePath = logFilePath;

        TxtLogPath.Text = $"Soubor: {_logFilePath}";

        BtnRefresh.Click += (_, _) => LoadLog();
        BtnClose.Click += (_, _) => Close();
        CmbLineCount.SelectionChanged += (_, _) => LoadLog();

        // První načtení
        LoadLog();
    }

    /// <summary>
    /// Načte log podle aktuálního výběru v ComboBoxu.
    /// </summary>
    private void LoadLog()
    {
        if (!File.Exists(_logFilePath))
        {
            TxtLogContent.Text = $"Log soubor neexistuje:\n{_logFilePath}\n\nGateway zřejmě dnes ještě neběžel.";
            TxtStatus.Text = "Soubor neexistuje.";
            return;
        }

        try
        {
            // Kolik řádků zobrazit (0 = celý log)
            int lineCount = 20;
            if (CmbLineCount.SelectedItem is ComboBoxItem item &&
                item.Tag is string tagStr &&
                int.TryParse(tagStr, out var parsed))
            {
                lineCount = parsed;
            }

            // Načteme všechny řádky (sdílený přístup — Gateway může psát současně)
            string[] allLines;
            using (var stream = new FileStream(_logFilePath, FileMode.Open,
                                               FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                allLines = content.Split('\n');
            }

            // Vyber posledních N řádků (nebo všechno)
            string[] selectedLines;
            if (lineCount == 0 || lineCount >= allLines.Length)
            {
                selectedLines = allLines;
                TxtStatus.Text = $"Zobrazen celý log ({allLines.Length} řádků).";
            }
            else
            {
                selectedLines = allLines.Skip(Math.Max(0, allLines.Length - lineCount)).ToArray();
                TxtStatus.Text = $"Zobrazeno posledních {selectedLines.Length} z {allLines.Length} řádků.";
            }

            TxtLogContent.Text = string.Join("\n", selectedLines);

            // Auto-scroll na konec
            TxtLogContent.ScrollToEnd();
        }
        catch (Exception ex)
        {
            TxtLogContent.Text = $"Chyba při čtení logu:\n{ex.Message}";
            TxtStatus.Text = "Chyba při načítání.";
        }
    }
}
