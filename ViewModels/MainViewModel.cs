using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenClawManager.Services;

namespace OpenClawManager.ViewModels;

public interface IMainWindowCallback
{
    void StartTui();
    void StopTui();
    bool IsTuiRunning { get; }
    void DisposeSplash();
    void RefreshLocalizationAndLayout();
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IGatewayService _gatewayService;
    private readonly IResourceMonitor _resourceMonitor;
    private readonly IProcessDetector _processDetector;
    private readonly ISettingsService _settingsService;
    private GatewayUiState _gatewayState = GatewayUiState.Stopped;
    private bool _waitingForGatewayReady;
    private DateTime? _gatewayStartTime;
    private int? _lastKnownGatewayPid;
    private bool _isStatusUpdateRunning;
    public string AppVersionText { get; } = AppVersionInfo.Display;

    public MainViewModel(
        IGatewayService gatewayService,
        IResourceMonitor resourceMonitor,
        IProcessDetector processDetector,
        ISettingsService settingsService)
    {
        _gatewayService = gatewayService;
        _resourceMonitor = resourceMonitor;
        _processDetector = processDetector;
        _settingsService = settingsService;

        RefreshLocalization();
    }

    public enum GatewayUiState { Stopped, Starting, Running, Failed }

    public event EventHandler? GatewayReadyForTui;
    public event EventHandler? GatewayStateChanged;
    public event EventHandler? LogAppended;

    public ObservableCollection<string> AppLogItems { get; } = new();

    public bool IsGatewayRunning => _gatewayState == GatewayUiState.Running;
    public bool IsGatewayStoppedOrFailed => _gatewayState is GatewayUiState.Stopped or GatewayUiState.Failed;

    [ObservableProperty] private string _ramText = "RAM: —";
    [ObservableProperty] private string _cpuText = "CPU: —";
    [ObservableProperty] private string _vramText = "VRAM: —";
    [ObservableProperty] private bool _isVramVisible = true;

    [ObservableProperty] private string _gatewayStatusText = "";
    [ObservableProperty] private Brush _gatewayStatusColor = Brushes.Gray;
    [ObservableProperty] private string _gatewayPidText = "PID: —";
    [ObservableProperty] private bool _isPidVisible;
    [ObservableProperty] private string _gatewayUptimeText = "uptime: —";
    [ObservableProperty] private bool _isUptimeVisible;

    [ObservableProperty] private string _latencyLastText = "—";
    [ObservableProperty] private Brush _latencyLastColor = SystemColors.ControlTextBrush;
    [ObservableProperty] private string _latencyAvgText = "—";
    [ObservableProperty] private string _latencyMaxText = "—";
    [ObservableProperty] private string _latencyCountText = "—";

    [ObservableProperty] private bool _btnGatewayStartEnabled = true;
    [ObservableProperty] private bool _btnGatewayStopEnabled;
    [ObservableProperty] private bool _btnGatewayRestartEnabled;

