using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using OpenClawManager.Models;
using OpenClawManager.Services;
using OpenClawManager.Views;
using OpenClawManager.ViewModels;

namespace OpenClawManager;

public partial class MainWindow : Window, IMainWindowCallback
{
    private readonly ISettingsService _settingsService;
    private readonly IGatewayService _gatewayService;
    private readonly IProcessDetector _processDetector;
    private readonly IAppEnvironment _env;
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _statusTimer;
    private Action<string> Log => _vm.Log;

    private static Brush ActionPositiveBrush =>
        ThemeService.GetBrush("Brush.ActionPositive", Color.FromRgb(0xD0, 0xFF, 0xD0));
    private static Brush ActionDangerBrush =>
        ThemeService.GetBrush("Brush.ActionDanger", Color.FromRgb(0xFF, 0xD0, 0xD0));

    public MainWindow()
        : this(new AppEnvironment())
    {
    }

    private MainWindow(IAppEnvironment env)
        : this(new SettingsService(env), env)
    {
    }

    private MainWindow(ISettingsService settingsService, IAppEnvironment env)
        : this(
            settingsService,
            new GatewayService(settingsService, new ProcessDetector()),
            new ResourceMonitor(),
            new ProcessDetector(),
            env,
            new MainViewModel(
                new GatewayService(settingsService, new ProcessDetector()),
                new ResourceMonitor(),
                new ProcessDetector(),
                settingsService))
    {
    }

    public MainWindow(
        ISettingsService settingsService,
        IGatewayService gatewayService,
        IResourceMonitor resourceMonitor,
        IProcessDetector processDetector,
        IAppEnvironment env,
        MainViewModel viewModel)
    {
        _settingsService = settingsService;
        _gatewayService = gatewayService;
        _processDetector = processDetector;
        _env = env;
        _vm = viewModel;

        InitializeComponent();
        DataContext = _vm;

        AppLog.ItemsSource = _vm.AppLogItems;
        _vm.LogAppended += OnViewModelLogAppended;
        _vm.GatewayReadyForTui += OnViewModelGatewayReadyForTui;
        _vm.GatewayStateChanged += OnViewModelGatewayStateChanged;

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

        MnuOpenOpenClawFolder.Click += (_, _) => OpenInExplorer(_settingsService.Settings.OpenClawPath);
        MnuOpenTempFolder.Click += (_, _) => OpenInExplorer(_settingsService.Settings.TempPath);
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
        _ = _vm.UpdateStatusAsync();
        _vm.Log(L10n.Get("Str_Log_AppStarted"));

        // v0.5: tema + splash screen (pořadí důležité: theme před splash)
        InitTheme();
        InitSplash();
    }

    void IMainWindowCallback.StartTui() => Terminal.StartTui();
    void IMainWindowCallback.StopTui() => Terminal.StopTui();
    bool IMainWindowCallback.IsTuiRunning => Terminal.IsTuiRunning;
    void IMainWindowCallback.DisposeSplash() => DisposeSplash();
    void IMainWindowCallback.RefreshLocalizationAndLayout()
    {
        ApplyLocalization();
        ReapplyCurrentThemeLayoutAfterLocalization();
    }

    private void OnViewModelLogAppended(object? sender, EventArgs e)
    {
        if (AppLog.Items.Count > 0)
            AppLog.ScrollIntoView(AppLog.Items[AppLog.Items.Count - 1]);
    }

    private void OnViewModelGatewayReadyForTui(object? sender, EventArgs e)
    {
        Terminal.StartTui();
    }

    private void OnViewModelGatewayStateChanged(object? sender, EventArgs e)
    {
        UpdateStartTuiButton(Terminal.IsTuiRunning);
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
        _vm.RefreshLocalization();

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
                if (_vm.IsGatewayRunning)
                    BtnGatewayStop_Click(this, new RoutedEventArgs());
                else if (_vm.IsGatewayStoppedOrFailed)
                    BtnGatewayStart_Click(this, new RoutedEventArgs());
                e.Handled = true; break;
            case Key.R:
                if (_vm.IsGatewayRunning)
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

    private async void StatusTimer_Tick(object? sender, EventArgs e) => await _vm.UpdateStatusAsync();

    // ==================== Gateway tlačítka ====================

    private void BtnGatewayStart_Click(object? sender, RoutedEventArgs e)
    {
        _vm.StartGateway();
    }

    private void BtnGatewayStop_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new StopGatewayDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        _vm.ResetLatencyAndWaiting();
        Terminal.StopTui();
        _vm.StopGateway(dialog.CloseTui);
    }

