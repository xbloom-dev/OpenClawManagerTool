using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog pro zobrazení Gateway logu s volbou kolik řádků zobrazit.
///
/// Konstruktor přijímá defaultLineCount — kolik řádků zobrazit při otevření.
/// Mapuje se na ComboBox položky s odpovídajícím Tag (string s číslem).
/// 0 = celý log.
/// </summary>
public partial class GatewayLogWindow : Window
{
    private readonly string _logFilePath;
    private readonly int _defaultLineCount;

    public GatewayLogWindow(string logFilePath, int defaultLineCount = 20)
    {
        InitializeComponent();
        _logFilePath = logFilePath;
        _defaultLineCount = defaultLineCount;

        TxtLogPath.Text = $"Soubor: {_logFilePath}";

        // Předvybrat položku v ComboBoxu podle defaultLineCount
        SelectComboBoxItemByTag(_defaultLineCount.ToString());

        BtnRefresh.Click += (_, _) => LoadLog();
        BtnClose.Click += (_, _) => Close();
        CmbLineCount.SelectionChanged += (_, _) => LoadLog();

        LoadLog();
    }

    /// <summary>
    /// Najde a vybere ComboBoxItem podle Tag hodnoty.
    /// Pokud nenajde, ponechá default (první položka v XAML).
    /// </summary>
    private void SelectComboBoxItemByTag(string tagValue)
    {
        foreach (var item in CmbLineCount.Items)
        {
            if (item is ComboBoxItem cbItem && cbItem.Tag is string tag && tag == tagValue)
            {
                CmbLineCount.SelectedItem = cbItem;
                return;
            }
        }

        // Fallback: vyber 20 řádků
        foreach (var item in CmbLineCount.Items)
        {
            if (item is ComboBoxItem cbItem && cbItem.Tag is string tag && tag == "20")
            {
                CmbLineCount.SelectedItem = cbItem;
                return;
            }
        }
    }

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
            int lineCount = 20;
            if (CmbLineCount.SelectedItem is ComboBoxItem item &&
                item.Tag is string tagStr &&
                int.TryParse(tagStr, out var parsed))
            {
                lineCount = parsed;
            }

            string[] allLines;
            using (var stream = new FileStream(_logFilePath, FileMode.Open,
                                               FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();
                allLines = content.Split('\n');
            }

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
            TxtLogContent.ScrollToEnd();
        }
        catch (Exception ex)
        {
            TxtLogContent.Text = $"Chyba při čtení logu:\n{ex.Message}";
            TxtStatus.Text = "Chyba při načítání.";
        }
    }
}