    [ObservableProperty] private string _grpActionsHeader = "";
    [ObservableProperty] private string _grpToolsHeader = "";
    [ObservableProperty] private string _grpLatencyHeader = "";
    [ObservableProperty] private string _grpAppLogHeader = "";
    [ObservableProperty] private string _gatewayLabel = "";
    [ObservableProperty] private string _sectionOpenLabel = "";
    [ObservableProperty] private string _sectionToolsLabel = "";
    [ObservableProperty] private string _sectionMaintenanceLabel = "";
    [ObservableProperty] private string _latencyLastLabel = "";
    [ObservableProperty] private string _latencyAvgLabel = "";
    [ObservableProperty] private string _latencyMaxLabel = "";
    [ObservableProperty] private string _latencyCountLabel = "";
    [ObservableProperty] private string _btnGatewayStartLabel = "";
    [ObservableProperty] private string _btnGatewayStopLabel = "";
    [ObservableProperty] private string _btnGatewayRestartLabel = "";
    [ObservableProperty] private string _btnPowerShellLabel = "";
    [ObservableProperty] private string _btnGatewayLogLabel = "";
    [ObservableProperty] private string _btnCleaningToolLabel = "";
    [ObservableProperty] private string _btnTokenManagerLabel = "";
    [ObservableProperty] private string _btnDoctorFixLabel = "";
    [ObservableProperty] private string _btnGatewayStartToolTip = "";
    [ObservableProperty] private string _btnGatewayStopToolTip = "";
    [ObservableProperty] private string _btnGatewayRestartToolTip = "";
    [ObservableProperty] private string _btnPowerShellToolTip = "";
    [ObservableProperty] private string _btnGatewayLogToolTip = "";
    [ObservableProperty] private string _btnCleaningToolToolTip = "";
    [ObservableProperty] private string _btnTokenManagerToolTip = "";
    [ObservableProperty] private string _btnDoctorFixToolTip = "";
    [ObservableProperty] private string _grpLatencyToolTip = "";

    public void RefreshLocalization()
    {
        var isCzech = L10n.Current == L10n.Language.CS;

        GrpActionsHeader = L10n.Get("Str_Group_Actions");
        GrpToolsHeader = L10n.Get("Str_Section_Tools").TrimEnd(':');
        GrpLatencyHeader = L10n.Get("Str_Group_Latency");
        GrpAppLogHeader = L10n.Get("Str_Group_AppLog");
        GatewayLabel = L10n.Get("Str_Gateway_Label");
        SectionOpenLabel = L10n.Get("Str_Section_Open");
        SectionToolsLabel = L10n.Get("Str_Section_Tools");
        SectionMaintenanceLabel = L10n.Get("Str_Section_Maintenance");
        LatencyLastLabel = L10n.Get("Str_Latency_Last");
        LatencyAvgLabel = L10n.Get("Str_Latency_Avg");
        LatencyMaxLabel = L10n.Get("Str_Latency_Max");
        LatencyCountLabel = L10n.Get("Str_Latency_Count");
        BtnGatewayStartLabel = L10n.Get("Str_BtnGatewayStart");
        BtnGatewayStopLabel = L10n.Get("Str_BtnGatewayStop");
        BtnGatewayRestartLabel = L10n.Get("Str_BtnGatewayRestart");
        BtnPowerShellLabel = L10n.Get("Str_BtnPowerShell");
        BtnGatewayLogLabel = L10n.Get("Str_BtnGatewayLog");
        BtnCleaningToolLabel = L10n.Get("Str_BtnCleaningTool");
        BtnTokenManagerLabel = L10n.Get("Str_BtnTokenManager");
        BtnDoctorFixLabel = L10n.Get("Str_BtnDoctorFix");
        BtnGatewayStartToolTip = L10n.Get("Str_Tip_GatewayStart");
        BtnGatewayStopToolTip = L10n.Get("Str_Tip_GatewayStop");
        BtnGatewayRestartToolTip = L10n.Get("Str_Tip_GatewayRestart");
        BtnPowerShellToolTip = L10n.Get("Str_Tip_PowerShell");
        BtnGatewayLogToolTip = L10n.Get("Str_Tip_GatewayLog");
        BtnCleaningToolToolTip = L10n.Get("Str_Tip_CleaningTool");
        BtnTokenManagerToolTip = L10n.Get("Str_Tip_TokenManager");
        BtnDoctorFixToolTip = L10n.Get("Str_Tip_DoctorFix");
        GrpLatencyToolTip = isCzech
            ? "Měřeno z Gateway logu (res záznamy). Resetuje se při restartu Gateway."
            : "Measured from Gateway log (res entries). Resets on Gateway restart.";

        RefreshGatewayStatusText();
    }

    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AppLogItems.Add($"[{timestamp}] {message}");
        while (AppLogItems.Count > 100)
            AppLogItems.RemoveAt(0);

