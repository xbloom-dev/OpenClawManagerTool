using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public sealed partial class CleaningViewModel : ObservableObject
{
    private readonly IGatewayService _gatewayService;
    private readonly ISettingsService _settingsService;
    private readonly ICleanupService _cleanupService;
    private readonly IProcessDetector _processDetector;
    private bool _taskWasDisabled;

    public CleaningViewModel(
        IGatewayService gatewayService,
        ISettingsService settingsService,
        ICleanupService cleanupService,
        IProcessDetector processDetector)
    {
        _gatewayService = gatewayService;
        _settingsService = settingsService;
        _cleanupService = cleanupService;
        _processDetector = processDetector;

        RefreshLocalization();
        RefreshTaskStatus();
        AppendLog(ReadyLogText);
        AppendLog(InstructionLogText);
    }

    public event EventHandler<string>? LogMessageAppended;

    public Action? StopEmbeddedTuiAction { get; set; }
    public Action? CloseAction { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreOptionsEnabled))]
    [NotifyPropertyChangedFor(nameof(IsCloseAllEnabled))]
    [NotifyPropertyChangedFor(nameof(IsManageTaskEnabled))]
    [NotifyPropertyChangedFor(nameof(IsKeepSessionsEnabled))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isStep1Selected = true;

    [ObservableProperty]
    private bool _isStep2Selected = true;

    [ObservableProperty]
    private bool _isStep3Selected;

    [ObservableProperty]
    private bool _isStep4Selected = true;

    [ObservableProperty]
    private bool _isStep5Selected = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKeepSessionsEnabled))]
    private bool _isStep6Selected = true;

    [ObservableProperty]
    private bool _isStep7Selected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsManageTaskEnabled))]
    private bool _isManageTaskAvailable = true;

    [ObservableProperty]
    private bool _isManageTaskSelected = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCloseAllEnabled))]
    private bool _isStopGatewaySelected;

    [ObservableProperty]
    private bool _isCloseAllSelected;

    [ObservableProperty]
    private int _keepSessions = 10;

    [ObservableProperty]
    private string _summary = "";

    [ObservableProperty]
    private string _summarySize = "";

    [ObservableProperty]
    private string _taskStatus = "";

    [ObservableProperty]
    private string _windowTitle = "";

    [ObservableProperty]
    private string _stepsHeader = "";

    [ObservableProperty]
    private string _gatewayHeader = "";

    [ObservableProperty]
    private string _logHeader = "";

    [ObservableProperty]
    private string _step1Text = "";

    [ObservableProperty]
    private string _step2Text = "";

    [ObservableProperty]
    private string _step3Text = "";

    [ObservableProperty]
    private string _step4Text = "";

    [ObservableProperty]
    private string _step5Text = "";

    [ObservableProperty]
    private string _step6Text = "";

    [ObservableProperty]
    private string _step7Text = "";

    [ObservableProperty]
    private string _sessionsPerAgentText = "";

    [ObservableProperty]
    private string _manageTaskText = "";

    [ObservableProperty]
    private string _stopGatewayText = "";

    [ObservableProperty]
    private string _closeAllText = "";

    [ObservableProperty]
    private string _dryRunText = "";

    [ObservableProperty]
    private string _runText = "";

    [ObservableProperty]
    private string _closeText = "";

    [ObservableProperty]
    private string _dryRunToolTip = "";

    [ObservableProperty]
    private string _runToolTip = "";

    [ObservableProperty]
    private string _closeToolTip = "";

    private string ReadyLogText { get; set; } = "";
    private string InstructionLogText { get; set; } = "";

    public bool AreOptionsEnabled => !IsBusy;
    public bool IsCloseAllEnabled => !IsBusy && IsStopGatewaySelected;
    public bool IsManageTaskEnabled => !IsBusy && IsManageTaskAvailable;
    public bool IsKeepSessionsEnabled => !IsBusy && IsStep6Selected;

    private bool Cs => L10n.IsCzech;
    private string T(string cs, string en) => Cs ? cs : en;

    partial void OnIsBusyChanged(bool value)
    {
        DryRunCommand.NotifyCanExecuteChanged();
        RunCleanupCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsStopGatewaySelectedChanged(bool value)
    {
        if (!value)
            IsCloseAllSelected = false;
    }

    public void RefreshLocalization()
    {
        WindowTitle = T("Vyčistit soubory", "Cleaning Tool");
        StepsHeader = T("Kroky čištění", "Cleanup steps");
        GatewayHeader = "OpenClaw Gateway";
        LogHeader = T("Průběh / Log", "Progress / Log");

        Step1Text = T("[1] Staré logy - Gateway log soubory starší než dnešek", "[1] Old logs - Gateway log files older than today");
        Step2Text = T("[2] Zálohy konfigurace - openclaw.json.bak* (ponechat 2 nejnovější)", "[2] Configuration backups - openclaw.json.bak* (keep 2 newest)");
        Step3Text = T("[3] Stability reporty - logs/stability/ starší než 3 dny (opt-in)", "[3] Stability reports - logs/stability/ older than 3 days (opt-in)");
        Step4Text = T("[4] Browser cache - browser-data/ starší než 1 den", "[4] Browser cache - browser-data/ older than 1 day");
        Step5Text = T("[5] Session locky - všechny *.lock v agents/*/sessions/", "[5] Session locks - all *.lock files in agents/*/sessions/");
        Step6Text = T("[6] sessions.json - zachovat:", "[6] sessions.json - keep:");
        Step7Text = T("[7] Token Manager zálohy - *.bak soubory v Token Manager složce (opt-in)", "[7] Token Manager backups - *.bak files in the Token Manager folder (opt-in)");
        SessionsPerAgentText = T("sessions/agent", "sessions/agent");

        ManageTaskText = T("Dočasně vypnout Scheduled Task 'OpenClaw Gateway' během cleanupu", "Temporarily disable the 'OpenClaw Gateway' scheduled task during cleanup");
        StopGatewayText = T("Zastavit běžící Gateway před cleanupem", "Stop the running Gateway before cleanup");
        CloseAllText = T("Zavřít všechna OpenClaw okna (embedded TUI + PowerShell wrappery)", "Close all OpenClaw windows (embedded TUI + PowerShell wrappers)");

        Summary = T("Připraveno.", "Ready.");
        DryRunText = T("Náhled", "Preview");
        RunText = T("Spustit", "Run");
        CloseText = T("Zavřít", "Close");
        DryRunToolTip = T("Zobrazí co by se smazalo, nic nemaže.", "Shows what would be deleted without deleting anything.");
        RunToolTip = T("POZOR: Trvale smaže vybrané soubory. Akci nelze vrátit.", "WARNING: Permanently deletes selected files. This cannot be undone.");
        CloseToolTip = T("Zavře Cleaning Tool.", "Closes Cleaning Tool.");

        ReadyLogText = T("Cleaning Tool připraven.", "Cleaning Tool ready.");
        InstructionLogText = T("Vyber kroky a klikni Dry-run pro náhled, nebo Spustit pro mazání.", "Select steps and click Preview or Run.");
    }

    public void RefreshTaskStatus()
    {
        if (!ScheduledTaskService.Exists())
        {
            IsManageTaskAvailable = false;
            IsManageTaskSelected = false;
            TaskStatus = T("Task 'OpenClaw Gateway' neexistuje.", "Task 'OpenClaw Gateway' does not exist.");
            return;
        }

        IsManageTaskAvailable = true;
        var isEnabled = ScheduledTaskService.IsEnabled();
        var adminLabel = AdminService.IsAdmin() ? T("máš admin práva", "admin rights available") : T("BEZ admin práv", "NO admin rights");
        var stateLabel = isEnabled ? T("zapnutý", "enabled") : T("vypnutý", "disabled");
        TaskStatus = T($"Task existuje a je {stateLabel} ({adminLabel}).", $"Task exists and is {stateLabel} ({adminLabel}).");

        if (!AdminService.IsAdmin())
            TaskStatus += T(" Pro vypnutí budou potřeba admin práva.", " Admin rights are required to disable it.");
    }

    [RelayCommand(CanExecute = nameof(CanRunCommands))]
    private Task DryRunAsync() => ExecuteCleanupAsync(dryRun: true);

    [RelayCommand(CanExecute = nameof(CanRunCommands))]
    private async Task RunCleanupAsync()
    {
        if (!await PrepareForDestructiveRunAsync())
            return;

        await ExecuteCleanupAsync(dryRun: false);
    }

    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Close() => CloseAction?.Invoke();

    private bool CanRunCommands() => !IsBusy;
    private bool CanClose() => !IsBusy;

    private async Task<bool> PrepareForDestructiveRunAsync()
    {
        if (IsStopGatewaySelected && _processDetector.IsGatewayRunning())
        {
            AppendLog(T("Zastavuji Gateway...", "Stopping Gateway..."));

            if (IsCloseAllSelected)
            {
                StopEmbeddedTuiAction?.Invoke();
                await Task.Run(() => _gatewayService.StopAndCloseTui());
                AppendLog(T("  OK Gateway zastaven, všechna OpenClaw okna zavřena", "  OK Gateway stopped, all OpenClaw windows closed"));
            }
            else
            {
                await Task.Run(() => _gatewayService.Stop());
                AppendLog(T("  OK Gateway zastaven", "  OK Gateway stopped"));
            }

            await Task.Delay(1500);
        }
        else if (!IsStopGatewaySelected && _processDetector.IsGatewayRunning())
        {
            var warn = MessageBox.Show(
                T(
                    "OpenClaw Gateway aktuálně běží.\n\nCleaning Tool může smazat soubory které Gateway používá (zejména session locky a browser cache).\n\nPokračovat přesto?",
                    "OpenClaw Gateway is currently running.\n\nCleaning Tool can delete files used by Gateway, especially session locks and browser cache.\n\nContinue anyway?"),
                T("Gateway běží", "Gateway running"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (warn != MessageBoxResult.Yes)
            {
                AppendLog(T("Spuštění zrušeno (Gateway běží).", "Run canceled (Gateway is running)."));
                return false;
            }
        }

        if (IsManageTaskSelected && ScheduledTaskService.Exists() && !AdminService.IsAdmin())
        {
            var elevated = AdminService.PromptForElevation(
                T("Vypnutí Scheduled Task 'OpenClaw Gateway' vyžaduje admin práva.", "Disabling the 'OpenClaw Gateway' scheduled task requires admin rights."));

            if (elevated)
                return false;

            var skip = MessageBox.Show(
                T("Pokračovat bez vypnutí Scheduled Task?", "Continue without disabling the scheduled task?"),
                T("Bez admin práv", "No admin rights"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (skip != MessageBoxResult.Yes)
            {
                AppendLog(T("Spuštění zrušeno.", "Run canceled."));
                return false;
            }

            IsManageTaskSelected = false;
        }

        var confirm = MessageBox.Show(
            T("Opravdu trvale smazat vybrané soubory?\nTuto akci nelze vrátit.", "Permanently delete selected files?\nThis action cannot be undone."),
            T("Potvrzení mazání", "Confirm deletion"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
            return true;

        AppendLog(T("Spuštění zrušeno.", "Run canceled."));
        return false;
    }

    private async Task ExecuteCleanupAsync(bool dryRun)
    {
        IsBusy = true;
        Summary = "";
        SummarySize = "";
        ClearLog();

        var label = dryRun ? "[DRY-RUN]" : T("[MAZÁNÍ]", "[DELETE]");
        AppendLog($"==== {label} START ====");

        var steps = GetSelectedSteps();
        if (steps.Count == 0)
        {
            AppendLog(T("Žádné kroky nejsou vybrány.", "No steps selected."));
            IsBusy = false;
            return;
        }

        var keepSessions = KeepSessions;
        var manageTask = IsManageTaskSelected;

        try
        {
            var result = await Task.Run(() => RunCleanupCore(dryRun, steps, keepSessions, manageTask, label));

            Summary = T($"Celkem: {result.TotalFiles} položek", $"Total: {result.TotalFiles} items");
            SummarySize = _cleanupService.FormatBytes(result.TotalBytes);
        }
        catch (Exception ex)
        {
            AppendLog(T($"ERROR Cleanup selhal: {ex.Message}", $"ERROR Cleanup failed: {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
            RefreshTaskStatus();
        }
    }

    private CleanupRunResult RunCleanupCore(
        bool dryRun,
        IReadOnlyList<int> steps,
        int keepSessions,
        bool manageTask,
        string label)
    {
        if (!dryRun && manageTask && ScheduledTaskService.Exists() && AdminService.IsAdmin())
        {
            AppendLog(T("Vypínám Scheduled Task 'OpenClaw Gateway'...", "Disabling scheduled task 'OpenClaw Gateway'..."));
            if (ScheduledTaskService.Disable())
            {
                AppendLog(T("  OK Task vypnut", "  OK Task disabled"));
                _taskWasDisabled = true;
            }
            else
            {
                AppendLog(T("  WARNING Vypnutí selhalo (pokračuji)", "  WARNING Disable failed (continuing)"));
            }

            AppendLog("");
        }

        var totalFiles = 0;
        var totalBytes = 0L;

        foreach (var stepNum in steps)
        {
            var stepResult = _cleanupService.RunStep(stepNum, dryRun, AppendLog, keepSessions);
            if (stepResult.ErrorMessage != null)
                AppendLog(T($"  ERROR Chyba: {stepResult.ErrorMessage}", $"  ERROR Error: {stepResult.ErrorMessage}"));

            totalFiles += stepResult.FilesProcessed;
            totalBytes += stepResult.BytesProcessed;
            AppendLog("");
        }

        if (_taskWasDisabled)
        {
            AppendLog(T("Zapínám Scheduled Task zpět...", "Re-enabling scheduled task..."));
            if (ScheduledTaskService.Enable())
                AppendLog(T("  OK Task zapnut", "  OK Task enabled"));
            else
                AppendLog(T("  WARNING Zapnutí selhalo - zapni ručně přes Plánovač úloh", "  WARNING Enable failed - re-enable it manually in Task Scheduler"));

            _taskWasDisabled = false;
            AppendLog("");
        }

        var action = dryRun ? T("by smazalo", "would delete") : T("smazáno", "deleted");
        AppendLog($"==== {label} {T("HOTOVO", "DONE")}: {action} {totalFiles} {T("položek", "items")} ({_cleanupService.FormatBytes(totalBytes)}) ====");

        return new CleanupRunResult(totalFiles, totalBytes);
    }

    private List<int> GetSelectedSteps()
    {
        var result = new List<int>();
        if (IsStep1Selected) result.Add(1);
        if (IsStep2Selected) result.Add(2);
        if (IsStep3Selected) result.Add(3);
        if (IsStep4Selected) result.Add(4);
        if (IsStep5Selected) result.Add(5);
        if (IsStep6Selected) result.Add(6);
        if (IsStep7Selected) result.Add(7);
        return result;
    }

    private void AppendLog(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        LogMessageAppended?.Invoke(this, $"[{ts}] {message}");
    }

    private void ClearLog() => LogMessageAppended?.Invoke(this, "__CLEAR__");

    private sealed record CleanupRunResult(int TotalFiles, long TotalBytes);
}
