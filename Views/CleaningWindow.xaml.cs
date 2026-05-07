using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog Cleaning Tool — port logiky z cleanup.ps1 (kroky 1-5).
///
/// Záměrně bez Recycle Bin — dry-run je primární bezpečnost.
/// Mazání je vždy trvalé (File.Delete).
/// </summary>
public partial class CleaningWindow : Window
{
    public CleaningWindow()
    {
        InitializeComponent();

        BtnDryRun.Click += (_, _) => RunCleanup(dryRun: true);
        BtnRun.Click += BtnRun_Click;
        BtnClose.Click += (_, _) => Close();

        AppendLog("Cleaning Tool připraven.");
        AppendLog("Vyber kroky a klikni Dry-run pro náhled, nebo Spustit pro mazání.");
    }

    private void BtnRun_Click(object? sender, RoutedEventArgs e)
    {
        // Varování pokud Gateway běží
        if (ProcessDetector.IsGatewayRunning())
        {
            var warn = MessageBox.Show(
                "OpenClaw Gateway aktuálně běží.\n\n" +
                "Cleaning Tool může smazat soubory které Gateway používá.\n\n" +
                "Doporučeno: zastavit Gateway před cleanupem.\n\nPokračovat přesto?",
                "Gateway běží",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (warn != MessageBoxResult.Yes)
            {
                AppendLog("Spuštění zrušeno (Gateway běží).");
                return;
            }
        }

        // Potvrzení mazání
        var confirm = MessageBox.Show(
            "Opravdu trvale smazat vybrané soubory?\nTuto akci nelze vrátit.",
            "Potvrzení mazání",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            AppendLog("Spuštění zrušeno.");
            return;
        }

        RunCleanup(dryRun: false);
    }

    private void RunCleanup(bool dryRun)
    {
        SetButtonsEnabled(false);
        TxtLog.Clear();

        var label = dryRun ? "[DRY-RUN]" : "[MAZÁNÍ]";
        AppendLog($"==== {label} START ====");

        var steps = GetSelectedSteps();
        if (steps.Count == 0)
        {
            AppendLog("Žádné kroky nejsou vybrány.");
            SetButtonsEnabled(true);
            return;
        }

        int totalFiles = 0;
        long totalBytes = 0;

        foreach (var stepNum in steps)
        {
            var result = CleanupService.RunStep(stepNum, dryRun, AppendLog);

            if (result.ErrorMessage != null)
                AppendLog($"  ✗ Chyba: {result.ErrorMessage}");

            totalFiles += result.FilesProcessed;
            totalBytes += result.BytesProcessed;
            AppendLog("");
        }

        var action = dryRun ? "by smazalo" : "smazáno";
        AppendLog($"==== {label} HOTOVO: {action} {totalFiles} souborů ({CleanupService.FormatBytes(totalBytes)}) ====");

        TxtSummary.Text = $"Celkem: {totalFiles} souborů";
        TxtSummarySize.Text = CleanupService.FormatBytes(totalBytes);

        SetButtonsEnabled(true);
    }

    private List<int> GetSelectedSteps()
    {
        var result = new List<int>();
        if (ChkStep1.IsChecked == true) result.Add(1);
        if (ChkStep2.IsChecked == true) result.Add(2);
        if (ChkStep3.IsChecked == true) result.Add(3);
        if (ChkStep4.IsChecked == true) result.Add(4);
        if (ChkStep5.IsChecked == true) result.Add(5);
        return result;
    }

    private void SetButtonsEnabled(bool enabled)
    {
        BtnDryRun.IsEnabled = enabled;
        BtnRun.IsEnabled = enabled;
        ChkStep1.IsEnabled = enabled;
        ChkStep2.IsEnabled = enabled;
        ChkStep3.IsEnabled = enabled;
        ChkStep4.IsEnabled = enabled;
        ChkStep5.IsEnabled = enabled;
    }

    private void AppendLog(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        TxtLog.AppendText($"[{ts}] {message}\n");
        TxtLog.ScrollToEnd();
    }
}