        LogAppended?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateStatusAsync()
    {
        if (_isStatusUpdateRunning)
            return;

        _isStatusUpdateRunning = true;
        try
        {
            var snap = await _resourceMonitor.MeasureAsync();
            RamText = $"RAM: {snap.RamUsedGb:F1}/{snap.RamTotalGb:F1} GB";
            CpuText = $"CPU: {snap.CpuPercent}%";

            if (snap.VramUsedGb.HasValue)
            {
                VramText = $"VRAM: {snap.VramUsedGb.Value:F1}/{snap.VramTotalGb!.Value:F1} GB";
                IsVramVisible = true;
            }
            else
            {
                IsVramVisible = false;
            }

            var gateway = await _processDetector.FindGatewayProcessAsync();
            RefreshGatewayFromProcess(gateway);
            await UpdateLatencyStatsAsync();
        }
        catch (Exception ex)
        {
            Log($"[CHYBA] Status update selhal: {ex.Message}");
        }
        finally
        {
            _isStatusUpdateRunning = false;
        }
    }

    public void StartGateway()
    {
        try
        {
            Log(L10n.Get("Str_Log_GatewayStarting"));
            _waitingForGatewayReady = true;
            MarkGatewayStarting();
            _gatewayService.Start();
            _ = WatchForGatewayReady();
        }
        catch (Exception ex)
        {
            _waitingForGatewayReady = false;
            Log($"[CHYBA] {ex.Message}");
            MarkGatewayFailed();
        }
    }

    public void StopGateway(bool closeTui)
    {
        _waitingForGatewayReady = false;
        LatencyTracker.Reset();

        if (closeTui)
        {
            Log(L10n.Get("Str_Log_StoppingGatewayTui"));
            _gatewayService.StopAndCloseTui();
        }
        else
        {
            Log(L10n.Get("Str_Log_GatewayStopping"));
            _gatewayService.Stop();
        }

        Log(L10n.Get("Str_Log_GatewayStopped"));
    }

    public async Task RestartGatewayAsync()
    {
        _waitingForGatewayReady = true;
        LatencyTracker.Reset();
        Log("Restart Gateway...");

        try
        {
            MarkGatewayStarting();
            await _gatewayService.RestartAsync();
            _ = WatchForGatewayReady();
        }
        catch (Exception ex)
        {
            _waitingForGatewayReady = false;
            Log($"[CHYBA] Restart selhal: {ex.Message}");
            MarkGatewayFailed();
        }
    }

    public void RunDoctorFix()
    {
        Log(L10n.Get("Str_Log_DoctorFix"));
        _gatewayService.RunDoctorFix();
    }

    public void ResetLatencyAndWaiting()
    {
        _waitingForGatewayReady = false;
        LatencyTracker.Reset();
    }

    public void SetWaitingForGatewayReady(bool value)
    {
        _waitingForGatewayReady = value;
    }

    public void MarkGatewayStarting() => SetGatewayState(GatewayUiState.Starting, null);
    public void MarkGatewayFailed() => SetGatewayState(GatewayUiState.Failed, null);
    public void MarkGatewayRunning(Process? gateway) => SetGatewayState(GatewayUiState.Running, gateway);

    private async Task WatchForGatewayReady()
    {
        var logPath = _settingsService.Settings.GetTodayGatewayLogPath();
        try
        {
            var ready = await LogMonitor.WaitForGatewayReady(logPath, 180);
            _waitingForGatewayReady = false;

            if (ready)
            {
                Log(L10n.Get("Str_Log_GatewayReady"));
                var gateway = _processDetector.FindGatewayProcess();
                SetGatewayState(GatewayUiState.Running, gateway);
                Log(L10n.Get("Str_Log_TuiStarting"));
                GatewayReadyForTui?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Log(L10n.Get("Str_Log_GatewayTimeout") + " 180s.");
                SetGatewayState(GatewayUiState.Failed, null);
            }
        }
        catch
        {
            _waitingForGatewayReady = false;
        }
    }

