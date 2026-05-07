using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

    private enum GatewayUiState { Stopped, Starting, Running, Failed }
    private GatewayUiState _gatewayState = GatewayUiState.Stopped;

    /// <summary>
    /// Když jsme právě spustili Gateway (přes Start nebo TUI sekvenci),
    /// čekáme na "gateway ready" v logu než přepneme na Running.
    /// Pokud false, přepneme na Running hned jak detekujeme proces
    /// (např. aplikace se spustila a Gateway už běžel).
    /// </summary>
    private bool _waitingForGatewayReady = false;

    public MainWindow()
    {
        InitializeComponent();

        BtnStartTui.Click += BtnStartTui_Click;
        BtnGatewayStart.Click += BtnGatewayStart_Click;
        BtnGatewayStop.Click += BtnGatewayStop_Click;
        BtnGatewayRestart.Click += BtnGatewayRestart_Click;
        BtnCleaningTool.Click += BtnCleaningTool_Click;
        BtnDoctorFix.Click += BtnDoctorFix_Click;

        BtnOpenPowerShell.Click += (_, _) => OpenPowerShell();
        BtnOpenGatewayLog.Click += (_, _) => OpenGatewayLog(20);

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

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statusTimer.Tick += StatusTimer_Tick;
        _statusTimer.Start();

        UpdateStatus();
        Log("Aplikace spuštěna.");
    }

    // ==================== STATUS UPDATE ====================

    private void StatusTimer_Tick(object? sender, EventArgs e) => UpdateStatus();

    private void UpdateStatus()
    {
        var snap = ResourceMonitor.Measure();
        StatusRam.Text = $"RAM: {snap.RamUsedGb:F1}/{snap.RamTotalGb:F1} GB";
        StatusCpu.Text = $"CPU: {snap.CpuPercent}%";

        if (snap.VramUsedGb.HasValue && snap.VramTotalGb.HasValue)
        {
            StatusVram.Text = $"VRAM: {snap.VramUsedGb.Value:F1}/{snap.VramTotalGb.Value:F1} GB";
            StatusVram.Visibility = Visibility.Visible;
        }
        else
        {
            StatusVram.Visibility = Visibility.Collapsed;
        }

        UpdateGatewayStatus();
        UpdateLatencyStats();
    }

    private void UpdateGatewayStatus()
    {
        var gateway = ProcessDetector.FindGatewayProcess();

        if (gateway != null)
        {
            // Proces detekován
            if (_lastKnownGatewayPid != gateway.Id)
            {
                _lastKnownGatewayPid = gateway.Id;
                try { _gatewayStartTime = gateway.StartTime; }
                catch { _gatewayStartTime = DateTime.Now; }
            }

            // Pokud čekáme na "gateway ready" (my jsme ho spustili),
            // nepřepínáme na Running ještě — sekvence to udělá sama po WaitForGatewayReady.
            // Jinak (Gateway detekován při startu aplikace nebo z externího zdroje) → Running hned.
            if (!_waitingForGatewayReady)
            {
                SetGatewayUiState(GatewayUiState.Running, gateway);
            }
            // else: zůstáváme v Starting (oranžová) — BtnStartTui nebo BtnGatewayStart to vyřeší
        }
        else
        {
            _lastKnownGatewayPid = null;
            _gatewayStartTime = null;

            if (_gatewayState != GatewayUiState.Starting &&
                _gatewayState != GatewayUiState.Failed)
            {
                SetGatewayUiState(GatewayUiState.Stopped, null);
            }
            else
            {
                SetGatewayUiButtons(_gatewayState);
            }
        }
    }

    private void UpdateLatencyStats()
    {
        var stats = LatencyTracker.GetStats();

        if (stats.Count == 0)
        {
            LatencyLast.Text = "—";
            LatencyAvg.Text = "—";
            LatencyMax.Text = "—";
            LatencyCount.Text = "—";
            LatencyLast.Foreground = Brushes.Black;
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
                                   : Brushes.Black;
        }
    }

    private void SetGatewayUiState(GatewayUiState newState, Process? gateway)
    {
        _gatewayState = newState;

        switch (newState)
        {
            case GatewayUiState.Running:
                StatusGatewayDot.Fill = Brushes.LimeGreen;
                StatusGatewayText.Text = "běží";

                if (gateway != null)
                {
                    StatusGatewayPid.Text = $"PID: {gateway.Id}";
                    StatusGatewayPidItem.Visibility = Visibility.Visible;
                    StatusGatewayDetailsSep.Visibility = Visibility.Visible;
                }

                if (_gatewayStartTime.HasValue)
                {
                    var uptime = DateTime.Now - _gatewayStartTime.Value;
                    StatusGatewayUptime.Text =
                        $"uptime: {(int)uptime.TotalHours}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
                    StatusGatewayUptimeItem.Visibility = Visibility.Visible;
                    StatusGatewayUptimeSep.Visibility = Visibility.Visible;
                }
                break;

            case GatewayUiState.Starting:
                StatusGatewayDot.Fill = Brushes.Orange;
                StatusGatewayText.Text = "spouští se...";
                HideGatewayDetails();
                break;

            case GatewayUiState.Failed:
                StatusGatewayDot.Fill = Brushes.Red;
                StatusGatewayText.Text = "selhalo";
                HideGatewayDetails();
                break;

            default:
                StatusGatewayDot.Fill = Brushes.Gray;
                StatusGatewayText.Text = "neběží";
                HideGatewayDetails();
                break;
        }

        SetGatewayUiButtons(newState);
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

    // ==================== TLAČÍTKA — Gateway ====================

    private void BtnGatewayStart_Click(object? sender, RoutedEventArgs e)
    {
        Log("Spouštím Gateway...");
        _waitingForGatewayReady = true;
        SetGatewayUiState(GatewayUiState.Starting, null);

        var proc = GatewayService.Start();
        if (proc == null)
        {
            _waitingForGatewayReady = false;
            Log("[CHYBA] Gateway nebylo možné spustit.");
            SetGatewayUiState(GatewayUiState.Failed, null);
            MessageBox.Show("Gateway nebylo možné spustit.", "Chyba",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Čekáme na "gateway ready" na pozadí
        _ = WatchForGatewayReady();
    }

    private void BtnGatewayStop_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new StopGatewayDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        _waitingForGatewayReady = false;

        if (dialog.CloseTui)
        {
            Log("Zastavuji Gateway a zavírám TUI okna...");
            var ok = GatewayService.StopAndCloseTui();
            Log(ok ? "Gateway zastaven, TUI okna zavřena." : "[CHYBA] Stop selhal.");
        }
        else
        {
            Log("Zastavuji Gateway...");
            var ok = GatewayService.Stop();
            Log(ok ? "Gateway zastaven." : "[CHYBA] Stop selhal.");
        }
    }

    private async void BtnGatewayRestart_Click(object? sender, RoutedEventArgs e)
    {
        BtnGatewayRestart.IsEnabled = false;
        _waitingForGatewayReady = true;
        Log("Restart Gateway...");

        try
        {
            Log("  → Stop...");
            GatewayService.Stop();
            await Task.Delay(2500);

            Log("  → Start...");
            SetGatewayUiState(GatewayUiState.Starting, null);
            GatewayService.Start();

            // Čekáme na "gateway ready" na pozadí
            _ = WatchForGatewayReady();
        }
        catch (Exception ex)
        {
            _waitingForGatewayReady = false;
            Log($"[CHYBA] Restart selhal: {ex.Message}");
        }
    }

    /// <summary>
    /// Sleduje Gateway log a přepne status na Running jakmile detekuje "gateway ready".
    /// Voláno po každém Start nebo Restart.
    /// Timeout 180s — pokud nenajde ready, přepne na Failed.
    /// </summary>
    private async Task WatchForGatewayReady()
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();

        try
        {
            var ready = await LogMonitor.WaitForGatewayReady(logPath, timeoutSeconds: 180);

            _waitingForGatewayReady = false;

            if (ready)
            {
                Log("Gateway ready.");
                var gw = ProcessDetector.FindGatewayProcess();
                SetGatewayUiState(GatewayUiState.Running, gw);
            }
            else
            {
                Log("[CHYBA] Gateway ready timeout.");
                SetGatewayUiState(GatewayUiState.Failed, null);
            }
        }
        catch
        {
            _waitingForGatewayReady = false;
        }
    }

    // ==================== TLAČÍTKO SPUSTIT TUI — sekvence ====================

    private async void BtnStartTui_Click(object? sender, RoutedEventArgs e)
    {
        BtnStartTui.IsEnabled = false;
        _waitingForGatewayReady = true;

        try
        {
            var logPath = SettingsService.Current.GetTodayGatewayLogPath();

            Log("Mažu starý Gateway log...");
            LogMonitor.DeleteLogIfExists(logPath);

            if (ProcessDetector.IsGatewayRunning())
            {
                Log("Zastavuji Gateway...");
                GatewayService.Stop();
                await Task.Delay(2000);
            }

            Log("Spouštím Gateway...");
            SetGatewayUiState(GatewayUiState.Starting, null);
            var proc = GatewayService.Start();
            if (proc == null)
            {
                _waitingForGatewayReady = false;
                Log("[CHYBA] Gateway nebylo možné spustit.");
                SetGatewayUiState(GatewayUiState.Failed, null);
                MessageBox.Show("Gateway nebylo možné spustit.", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log("Čekám na Gateway ready (max 180s)...");
            var startTime = DateTime.Now;
            var ready = await LogMonitor.WaitForGatewayReady(logPath, timeoutSeconds: 180);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;

            _waitingForGatewayReady = false;

            if (!ready)
            {
                Log($"[CHYBA] Gateway ready timeout po {elapsed:F1}s.");
                SetGatewayUiState(GatewayUiState.Failed, null);
                MessageBox.Show("Gateway nestihl naběhnout do 180 sekund.",
                    "Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Log($"Gateway ready za {elapsed:F1}s.");
            var gw = ProcessDetector.FindGatewayProcess();
            SetGatewayUiState(GatewayUiState.Running, gw);

            Log("Spouštím OpenClaw TUI...");
            var tuiProc = GatewayService.StartTui();
            if (tuiProc == null)
            {
                Log("[CHYBA] TUI nebylo možné spustit.");
                MessageBox.Show("TUI nebylo možné spustit.", "Chyba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Log("TUI spuštěn.");
        }
        catch (Exception ex)
        {
            _waitingForGatewayReady = false;
            Log($"[CHYBA] SPUSTIT TUI selhal: {ex.Message}");
        }
        finally
        {
            BtnStartTui.IsEnabled = true;
        }
    }

    // ==================== CLEANING / DOCTOR ====================

    private void BtnCleaningTool_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new CleaningWindow { Owner = this };
        Log("Otevírám Cleaning Tool.");
        dialog.ShowDialog();
        Log("Cleaning Tool zavřen.");
    }

    private void BtnDoctorFix_Click(object? sender, RoutedEventArgs e)
    {
        Log("Spouštím openclaw doctor --fix...");
        var proc = GatewayService.RunDoctorFix();
        if (proc == null)
            Log("[CHYBA] doctor --fix nebylo možné spustit.");
    }

    // ==================== OTEVÍRACÍ AKCE ====================

    private void OpenInExplorer(string path)
    {
        if (!Directory.Exists(path))
        {
            Log($"[CHYBA] Složka neexistuje: {path}");
            MessageBox.Show($"Složka neexistuje:\n{path}\n\nMůžeš upravit cestu v Nastavení.",
                "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
            Log($"Otevřena složka: {path}");
        }
        catch (Exception ex)
        {
            Log($"[CHYBA] Průzkumník selhalo: {ex.Message}");
        }
    }

    private void OpenPowerShell()
    {
        var workingDir = SettingsService.Current.PowerShellWorkingDir;

        if (!Directory.Exists(workingDir))
        {
            workingDir = "";
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoExit -NoProfile",
                UseShellExecute = true,
                WorkingDirectory = workingDir
            });
            Log($"Otevřen PowerShell ({(string.IsNullOrEmpty(workingDir) ? "default dir" : workingDir)})");
        }
        catch (Exception ex)
        {
            Log($"[CHYBA] PowerShell selhalo: {ex.Message}");
        }
    }

    private void OpenGatewayLog(int defaultLineCount)
    {
        var logPath = SettingsService.Current.GetTodayGatewayLogPath();
        var dialog = new GatewayLogWindow(logPath, defaultLineCount) { Owner = this };
        Log($"Otevírám Gateway log viewer ({(defaultLineCount == 0 ? "celý log" : defaultLineCount + " řádků")}).");
        dialog.ShowDialog();
    }

    private void OpenLogMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        int lineCount = 20;
        if (sender is MenuItem item && item.Tag is string tagStr &&
            int.TryParse(tagStr, out var parsed))
        {
            lineCount = parsed;
        }
        OpenGatewayLog(lineCount);
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow { Owner = this };
        if (dialog.ShowDialog() == true)
            Log("Nastavení uloženo.");
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            "OpenClaw Manager Tool by Bloom\nVerze: v0.2\n\n" +
            "Diagnostický a údržbový nástroj pro OpenClaw setup.\n\n" +
            "Postaveno v C# / WPF / .NET 8.",
            "O aplikaci", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log($"[CHYBA] URL selhalo: {ex.Message}");
        }
    }

    // ==================== LOG ====================

    private void Log(string message)
    {
        var ts = DateTime.Now.ToString("HH:mm:ss");
        AppLog.Items.Add($"[{ts}] {message}");

        while (AppLog.Items.Count > 100)
            AppLog.Items.RemoveAt(0);

        if (AppLog.Items.Count > 0)
            AppLog.ScrollIntoView(AppLog.Items[AppLog.Items.Count - 1]);
    }
}
