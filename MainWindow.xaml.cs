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

    /// <summary>
    /// Aktuální nastavení aplikace. Načítají se při startu, mění se přes Settings okno.
    /// </summary>
    private AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        // Načíst settings (nebo defaulty pokud soubor neexistuje)
        _settings = SettingsService.Load();

        // ===== Hlavní tlačítka =====
        BtnStartTui.Click += BtnStartTui_Click;
        BtnGatewayStart.Click += BtnGatewayStart_Click;
        BtnGatewayStop.Click += BtnGatewayStop_Click;
        BtnGatewayRestart.Click += BtnGatewayRestart_Click;
        BtnCleaningTool.Click += BtnCleaningTool_Click;
        BtnDoctorFix.Click += BtnDoctorFix_Click;

        // ===== Menu Soubor =====
        MnuOpenOpenClawFolder.Click += (_, _) => OpenInExplorer(_settings.OpenClawPath);
        MnuOpenTempFolder.Click += (_, _) => OpenInExplorer(_settings.TempPath);
        MnuOpenPowerShell.Click += (_, _) => OpenPowerShell();
        MnuExit.Click += (_, _) => Close();

        // Submenu Otevřít Gateway log — všechny varianty volají stejnou metodu, jen s jiným Tag
        MnuOpenLog10.Click += OpenLogMenuItem_Click;
        MnuOpenLog20.Click += OpenLogMenuItem_Click;
        MnuOpenLog30.Click += OpenLogMenuItem_Click;
        MnuOpenLog50.Click += OpenLogMenuItem_Click;
        MnuOpenLog100.Click += OpenLogMenuItem_Click;
        MnuOpenLogAll.Click += OpenLogMenuItem_Click;

        // ===== Menu Nastavení =====
        MnuSettings.Click += (_, _) => OpenSettings();

        // ===== Menu Nápověda =====
        MnuAbout.Click += (_, _) => ShowAbout();
        MnuOpenClawWeb.Click += (_, _) => OpenUrl("https://docs.openclaw.ai/");

        // ===== Status timer =====
        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
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

            GatewayDot.Fill = Brushes.LimeGreen;
            GatewayStatusText.Text = "Běží";
            GatewayDetails.Visibility = Visibility.Visible;
            GatewayPid.Text = gateway.Id.ToString();

            if (_gatewayStartTime.HasValue)
            {
                var uptime = DateTime.Now - _gatewayStartTime.Value;
                GatewayUptime.Text = $"{(int)uptime.TotalHours}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
            }

            StatusGatewayDot.Fill = Brushes.LimeGreen;
            StatusGatewayText.Text = "běží";

            BtnGatewayStart.IsEnabled = false;
            BtnGatewayStop.IsEnabled = true;
            BtnGatewayRestart.IsEnabled = true;
        }
        else
        {
            _lastKnownGatewayPid = null;
            _gatewayStartTime = null;

            GatewayDot.Fill = Brushes.Gray;
            GatewayStatusText.Text = "Neběží";
            GatewayDetails.Visibility = Visibility.Collapsed;

            StatusGatewayDot.Fill = Brushes.Gray;
            StatusGatewayText.Text = "neběží";

            BtnGatewayStart.IsEnabled = true;
            BtnGatewayStop.IsEnabled = false;
            BtnGatewayRestart.IsEnabled = false;
        }
    }

    // ==================== TLAČÍTKA — Gateway ====================

    private void BtnGatewayStart_Click(object? sender, RoutedEventArgs e)
    {
        Log("Spouštím Gateway...");
        var proc = GatewayService.Start();
        if (proc == null)
        {
            Log("[CHYBA] Gateway nebylo možné spustit.");
            MessageBox.Show("Gateway nebylo možné spustit.", "Chyba",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnGatewayStop_Click(object? sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Opravdu zastavit Gateway?\nVšechny aktivní TUI sessions budou přerušeny.",
            "Zastavit Gateway",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        Log("Zastavuji Gateway...");
        var ok = GatewayService.Stop();
        Log(ok ? "Gateway zastaven." : "[CHYBA] Stop selhal.");
    }

    private void BtnGatewayRestart_Click(object? sender, RoutedEventArgs e)
    {
        Log("Restart Gateway...");
        GatewayService.Restart();
    }

    // ==================== TLAČÍTKA — TUI / Cleaning ====================

    private void BtnStartTui_Click(object? sender, RoutedEventArgs e)
    {
        Log("[TODO] SPUSTIT TUI sekvence — bude implementováno v dalším kroku.");
        MessageBox.Show("Funkce SPUSTIT TUI bude implementována v dalším kroku.",
            "Coming soon", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnCleaningTool_Click(object? sender, RoutedEventArgs e)
    {
        Log("[TODO] Cleaning Tool bude implementován v další fázi.");
        MessageBox.Show("Cleaning Tool bude přidán v další fázi vývoje.",
            "Coming soon", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnDoctorFix_Click(object? sender, RoutedEventArgs e)
    {
        Log("Spouštím openclaw doctor --fix v novém okně...");
        var proc = GatewayService.RunDoctorFix();
        if (proc == null)
            Log("[CHYBA] doctor --fix nebylo možné spustit.");
    }

    // ==================== MENU — Soubor ====================

    /// <summary>
    /// Otevře složku v Průzkumníku Windows.
    /// </summary>
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
            Log($"[CHYBA] Otevření Průzkumníka selhalo: {ex.Message}");
        }
    }

    /// <summary>
    /// Otevře nový PowerShell ve složce z nastavení.
    /// </summary>
    private void OpenPowerShell()
    {
        var workingDir = _settings.PowerShellWorkingDir;

        if (!Directory.Exists(workingDir))
        {
            Log($"[VAROVÁNÍ] PowerShell pracovní adresář neexistuje: {workingDir}");
            MessageBox.Show($"PowerShell pracovní adresář neexistuje:\n{workingDir}\n\nOtevírám PowerShell bez změny adresáře.",
                "Varování", MessageBoxButton.OK, MessageBoxImage.Warning);
            workingDir = "";
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoExit -NoProfile",
                UseShellExecute = true,
                WorkingDirectory = workingDir
            };

            Process.Start(psi);
            Log($"Otevřen PowerShell ({(string.IsNullOrEmpty(workingDir) ? "default dir" : workingDir)})");
        }
        catch (Exception ex)
        {
            Log($"[CHYBA] Spuštění PowerShellu selhalo: {ex.Message}");
        }
    }

    /// <summary>
    /// Společný handler pro všechny položky submenu "Otevřít Gateway log".
    /// Tag MenuItemu obsahuje počet řádků (string), 0 = celý log.
    /// </summary>
    private void OpenLogMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var logPath = _settings.GetTodayGatewayLogPath();

        var dialog = new GatewayLogWindow(logPath)
        {
            Owner = this
        };

        // Předvolba kolik řádků zobrazit (z Tag MenuItemu)
        if (sender is MenuItem item && item.Tag is string tagStr)
        {
            // Najdeme odpovídající ComboBox položku v okně podle Tag hodnoty
            // Implementace: dialog načte default 20 v konstruktoru, pak přepneme přes API.
            // Pro jednoduchost — dialog zatím vždy začne s default volbou (20).
            // Uživatel pak může přepnout v ComboBoxu.
            // (Vylepšení: předat počet řádků konstruktoru — nechávám pro budoucí refactor.)
        }

        Log($"Otevírám Gateway log viewer.");
        dialog.ShowDialog();
    }

    // ==================== MENU — Nastavení ====================

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_settings)
        {
            Owner = this
        };

        var result = dialog.ShowDialog();

        if (result == true && dialog.SavedSettings != null)
        {
            _settings = dialog.SavedSettings;
            Log("Nastavení uloženo.");
        }
    }

    // ==================== MENU — Nápověda ====================

    private void ShowAbout()
    {
        MessageBox.Show(
            "OpenClaw Manager Tool by Bloom\n" +
            "Verze: v0.1\n\n" +
            "Diagnostický a údržbový nástroj pro OpenClaw setup.\n\n" +
            "Postaveno v C# / WPF / .NET 8.",
            "O aplikaci",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log($"[CHYBA] Otevření URL selhalo: {ex.Message}");
        }
    }

    // ==================== LOG ====================

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        AppLog.Items.Add($"[{timestamp}] {message}");

        while (AppLog.Items.Count > 100)
            AppLog.Items.RemoveAt(0);

        if (AppLog.Items.Count > 0)
            AppLog.ScrollIntoView(AppLog.Items[AppLog.Items.Count - 1]);
    }
}