    private void RefreshGatewayFromProcess(Process? gateway)
    {
        if (gateway != null)
        {
            if (_lastKnownGatewayPid != gateway.Id)
            {
                _lastKnownGatewayPid = gateway.Id;
                try { _gatewayStartTime = gateway.StartTime; }
                catch { _gatewayStartTime = DateTime.Now; }
            }

            if (!_waitingForGatewayReady)
                SetGatewayState(GatewayUiState.Running, gateway);
        }
        else
        {
            _lastKnownGatewayPid = null;
            _gatewayStartTime = null;

            if (_gatewayState != GatewayUiState.Starting && _gatewayState != GatewayUiState.Failed)
                SetGatewayState(GatewayUiState.Stopped, null);
            else
                RefreshGatewayButtons();
        }
    }

    private async Task UpdateLatencyStatsAsync()
    {
        var logPath = _settingsService.Settings.GetTodayGatewayLogPath();
        await LatencyTracker.PollAsync(logPath);
        var stats = LatencyTracker.GetStats();

        if (stats.Count == 0)
        {
            LatencyLastText = "—";
            LatencyAvgText = "—";
            LatencyMaxText = "—";
            LatencyCountText = "—";
            LatencyLastColor = SystemColors.ControlTextBrush;
            return;
        }

        LatencyLastText = stats.LastMs.HasValue ? $"{stats.LastMs} ms" : "—";
        LatencyAvgText = stats.AvgMs.HasValue ? $"{stats.AvgMs:F0} ms" : "—";
        LatencyMaxText = stats.MaxMs.HasValue ? $"{stats.MaxMs} ms" : "—";
        LatencyCountText = stats.Count.ToString();

        if (stats.LastMs.HasValue)
        {
            LatencyLastColor = stats.LastMs > 5000 ? Brushes.Red
                : stats.LastMs < 1000 ? Brushes.Green
                : SystemColors.ControlTextBrush;
        }
    }

    private void SetGatewayState(GatewayUiState newState, Process? gateway)
    {
        _gatewayState = newState;
        RefreshGatewayStatusText();
        RefreshGatewayButtons();
        OnPropertyChanged(nameof(IsGatewayRunning));
        OnPropertyChanged(nameof(IsGatewayStoppedOrFailed));

        if (newState == GatewayUiState.Running && gateway != null)
        {
            GatewayPidText = $"PID: {gateway.Id}";
            IsPidVisible = true;

            if (_gatewayStartTime.HasValue)
            {
                var uptime = DateTime.Now - _gatewayStartTime.Value;
                GatewayUptimeText = $"uptime: {(int)uptime.TotalHours}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
                IsUptimeVisible = true;
            }
        }
        else
        {
            IsPidVisible = false;
            IsUptimeVisible = false;
        }

        GatewayStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshGatewayStatusText()
    {
        (GatewayStatusText, GatewayStatusColor) = _gatewayState switch
        {
            GatewayUiState.Running => (L10n.Get("Str_Status_Running"), Brushes.LimeGreen),
            GatewayUiState.Starting => (L10n.Get("Str_Status_Starting"), Brushes.Orange),
            GatewayUiState.Failed => (L10n.Get("Str_Status_Failed"), Brushes.Red),
            _ => (L10n.Get("Str_Status_Stopped"), Brushes.Gray),
        };
    }

    private void RefreshGatewayButtons()
    {
        BtnGatewayStartEnabled = _gatewayState is GatewayUiState.Stopped or GatewayUiState.Failed;
        BtnGatewayStopEnabled = _gatewayState == GatewayUiState.Running;
        BtnGatewayRestartEnabled = _gatewayState == GatewayUiState.Running;
    }
}
