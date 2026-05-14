// MainWindow.Theme.cs
// Partial class — theme switching + ikony v tlačítkách (v0.5+)
// Umístění: OpenClawManager/ (vedle MainWindow.xaml.cs)

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenClawManager.Models;
using OpenClawManager.Services;

namespace OpenClawManager;

public partial class MainWindow
{
    private sealed record ButtonVisualState(
        object? Content,
        double Height,
        Thickness Padding,
        Thickness BorderThickness,
        Brush Background,
        Brush BorderBrush);

    private sealed record ShellVisualState(Brush Background, Brush Foreground);

    private readonly Dictionary<Button, ButtonVisualState> _buttonVisualStates = new();
    private readonly Dictionary<Control, ShellVisualState> _shellControlStates = new();
    private readonly Dictionary<MenuItem, object?> _menuItemIcons = new();

    // ── Inicializace theme (volat v konstruktoru po InitializeComponent) ──────
    private void InitTheme()
    {
        CaptureThemeBaseline();

        // Přihlásit se na event — přepnutí tématu za běhu (ze SettingsWindow)
        ThemeService.ThemeChanged += OnThemeChanged;

        // Aplikovat téma z nastavení
        var theme = SettingsService.Current.Theme;
        ThemeService.Apply(theme);
    }

    // ── Callback při změně tématu (SettingsWindow → Save → ThemeService.Apply) ─
    private void OnThemeChanged(AppTheme theme)
    {
        // Musíme být na UI vlákně
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnThemeChanged(theme));
            return;
        }

        ApplyThemeToUi(theme);

        if (theme == AppTheme.Legacy)
        {
            StopSplashVideo();
            SplashOverlay.Visibility = Visibility.Collapsed;

            if (Terminal.IsTuiRunning)
            {
                Terminal.HideSplashBorder();
                Terminal.ShowWebView();
            }
            else
            {
                Terminal.ShowSplashBorder();
            }
        }
        else
        {
            Terminal.HideSplashBorder();
            SplashMedia.Visibility = Visibility.Collapsed;
            SplashProgress.Visibility = Visibility.Collapsed;

            if (!Terminal.IsTuiRunning)
                SplashOverlay.Visibility = Visibility.Visible;
        }
    }

    // ── Aplikace tématu na UI ─────────────────────────────────────────────────
    private void ApplyThemeToUi(AppTheme theme)
    {
        RestoreThemeBaseline();

        switch (theme)
        {
            case AppTheme.Modern:
            case AppTheme.Dark:
            case AppTheme.HighContrast:
                ApplyModernUi();
                break;
            case AppTheme.CrabCute:
                ApplyCrabCuteUi();
                break;
            case AppTheme.Legacy:
            default:
                ApplyLegacyUi();
                break;
        }
    }

    // ── Modern UI ─────────────────────────────────────────────────────────────
    private void ApplyModernUi()
    {
        // Ikony tlačítek — nahradit emoji za PNG ikony
        SetButtonIcon(BtnGatewayStart,    "start");
        SetButtonIcon(BtnGatewayStop,     "stop");
        SetButtonIcon(BtnGatewayRestart,  "restart");
        SetButtonIcon(BtnOpenPowerShell,  "powershell");
        SetButtonIcon(BtnOpenGatewayLog,  "gateway-log");
        SetButtonIcon(BtnCleaningTool,    "cleaning-tool");
        SetButtonIcon(BtnTokenManager,    "token-manager");
        SetButtonIcon(BtnDoctorFix,       "doctor-fix");

        // TUI tlačítko — ponechat dynamický stav (UpdateStartTuiButton ho řídí)
        // Ikony Start/Stop se přepínají v UpdateStartTuiButton()
        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    // ── CrabCute UI ──────────────────────────────────────────────────────────
    private void ApplyCrabCuteUi()
    {
        ApplyCrabCuteShell();

        SetCrabCuteButtonImage(BtnGatewayStart,   "start", 44);
        SetCrabCuteButtonImage(BtnGatewayStop,    "stop", 44);
        SetCrabCuteButtonImage(BtnGatewayRestart, "restart", 44);
        SetCrabCuteButtonImage(BtnOpenPowerShell, "powershell", 44);
        SetCrabCuteButtonImage(BtnOpenGatewayLog, "gateway-log", 44);
        SetCrabCuteButtonImage(BtnCleaningTool,   "cleaning-tool", 44);
        SetCrabCuteButtonImage(BtnTokenManager,   "token-manager", 44);
        SetCrabCuteButtonImage(BtnDoctorFix,      "doctor-fix", 44);

        SetMenuIcon(MnuOpenPowerShell, "menu-powershell");
        SetMenuIcon(MnuOpenGatewayLog, "log");
        SetMenuIcon(MnuSettings, "menu-settings");
        SetMenuIcon(MnuMenuSettings, "menu-settings");

        UpdateStartTuiButton(Terminal.IsTuiRunning);
    }

    private void ApplyCrabCuteShell()
    {
        var background = ThemeService.GetBrush("Theme.Brush.Background", Color.FromRgb(0xFF, 0xF7, 0xF0));
        var surface = ThemeService.GetBrush("Theme.Brush.Surface", Color.FromRgb(0xFF, 0xFF, 0xFF));
        var border = ThemeService.GetBrush("Theme.Brush.Border", Color.FromRgb(0xFF, 0xD8, 0xC2));
        var text = ThemeService.GetBrush("Theme.Brush.Text.Primary", Color.FromRgb(0x17, 0x20, 0x33));
        var secondary = ThemeService.GetBrush("Theme.Brush.Text.Secondary", Color.FromRgb(0x5F, 0x6B, 0x7A));

        Background = background;
        Foreground = text;

        MainMenu.Background = surface;
        MainMenu.Foreground = text;
        MainStatusBar.Background = surface;
        MainStatusBar.Foreground = secondary;
        MainGridSplitter.Background = border;

        foreach (var group in new[] { GrpActions, GrpLatency, GrpAppLog })
        {
            group.Background = surface;
            group.Foreground = text;
            group.BorderBrush = border;
            group.BorderThickness = new Thickness(1);
        }

        AppLog.Background = surface;
        AppLog.Foreground = text;
        AppLog.BorderBrush = border;
    }

    // ── Legacy UI — obnovit emoji TextBlock ───────────────────────────────────
    private void ApplyLegacyUi()
    {
        RestoreButtonLegacy(BtnGatewayStart,   "▶", "Green",  "Start");
        RestoreButtonLegacy(BtnGatewayStop,    "■", "Red",    "Stop");
        RestoreButtonLegacy(BtnGatewayRestart, "↻", "Orange", "Restart");
        RestoreButtonLegacy(BtnOpenPowerShell, "⚡", null,    BtnPowerShellLabel.Text);
        RestoreButtonLegacy(BtnOpenGatewayLog, "📄", null,   BtnGatewayLogLabel.Text);
        RestoreButtonLegacy(BtnCleaningTool,   "🧹", null,   BtnCleaningToolLabel.Text);
        RestoreButtonLegacy(BtnTokenManager,   "🔑", null,   BtnTokenManagerLabel.Text);
        RestoreButtonLegacy(BtnDoctorFix,      "🩺", null,   BtnDoctorFixLabel.Text);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Nastaví obsah tlačítka na PNG ikonu (Modern téma).
    /// Ikona je načtena z Resources/Icons/Modern/{name}.png jako embedded resource.
    /// Zachovává původní Label TextBlock pro lokalizaci — jen skryje emoji.
    /// </summary>
    private void SetButtonIcon(Button btn, string iconName)
    {
        var uri = ThemeService.GetIconUri(SettingsService.Current.Theme, iconName);
        if (uri == null) return;

        try
        {
            var img = new Image
            {
                Source = new BitmapImage(uri),
                Width  = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            // Najít existující Label TextBlock (druhý child v StackPanel)
            // a nahradit emoji TextBlock za Image
            if (btn.Content is StackPanel sp && sp.Children.Count >= 1)
            {
                // Nahradit první element (emoji TextBlock) za Image
                if (sp.Children[0] is TextBlock emoji)
                {
                    sp.Children.RemoveAt(0);
                    sp.Children.Insert(0, img);
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[Theme] Ikona {iconName} se nenačetla: {ex.Message}");
        }
    }

    private void SetCrabCuteButtonImage(Button btn, string iconName, double height)
    {
        var image = CreateThemeImage(iconName, height);
        if (image == null) return;

        btn.Content = image;
        btn.Height = height;
        btn.Padding = new Thickness(0);
        btn.BorderThickness = new Thickness(0);
        btn.Background = Brushes.Transparent;
        btn.BorderBrush = Brushes.Transparent;
    }

    private Image? CreateThemeImage(string iconName, double height)
    {
        var uri = ThemeService.GetIconUri(SettingsService.Current.Theme, iconName);
        if (uri == null) return null;

        try
        {
            var image = new Image
            {
                Source = new BitmapImage(uri),
                Height = height,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            return image;
        }
        catch (Exception ex)
        {
            Log($"[Theme] Ikona {iconName} se nenačetla: {ex.Message}");
            return null;
        }
    }

    private void SetMenuIcon(MenuItem item, string iconName)
    {
        item.Icon = CreateThemeImage(iconName, 18);
    }

    private void ApplyThemeSpecificTuiVisual(bool tuiRunning)
    {
        if (SettingsService.Current.Theme != AppTheme.CrabCute) return;

        SetCrabCuteButtonImage(BtnStartTui, tuiRunning ? "stop" : "tui", tuiRunning ? 44 : 88);
    }

    private void CaptureThemeBaseline()
    {
        if (_buttonVisualStates.Count > 0) return;

        foreach (var button in new[]
        {
            BtnStartTui,
            BtnGatewayStart,
            BtnGatewayStop,
            BtnGatewayRestart,
            BtnOpenPowerShell,
            BtnOpenGatewayLog,
            BtnCleaningTool,
            BtnTokenManager,
            BtnDoctorFix
        })
        {
            _buttonVisualStates[button] = new ButtonVisualState(
                button.Content,
                button.Height,
                button.Padding,
                button.BorderThickness,
                button.Background,
                button.BorderBrush);
        }

        foreach (var item in new[] { MnuOpenPowerShell, MnuOpenGatewayLog, MnuSettings, MnuMenuSettings })
        {
            _menuItemIcons[item] = item.Icon;
        }

        foreach (var control in new Control[] { MainMenu, MainStatusBar, GrpActions, GrpLatency, GrpAppLog, AppLog })
        {
            _shellControlStates[control] = new ShellVisualState(control.Background, control.Foreground);
        }
    }

    private void RestoreThemeBaseline()
    {
        Background = SystemColors.WindowBrush;
        Foreground = SystemColors.ControlTextBrush;
        MainGridSplitter.Background = Brushes.LightGray;

        foreach (var (button, state) in _buttonVisualStates)
        {
            button.Content = state.Content;
            button.Height = state.Height;
            button.Padding = state.Padding;
            button.BorderThickness = state.BorderThickness;
            button.Background = state.Background;
            button.BorderBrush = state.BorderBrush;
        }

        foreach (var (item, icon) in _menuItemIcons)
        {
            item.Icon = icon;
        }

        foreach (var (control, state) in _shellControlStates)
        {
            control.Background = state.Background;
            control.Foreground = state.Foreground;
        }
    }

    /// <summary>
    /// Obnoví původní emoji TextBlock v tlačítku (Legacy téma).
    /// </summary>
    private static void RestoreButtonLegacy(Button btn, string emoji, string? color, string label)
    {
        if (btn.Content is StackPanel sp && sp.Children.Count >= 1)
        {
            // Pokud je první child Image (z Modern), nahradit zpět TextBlock
            if (sp.Children[0] is Image)
            {
                sp.Children.RemoveAt(0);
                var tb = new TextBlock
                {
                    Text              = emoji,
                    FontFamily        = new FontFamily("Segoe UI Emoji"),
                    FontSize          = 14,
                    Margin            = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                if (color != null)
                {
                    tb.Foreground = (Brush)new BrushConverter().ConvertFromString(color)!;
                    tb.FontWeight = FontWeights.Bold;
                }
                sp.Children.Insert(0, tb);
            }
        }
    }
}
