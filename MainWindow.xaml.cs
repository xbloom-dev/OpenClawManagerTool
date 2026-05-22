using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using OpenClawManager.Models;
using OpenClawManager.Services;
using OpenClawManager.Views;

namespace OpenClawManager;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _statusTimer;
    private DateTime? _gatewayStartTime;
    private int? _lastKnownGatewayPid;
    private bool _isStatusUpdateRunning;

    private enum GatewayUiState { Stopped, Starting, Running, Failed }
    private GatewayUiState _gatewayState = GatewayUiState.Stopped;
    private bool _waitingForGatewayReady = false;

    private static Brush ActionPositiveBrush =>
        ThemeService.GetBrush("Brush.ActionPositive", Color.FromRgb(0xD0, 0xFF, 0xD0));
    private static Brush ActionDangerBrush =>
        ThemeService.GetBrush("Brush.ActionDanger", Color.FromRgb(0xFF, 0xD0, 0xD0));

    public MainWindow()
    {
        InitializeComponent();

        BtnStartTui.Click += BtnStartTui_Click;
        BtnGatewayStart.Click += BtnGatewayStart_Click;
        BtnGatewayStop.Click += BtnGatewayStop_Click;
        BtnGatewayRestart.Click += BtnGatewayRestart_Click;
        BtnCleaningTool.Click += BtnCleaningTool_Click;
        BtnTokenManager.Click += BtnTokenManager_Click;
        BtnDoctorFix.Click += BtnDoctorFix_Click;
        BtnOpenPowerShell.Click += (_, _) => OpenPowerShell();
        BtnOpenGatewayLog.Click += (_, _) => OpenGatewayLog(20);

        Terminal.TuiStateChanged += OnTuiStateChanged;

        MnuOpenOpenClawFolder.Click += (_, _) => OpenInExplorer(SettingsService.Current.OpenClawPath);
        MnuOpenTempFolder.Click += (_, _) => OpenInExplorer(SettingsService.Current.TempPath);
        MnuOpenPowerShell.Click += (_, _) => OpenPowerShell();
        MnuExit.Click += (_, _) => Close();
        MnuOpenLog10.Click += OpenLogMenuItem_Click;
        MnuOpenLog20.Click += OpenLogMenuItem_Click;
        MnuOpenLog30.Click += OpenLogMenuItem_Click;
        MnuOpenLog50.Click += OpenLogMenuItem_Click;
        MnuOpenLog100.Click += OpenLogMenuItem_Click;
        MnuOpenLogAll.Click += OpenLogMenuItem_Click;
        MnuSettings.Click += (_, _) => OpenSettings();
        MnuAbout.Click += (_, _) => ShowAbout();
        MnuOpenClawWeb.Click += (_, _) => OpenUrl("https://docs.openclaw.ai/");

        TitleBarDragSurface.MouseLeftButtonDown += TitleBarDragSurface_MouseLeftButtonDown;
        BtnWindowMinimize.Click += (_, _) => WindowState = WindowState.Minimized;
        BtnWindowMaximize.Click += (_, _) => ToggleWindowMaximized();
        BtnWindowClose.Click += (_, _) => Close();
        StateChanged += (_, _) => UpdateMaximizeGlyph();

        PreviewKeyDown += MainWindow_PreviewKeyDown;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();

        ApplyLocalization();
        _ = UpdateStatusAsync();
        Log(L10n.Get("Str_Log_AppStarted"));

        // v0.5: tema + splash screen (pořadí důležité: theme před splash)
        InitTheme();
        InitSplash();
    }

    private void TitleBarDragSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (CaptionButtons.Visibility != Visibility.Visible) return;

        if (e.ClickCount == 2)
        {
            ToggleWindowMaximized();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void ToggleWindowMaximized()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateMaximizeGlyph()
    {
        BtnWindowMaximize.Content = WindowState == WindowState.Maximized ? "❐" : "□";
    }

    // ==================== LOKALIZACE ====================

    public void ApplyLocalization()
    {
        // Sekce headers
        GrpActions.Header = L10n.Get("Str_Group_Actions");
        GrpTools.Header = L10n.Get("Str_Section_Tools").TrimEnd(':');
        GrpLatency.Header = L10n.Get("Str_Group_Latency");
        GrpAppLog.Header = L10n.Get("Str_Group_AppLog");

        // Latency labels
        TxtLatencyLast.Text = L10n.Get("Str_Latency_Last");
        TxtLatencyAvg.Text = L10n.Get("Str_Latency_Avg");
        TxtLatencyMax.Text = L10n.Get("Str_Latency_Max");
        TxtLatencyCount.Text = L10n.Get("Str_Latency_Count");

        // Sekce labels
        TxtGatewayLabel.Text = L10n.Get("Str_Gateway_Label");
        TxtSectionOpen.Text = L10n.Get("Str_Section_Open");
        TxtSectionTools.Text = L10n.Get("Str_Section_Tools");
        TxtSectionMaintenance.Text = L10n.Get("Str_Section_Maintenance");

        // Tlačítka — popisky
        BtnGatewayStartLabel.Text = L10n.Get("Str_BtnGatewayStart");
        BtnGatewayStopLabel.Text = L10n.Get("Str_BtnGatewayStop");
        BtnGatewayRestartLabel.Text = L10n.Get("Str_BtnGatewayRestart");
        BtnPowerShellLabel.Text = L10n.Get("Str_BtnPowerShell");
        BtnGatewayLogLabel.Text = L10n.Get("Str_BtnGatewayLog");
        BtnCleaningToolLabel.Text = L10n.Get("Str_BtnCleaningTool");
        BtnTokenManagerLabel.Text = L10n.Get("Str_BtnTokenManager");
        BtnDoctorFixLabel.Text = L10n.Get("Str_BtnDoctorFix");

        // Tlačítka — tooltipy
        BtnGatewayStart.ToolTip = L10n.Get("Str_Tip_GatewayStart");
        BtnGatewayStop.ToolTip = L10n.Get("Str_Tip_GatewayStop");
        BtnGatewayRestart.ToolTip = L10n.Get("Str_Tip_GatewayRestart");
        BtnOpenPowerShell.ToolTip = L10n.Get("Str_Tip_PowerShell");
        BtnOpenGatewayLog.ToolTip = L10n.Get("Str_Tip_GatewayLog");
        BtnCleaningTool.ToolTip = L10n.Get("Str_Tip_CleaningTool");
        BtnTokenManager.ToolTip = L10n.Get("Str_Tip_TokenManager");
        BtnDoctorFix.ToolTip = L10n.Get("Str_Tip_DoctorFix");

        // Menu — headers
        MnuMenuOpen.Header = L10n.Get("Str_Menu_Open");
        MnuOpenOpenClawFolder.Header = L10n.Get("Str_Menu_OpenOpenClawFolder");
        MnuOpenTempFolder.Header = L10n.Get("Str_Menu_OpenTempFolder");
        MnuOpenGatewayLog.Header = L10n.Get("Str_Menu_OpenGatewayLog");
        MnuOpenLog10.Header = L10n.Get("Str_Menu_OpenLog10");
        MnuOpenLog20.Header = L10n.Get("Str_Menu_OpenLog20");
        MnuOpenLog30.Header = L10n.Get("Str_Menu_OpenLog30");
        MnuOpenLog50.Header = L10n.Get("Str_Menu_OpenLog50");
        MnuOpenLog100.Header = L10n.Get("Str_Menu_OpenLog100");
        MnuOpenLogAll.Header = L10n.Get("Str_Menu_OpenLogAll");
        MnuOpenPowerShell.Header = L10n.Get("Str_Menu_OpenPowerShell");
        MnuExit.Header = L10n.Get("Str_Menu_Exit");
        MnuMenuSettings.Header = L10n.Get("Str_Menu_Settings");
        MnuSettings.Header = L10n.Get("Str_Menu_OpenSettings");
        MnuMenuHelp.Header = L10n.Get("Str_Menu_Help");
        MnuAbout.Header = L10n.Get("Str_Menu_About");
        MnuOpenClawWeb.Header = L10n.Get("Str_Menu_OpenClawWeb");

        // Latency section tooltip
        bool cs = L10n.Current == L10n.Language.CS;
        GrpLatency.ToolTip = cs
            ? "Měřeno z Gateway logu (res záznamy). Resetuje se při restartu Gateway."
            : "Measured from Gateway log (res entries). Resets on Gateway restart.";

        // TUI tlačítko — popisek + tooltip (volá UpdateStartTuiButton)
        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    // ==================== KLÁVESOVÉ ZKRATKY ====================

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (FocusManager.GetFocusedElement(this) is TextBox) return;

        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (e.Key == Key.F1 && !ctrl && !shift)
        {
            ShowAbout(); e.Handled = true; return;
        }
        if (!ctrl) return;

        switch (e.Key)
        {
            case Key.T:
                BtnStartTui_Click(this, new RoutedEventArgs()); e.Handled = true; break;
            case Key.G:
                if (_gatewayState == GatewayUiState.Running)
                    BtnGatewayStop_Click(this, new RoutedEventArgs());
                else if (_gatewayState == GatewayUiState.Stopped || _gatewayState == GatewayUiState.Failed)
                    BtnGatewayStart_Click(this, new RoutedEventArgs());
                e.Handled = true; break;
            case Key.R:
                if (_gatewayState == GatewayUiState.Running)
                    BtnGatewayRestart_Click(this, new RoutedEventArgs());
                e.Handled = true; break;
            case Key.L:
                OpenLiveGatewayLog(20); e.Handled = true; break;
            case Key.C:
                if (shift) { BtnCleaningTool_Click(this, new RoutedEventArgs()); e.Handled = true; }
                break;
            case Key.OemComma:
                OpenSettings(); e.Handled = true; break;
        }
    }

    // ==================== STATUS UPDATE ====================

    private async void StatusTimer_Tick(object? sender, EventArgs e) => await UpdateStatusAsync();

    private async Task UpdateStatusAsync()
    {
        if (_isStatusUpdateRunning) return;
        _isStatusUpdateRunning = true;
        try
        {
            var snap = await ResourceMonitor.MeasureAsync();
            StatusRam.Text = $"RAM: {snap.RamUsedGb:F1}/{snap.RamTotalGb:F1} GB";
            StatusCpu.Text = $"CPU: {snap.CpuPercent}%";

            if (snap.VramUsedGb.HasValue)
            {
                StatusVram.Text = $"VRAM: {snap.VramUsedGb.Value:F1}/{snap.VramTotalGb!.Value:F1} GB";
                StatusVram.Visibility = Visibility.Visible;
            }
            else
            {
                StatusVram.Visibility = Visibility.Collapsed;
            }

            UpdateGatewayStatus();
            UpdateLatencyStats();
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

    private void UpdateGatewayStatus()
    {
        var gateway = ProcessDetector.FindGatewayProcess();

        if (gateway != null)
        {
            if (_lastKnownGatewayPid != gateway.Id)
            {
                _lastKnownGatewayPid = gateway.Id;
                try { _gatewayStartTime = gateway.StartTime; }
                catch { _gatewayStartTime = DateTime.Now; }
            }

            if (!_waitingForGatewayReady)
                SetGatewayUiState(GatewayUiState.Running, gateway);
        }
        else
        {
            _lastKnownGatewayPid = null;
            _gatewayStartTime = null;

            if (_gatewayState != GatewayUiState.Starting && _gatewayState != GatewayUiState.Failed)
                SetGatewayUiState(GatewayUiState.Stopped, null);
            else
                SetGatewayUiButtons(_gatewayState);
        }
    }

    private void UpdateLatencyStats()
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();
        LatencyTracker.Poll(logPath);

        var stats = LatencyTracker.GetStats();

        if (stats.Count == 0)
        {
            LatencyLast.Text = "—";
            LatencyAvg.Text = "—";
            LatencyMax.Text = "—";
            LatencyCount.Text = "—";
            LatencyLast.Foreground = SystemColors.ControlTextBrush;
            return;
        }

        LatencyLast.Text = stats.LastMs.HasValue ? $"{stats.LastMs} ms" : "—";
        LatencyAvg.Text = stats.AvgMs.HasValue ? $"{stats.AvgMs:F0} ms" : "—";
        LatencyMax.Text = stats.MaxMs.HasValue ? $"{stats.MaxMs} ms" : "—";
        LatencyCount.Text = stats.Count.ToString();

        if (stats.LastMs.HasValue)
        {
            LatencyLast.Foreground = stats.LastMs > 5000 ? Brushes.Red
                                   : stats.LastMs < 1000 ? Brushes.Green
                                   : SystemColors.ControlTextBrush;
        }
    }

    private void SetGatewayUiState(GatewayUiState newState, Process? gateway)
    {
        _gatewayState = newState;

        switch (newState)
        {
            case GatewayUiState.Running:
                StatusGatewayDot.Fill = Brushes.LimeGreen;
                StatusGatewayText.Text = L10n.Get("Str_Status_Running");
                if (gateway != null)
                {
                    StatusGatewayPid.Text = $"PID: {gateway.Id}";
                    StatusGatewayPidItem.Visibility = Visibility.Visible;
                    StatusGatewayDetailsSep.Visibility = Visibility.Visible;
                }
                if (_gatewayStartTime.HasValue)
                {
                    var up = DateTime.Now - _gatewayStartTime.Value;
                    StatusGatewayUptime.Text = $"uptime: {(int)up.TotalHours}:{up.Minutes:D2}:{up.Seconds:D2}";
                    StatusGatewayUptimeItem.Visibility = Visibility.Visible;
                    StatusGatewayUptimeSep.Visibility = Visibility.Visible;
                }
                break;

            case GatewayUiState.Starting:
                StatusGatewayDot.Fill = Brushes.Orange;
                StatusGatewayText.Text = L10n.Get("Str_Status_Starting");
                HideGatewayDetails();
                break;

            case GatewayUiState.Failed:
                StatusGatewayDot.Fill = Brushes.Red;
                StatusGatewayText.Text = L10n.Get("Str_Status_Failed");
                HideGatewayDetails();
                break;

            default:
                StatusGatewayDot.Fill = Brushes.Gray;
                StatusGatewayText.Text = L10n.Get("Str_Status_Stopped");
                HideGatewayDetails();
                break;
        }

        SetGatewayUiButtons(newState);
        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void HideGatewayDetails()
    {
        StatusGatewayPidItem.Visibility = Visibility.Collapsed;
        StatusGatewayDetailsSep.Visibility = Visibility.Collapsed;
        StatusGatewayUptimeItem.Visibility = Visibility.Collapsed;
        StatusGatewayUptimeSep.Visibility = Visibility.Collapsed;
    }

    private void SetGatewayUiButtons(GatewayUiState state)
    {
        switch (state)
        {
            case GatewayUiState.Running:
                BtnGatewayStart.IsEnabled = false;
                BtnGatewayStop.IsEnabled = true;
                BtnGatewayRestart.IsEnabled = true;
                break;
            case GatewayUiState.Starting:
                BtnGatewayStart.IsEnabled = false;
                BtnGatewayStop.IsEnabled = false;
                BtnGatewayRestart.IsEnabled = false;
                break;
            default:
                BtnGatewayStart.IsEnabled = true;
                BtnGatewayStop.IsEnabled = false;
                BtnGatewayRestart.IsEnabled = false;
                break;
        }
    }

    // ==================== Gateway tlačítka ====================

    private void BtnGatewayStart_Click(object? sender, RoutedEventArgs e)
    {
        Log(L10n.Get("Str_Log_GatewayStarting"));
        _waitingForGatewayReady = true;
        SetGatewayUiState(GatewayUiState.Starting, null);
        GatewayService.Start();
        _ = WatchForGatewayReady();
    }

    private void BtnGatewayStop_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new StopGatewayDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        _waitingForGatewayReady = false;
        Terminal.StopTui();
        LatencyTracker.Reset();

        if (dialog.CloseTui)
        {
            Log(L10n.Get("Str_Log_StoppingGatewayTui"));
            GatewayService.StopAndCloseTui();
        }
        else
        {
            Log(L10n.Get("Str_Log_GatewayStopping"));
            GatewayService.Stop();
        }
        Log(L10n.Get("Str_Log_GatewayStopped"));
    }

    private async void BtnGatewayRestart_Click(object? sender, RoutedEventArgs e)
    {
        BtnGatewayRestart.IsEnabled = false;
        _waitingForGatewayReady = true;
        Terminal.StopTui();
        LatencyTracker.Reset();
        Log("Restart Gateway...");

        try
        {
            SetGatewayUiState(GatewayUiState.Starting, null);
            await GatewayService.RestartAsync();
            _ = WatchForGatewayReady();
        }
        catch (Exception ex)
        {
            _waitingForGatewayReady = false;
            Log($"[CHYBA] Restart selhal: {ex.Message}");
        }
    }

    private async Task WatchForGatewayReady()
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();
        try
        {
            var ready = await LogMonitor.WaitForGatewayReady(logPath, 180);
            _waitingForGatewayReady = false;

            if (ready)
            {
                Log(L10n.Get("Str_Log_GatewayReady"));
                var gw = ProcessDetector.FindGatewayProcess();
                SetGatewayUiState(GatewayUiState.Running, gw);
                Log(L10n.Get("Str_Log_TuiStarting"));
                Terminal.StartTui();
            }
            else
            {
                Log(L10n.Get("Str_Log_GatewayTimeout") + " 180s.");
                SetGatewayUiState(GatewayUiState.Failed, null);
            }
        }
        catch { _waitingForGatewayReady = false; }
    }

    // ==================== TUI tlačítko ====================

    private async void BtnStartTui_Click(object? sender, RoutedEventArgs e)
    {
        // v0.5: uvolnit splash overlay před spuštěním TUI
        DisposeSplash();

        BtnStartTui.IsEnabled = false;
        try
        {
            if (Terminal.IsTuiRunning)
            {
                Log(L10n.Get("Str_Log_TuiStopping"));
                Terminal.StopTui();
                return;
            }

            if (_gatewayState == GatewayUiState.Running)
            {
                Log(L10n.Get("Str_Log_TuiStarting"));
                Terminal.StartTui();
                return;
            }

            _waitingForGatewayReady = true;
            var logPath = SettingsService.Current.GetTodayGatewayLogPath();

            Log(L10n.Get("Str_Log_DeletingLog"));
            LogMonitor.DeleteLogIfExists(logPath);
            LatencyTracker.Reset();

            if (ProcessDetector.IsGatewayRunning())
            {
                Log(L10n.Get("Str_Log_GatewayStopping"));
                GatewayService.Stop();
                await Task.Delay(2000);
            }

            Log(L10n.Get("Str_Log_GatewayStarting"));
            SetGatewayUiState(GatewayUiState.Starting, null);
            var proc = GatewayService.Start();
            if (proc == null)
            {
                _waitingForGatewayReady = false;
                Log("[CHYBA] Gateway nebylo možné spustit.");
                SetGatewayUiState(GatewayUiState.Failed, null);
                return;
            }

            Log(L10n.Get("Str_Log_WaitingGatewayReady"));
            var startTime = DateTime.Now;
            var ready = await LogMonitor.WaitForGatewayReady(logPath, 180);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;
            _waitingForGatewayReady = false;

            if (!ready)
            {
                Log($"{L10n.Get("Str_Log_GatewayTimeout")} {elapsed:F1}s.");
                SetGatewayUiState(GatewayUiState.Failed, null);
                return;
            }

            Log($"{L10n.Get("Str_Log_GatewayReadyIn")} {elapsed:F1}s.");
            var gw = ProcessDetector.FindGatewayProcess();
            SetGatewayUiState(GatewayUiState.Running, gw);

            Log(L10n.Get("Str_Log_TuiStarting"));
            Terminal.StartTui();
        }
        catch (Exception ex)
        {
            _waitingForGatewayReady = false;
            Log($"[CHYBA] {ex.Message}");
        }
        finally
        {
            BtnStartTui.IsEnabled = true;
        }
    }

    private void OnTuiStateChanged(bool isRunning)
    {
        Dispatcher.BeginInvoke(new Action(() => UpdateStartTuiButton(isRunning)));
    }

    private void UpdateStartTuiButton(bool tuiRunning)
    {
        BtnStartTui.ToolTip = L10n.Get("Str_Tip_StartTui");

        if (tuiRunning)
        {
            BtnStartTuiSymbol.Text = "■";
            BtnStartTuiSymbol.Foreground = Brushes.Red;
            BtnStartTuiLabel.Text = L10n.Get("Str_BtnStartTui_Stop");
            BtnStartTuiSubLabel.Text = L10n.Get("Str_BtnStartTui_Sub_Stop");
            BtnStartTui.Background = ActionDangerBrush;
        }
        else if (_gatewayState == GatewayUiState.Running)
        {
            BtnStartTuiSymbol.Text = "▶";
            BtnStartTuiSymbol.Foreground = Brushes.Green;
            BtnStartTuiLabel.Text = L10n.Get("Str_BtnStartTui_Label");
            BtnStartTuiSubLabel.Text = L10n.Get("Str_BtnStartTui_Sub_Running");
            BtnStartTui.Background = ActionPositiveBrush;
        }
        else
        {
            BtnStartTuiSymbol.Text = "▶";
            BtnStartTuiSymbol.Foreground = Brushes.Green;
            BtnStartTuiLabel.Text = L10n.Get("Str_BtnStartTui_Label");
            BtnStartTuiSubLabel.Text = L10n.Get("Str_BtnStartTui_Sub_Restart");
            BtnStartTui.Background = ActionPositiveBrush;
        }

        ApplyThemeSpecificTuiVisual(tuiRunning);
    }

    // ==================== Ostatní tlačítka ====================

    private void BtnCleaningTool_Click(object? sender, RoutedEventArgs e)
    {
        Log(L10n.Get("Str_Log_OpeningCleaningTool"));
        var dialog = new CleaningWindow { Owner = this };
        dialog.ShowDialog();
        Log(L10n.Get("Str_Log_ClosedCleaningTool"));
    }

    private void BtnTokenManager_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new TokenManagerWindow { Owner = this };
        dialog.ShowDialog();
    }

    private void BtnDoctorFix_Click(object? sender, RoutedEventArgs e)
    {
        Log(L10n.Get("Str_Log_DoctorFix"));
        GatewayService.RunDoctorFix();
    }

    private void OpenInExplorer(string path)
    {
        if (!Directory.Exists(path)) { Log($"[CHYBA] Složka neexistuje: {path}"); return; }
        try { Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true }); }
        catch (Exception ex) { Log($"[CHYBA] {ex.Message}"); }
    }

    private void OpenPowerShell()
    {
        var workDir = SettingsService.Current.PowerShellWorkingDir;
        if (!Directory.Exists(workDir)) workDir = "";
        try { Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoExit -NoProfile", UseShellExecute = true, WorkingDirectory = workDir }); }
        catch (Exception ex) { Log($"[CHYBA] {ex.Message}"); }
    }

    private void OpenGatewayLog(int lines)
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();
        var dialog = new GatewayLogWindow(logPath, lines) { Owner = this };
        dialog.ShowDialog();
    }

    private void OpenLiveGatewayLog(int lines)
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();
        var live = new LiveLogWindow(logPath, lines) { Owner = this };
        live.Show();
    }

    private void OpenLogMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        int n = 20;
        if (sender is MenuItem mi && mi.Tag is string s && int.TryParse(s, out var p)) n = p;
        OpenGatewayLog(n);
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var lang = SettingsService.Current.Language == "EN"
                ? L10n.Language.EN : L10n.Language.CS;
            L10n.Apply(lang);
            ApplyLocalization();
            ReapplyCurrentThemeLayoutAfterLocalization();
            Log(L10n.Get("Str_Log_SettingsSaved"));
        }
    }

    private void ShowAbout()
    {
        var dialog = new AboutWindow { Owner = this };
        dialog.ShowDialog();
    }

    internal void ExecuteAboutCommand(string command)
    {
        switch (command.Trim().ToLowerInvariant())
        {
            case "replay":
                ReplaySplashFromAboutCommand();
                break;
            case "exit":
                Close();
                break;
            case "legacy":
                ApplyThemeFromAboutCommand(AppTheme.Legacy);
                break;
            case "dark":
                ApplyThemeFromAboutCommand(AppTheme.StandardDark);
                break;
            case "light":
                ApplyThemeFromAboutCommand(AppTheme.ModernLight);
                break;
            case "modern":
                ApplyThemeFromAboutCommand(AppTheme.Modern);
                break;
            case "crab":
                ApplyThemeFromAboutCommand(AppTheme.CrabCute);
                break;
            case "logs":
                OpenGatewayLog(20);
                break;
            case "tokens":
                BtnTokenManager_Click(this, new RoutedEventArgs());
                break;
            case "settings":
                OpenSettings();
                break;
            case "help":
                ShowAboutCommandHelp();
                break;
        }
    }

    private void ReplaySplashFromAboutCommand()
    {
        if (Terminal.IsTuiRunning)
        {
            Log("[About] Splash replay skipped because OpenClaw TUI is running.");
            return;
        }

        Log("[About] Replaying splash screen.");
        StopSplashVideo();
        InitSplash();
    }

    private void ApplyThemeFromAboutCommand(AppTheme theme)
    {
        var settings = SettingsService.Current;
        if (settings.Theme == theme)
        {
            Log($"[About] Theme already active: {theme}.");
            return;
        }

        settings.Theme = theme;
        if (!SettingsService.Save(settings))
        {
            Log($"[CHYBA] Theme switch failed: {theme}.");
            return;
        }

        ThemeService.Apply(theme);
        ApplyLocalization();
        ReapplyCurrentThemeLayoutAfterLocalization();
        Log($"[About] Theme switched to {theme}.");
    }

    private static void ShowAboutCommandHelp()
    {
        MessageBox.Show(
            L10n.Get("Str_About_CommandHelpText"),
            L10n.Get("Str_About_CommandHelpTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch { }
    }

    private void Log(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        AppLog.Items.Add($"[{ts}] {message}");
        while (AppLog.Items.Count > 100) AppLog.Items.RemoveAt(0);
        if (AppLog.Items.Count > 0) AppLog.ScrollIntoView(AppLog.Items[AppLog.Items.Count - 1]);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (ProcessDetector.IsGatewayRunning())
        {
            bool cs = L10n.Current == L10n.Language.CS;
            var result = MessageBox.Show(
                cs ? "OpenClaw Gateway stále běží.\n\nAno = Zastavit Gateway a zavřít\nNe = Zavřít a nechat Gateway běžet\nZrušit = Zpět do aplikace"
                   : "OpenClaw Gateway is still running.\n\nYes = Stop Gateway and close\nNo = Close and leave Gateway running\nCancel = Return to application",
                cs ? "Gateway běží" : "Gateway running",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    Terminal.StopTui();
                    GatewayService.StopAndCloseTui();
                    break;
                case MessageBoxResult.No:
                    Terminal.StopTui();
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    return;
            }
        }
        else
        {
            Terminal.StopTui();
        }

        Terminal.Shutdown();

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }
}