    private async void BtnGatewayRestart_Click(object? sender, RoutedEventArgs e)
    {
        Terminal.StopTui();
        await _vm.RestartGatewayAsync();
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
                _vm.Log(L10n.Get("Str_Log_TuiStopping"));
                Terminal.StopTui();
                return;
            }

            if (_vm.IsGatewayRunning)
            {
                _vm.Log(L10n.Get("Str_Log_TuiStarting"));
                Terminal.StartTui();
                return;
            }

            var logPath = _settingsService.Settings.GetTodayGatewayLogPath();

            _vm.Log(L10n.Get("Str_Log_DeletingLog"));
            LogMonitor.DeleteLogIfExists(logPath);
            _vm.ResetLatencyAndWaiting();

            if (_processDetector.IsGatewayRunning())
            {
                _vm.Log(L10n.Get("Str_Log_GatewayStopping"));
                _gatewayService.Stop();
                await Task.Delay(2000);
            }

            _vm.StartGateway();
        }
        catch (Exception ex)
        {
            _vm.Log($"[CHYBA] {ex.Message}");
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
        else if (_vm.IsGatewayRunning)
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
        _vm.Log(L10n.Get("Str_Log_OpeningCleaningTool"));
        var dialog = App.GetService<CleaningWindow>();
        dialog.Owner = this;
        dialog.ShowDialog();
        _vm.Log(L10n.Get("Str_Log_ClosedCleaningTool"));
    }

    private void BtnTokenManager_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new TokenManagerWindow(App.GetService<ITokenService>(), _settingsService) { Owner = this };
        dialog.ShowDialog();
    }

    private void BtnDoctorFix_Click(object? sender, RoutedEventArgs e)
    {
        _vm.RunDoctorFix();
    }

    private void OpenInExplorer(string path)
    {
        if (!Directory.Exists(path)) { _vm.Log($"[CHYBA] Složka neexistuje: {path}"); return; }
        try { Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{path}\"", UseShellExecute = true }); }
        catch (Exception ex) { _vm.Log($"[CHYBA] {ex.Message}"); }
    }

    private void OpenPowerShell()
    {
        var workDir = _settingsService.Settings.PowerShellWorkingDir;
        if (!Directory.Exists(workDir)) workDir = "";
        try { Process.Start(new ProcessStartInfo { FileName = "powershell.exe", Arguments = "-NoExit -NoProfile", UseShellExecute = true, WorkingDirectory = workDir }); }
        catch (Exception ex) { _vm.Log($"[CHYBA] {ex.Message}"); }
    }

    private void OpenGatewayLog(int lines)
    {
        var logPath = _settingsService.Settings.GetTodayGatewayLogPath();
        var dialog = new GatewayLogWindow(logPath, lines) { Owner = this };
        dialog.ShowDialog();
    }

    private void OpenLiveGatewayLog(int lines)
    {
        var logPath = _settingsService.Settings.GetTodayGatewayLogPath();
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
        var dialog = App.Services.GetRequiredService<SettingsWindow>();
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            var lang = _settingsService.Settings.Language == "EN"
                ? L10n.Language.EN : L10n.Language.CS;
            L10n.Apply(lang);
            _vm.RefreshLocalization();
            ApplyLocalization();
            ReapplyCurrentThemeLayoutAfterLocalization();
            _vm.Log(L10n.Get("Str_Log_SettingsSaved"));
        }
    }

    private void ShowAbout()
    {
        var dialog = App.Services.GetRequiredService<AboutWindow>();
        dialog.Owner = this;
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
            _vm.Log("[About] Splash replay skipped because OpenClaw TUI is running.");
            return;
        }

        _vm.Log("[About] Replaying splash screen.");
        StopSplashVideo();
        InitSplash();
    }

    private void ApplyThemeFromAboutCommand(AppTheme theme)
    {
        var settings = _settingsService.Settings;
        if (settings.Theme == theme)
        {
            _vm.Log($"[About] Theme already active: {theme}.");
            return;
        }

        settings.Theme = theme;
        if (!_settingsService.Save(settings))
        {
            _vm.Log($"[CHYBA] Theme switch failed: {theme}.");
            return;
        }

        ThemeService.Apply(theme);
        ApplyLocalization();
        ReapplyCurrentThemeLayoutAfterLocalization();
        _vm.Log($"[About] Theme switched to {theme}.");
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

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_processDetector.IsGatewayRunning())
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
                    _gatewayService.StopAndCloseTui();
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
        _vm.LogAppended -= OnViewModelLogAppended;
        _vm.GatewayReadyForTui -= OnViewModelGatewayReadyForTui;
        _vm.GatewayStateChanged -= OnViewModelGatewayStateChanged;
        ThemeService.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }
}
