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
    // ── Inicializace theme (volat v konstruktoru po InitializeComponent) ──────
    private void InitTheme()
    {
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
        switch (theme)
        {
            case AppTheme.Modern:
            case AppTheme.Dark:
            case AppTheme.HighContrast:
            case AppTheme.Compact:
                ApplyModernUi();
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
        var uri = ThemeService.GetIconUri(AppTheme.Modern, iconName);
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
