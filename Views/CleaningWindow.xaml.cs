using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Dialog Cleaning Tool — kroky 1-6 + Gateway/Task management.
///
/// Sekce "OpenClaw Gateway" má 3 checkboxy:
/// 1. Dočasně vypnout Scheduled Task (vyžaduje admin)
/// 2. Zastavit běžící Gateway před cleanupem
/// 3. Zavřít všechna OpenClaw okna — aktivní jen pokud je zaškrtnutý checkbox 2
///
/// Pokud Gateway stále běží při kliknutí Spustit a checkbox 2 není zaškrtnutý,
/// zobrazí se původní MessageBox varování.
/// </summary>
public partial class CleaningWindow : Window
{
    private bool _taskWasDisabled = false;

    public CleaningWindow()
    {
        InitializeComponent();
        Title = L10n.IsCzech ? "OpenClaw Manager — Vyčistit soubory" : "OpenClaw Manager — Cleaning Tool";

        BtnDryRun.Click += (_, _) => RunCleanup(dryRun: true);
        BtnRun.Click += BtnRun_Click;
        BtnClose.Click += (_, _) => Close();

        // Když uživatel odškrtne "Zastavit Gateway", odškrtni i závislý checkbox
        ChkStopGateway.Unchecked += (_, _) => ChkCloseAll.IsChecked = false;

        AppendLog("Cleaning Tool připraven.");
        AppendLog("Vyber kroky a klikni Dry-run pro náhled, nebo Spustit pro mazání.");

        UpdateTaskStatus();
    }

    private void UpdateTaskStatus()
    {
        if (!ScheduledTaskService.Exists())
        {
            ChkManageTask.IsEnabled = false;
            ChkManageTask.IsChecked = false;
            TxtTaskStatus.Text = "Task 'OpenClaw Gateway' neexistuje.";
            return;
        }

        var isEnabled = ScheduledTaskService.IsEnabled();
        var adminLabel = AdminService.IsAdmin() ? "máš admin práva" : "BEZ admin práv";
        var stateLabel = isEnabled ? "zapnutý" : "vypnutý";
        TxtTaskStatus.Text = $"Task existuje a je {stateLabel} ({adminLabel}).";

        if (!AdminService.IsAdmin())
            TxtTaskStatus.Text += " Pro vypnutí budou potřeba admin práva.";
    }

    private void BtnRun_Click(object? sender, RoutedEventArgs e)
    {
        // 1. Zastavit Gateway pokud je checkbox zaškrtnutý
        if (ChkStopGateway.IsChecked == true && ProcessDetector.IsGatewayRunning())
        {
            AppendLog("Zastavuji Gateway...");

            if (ChkCloseAll.IsChecked == true)
            {
                // Zastavit Gateway + embedded TUI + PowerShell wrappery
                if (Owner is MainWindow main)
                    main.Terminal.StopTui();
                GatewayService.StopAndCloseTui();
                AppendLog("  ✓ Gateway zastaven, všechna OpenClaw okna zavřena");
            }
            else
            {
                GatewayService.Stop();
                AppendLog("  ✓ Gateway zastaven");
            }

            System.Threading.Thread.Sleep(1500);
        }
        else if (ChkStopGateway.IsChecked == false && ProcessDetector.IsGatewayRunning())
        {
            // Původní MessageBox varování — Gateway běží ale uživatel nezaškrtl stop
            var warn = MessageBox.Show(
                "OpenClaw Gateway aktuálně běží.\n\n" +
                "Cleaning Tool může smazat soubory které Gateway používá " +
                "(zejména session locky a browser cache).\n\n" +
                "Pokračovat přesto?",
                "Gateway běží",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (warn != MessageBoxResult.Yes)
            {
                AppendLog("Spuštění zrušeno (Gateway běží).");
                return;
            }
        }

        // 2. Scheduled Task — vyžaduje admin
        if (ChkManageTask.IsChecked == true && ScheduledTaskService.Exists())
        {
            if (!AdminService.IsAdmin())
            {
                var elevated = AdminService.PromptForElevation(
                    "Vypnutí Scheduled Task 'OpenClaw Gateway' vyžaduje admin práva.");

                if (elevated)
                    return;

                // Odmítl — pokračujeme bez task managementu
                var skip = MessageBox.Show(
                    "Pokračovat bez vypnutí Scheduled Task?",
                    "Bez admin práv",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (skip != MessageBoxResult.Yes)
                {
                    AppendLog("Spuštění zrušeno.");
                    return;
                }

                ChkManageTask.IsChecked = false;
            }
        }

        // 3. Potvrzení mazání
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

        // Disable Scheduled Task před cleanupem
        if (!dryRun && ChkManageTask.IsChecked == true
            && ScheduledTaskService.Exists() && AdminService.IsAdmin())
        {
            AppendLog("Vypínám Scheduled Task 'OpenClaw Gateway'...");
            if (ScheduledTaskService.Disable())
            {
                AppendLog("  ✓ Task vypnut");
                _taskWasDisabled = true;
            }
            else
            {
                AppendLog("  ⚠ Vypnutí selhalo (pokračuji)");
            }
            AppendLog("");
        }

        int totalFiles = 0;
        long totalBytes = 0;
        var keepSessions = (int)SliderKeep.Value;

        foreach (var stepNum in steps)
        {
            var result = CleanupService.RunStep(stepNum, dryRun, AppendLog, keepSessions);
            if (result.ErrorMessage != null)
                AppendLog($"  ✗ Chyba: {result.ErrorMessage}");
            totalFiles += result.FilesProcessed;
            totalBytes += result.BytesProcessed;
            AppendLog("");
        }

        // Enable Scheduled Task po cleanupu
        if (_taskWasDisabled)
        {
            AppendLog("Zapínám Scheduled Task zpět...");
            if (ScheduledTaskService.Enable())
                AppendLog("  ✓ Task zapnut");
            else
                AppendLog("  ⚠ Zapnutí selhalo — zapni ručně přes Plánovač úloh");
            _taskWasDisabled = false;
            AppendLog("");
        }

        var action = dryRun ? "by smazalo" : "smazáno";
        AppendLog($"==== {label} HOTOVO: {action} {totalFiles} položek ({CleanupService.FormatBytes(totalBytes)}) ====");

        TxtSummary.Text = $"Celkem: {totalFiles} položek";
        TxtSummarySize.Text = CleanupService.FormatBytes(totalBytes);

        SetButtonsEnabled(true);
        UpdateTaskStatus();
    }

    private List<int> GetSelectedSteps()
    {
        var result = new List<int>();
        if (ChkStep1.IsChecked == true) result.Add(1);
        if (ChkStep2.IsChecked == true) result.Add(2);
        if (ChkStep3.IsChecked == true) result.Add(3);
        if (ChkStep4.IsChecked == true) result.Add(4);
        if (ChkStep5.IsChecked == true) result.Add(5);
        if (ChkStep6.IsChecked == true) result.Add(6);
        if (ChkStep7.IsChecked == true) result.Add(7);
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
        ChkStep6.IsEnabled = enabled;
        ChkStep7.IsEnabled = enabled;
        SliderKeep.IsEnabled = enabled && ChkStep6.IsChecked == true;
        ChkManageTask.IsEnabled = enabled;
        ChkStopGateway.IsEnabled = enabled;
        ChkCloseAll.IsEnabled = enabled && ChkStopGateway.IsChecked == true;
    }

    private void AppendLog(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        TxtLog.AppendText($"[{ts}] {message}\n");
        TxtLog.ScrollToEnd();
    }
}
