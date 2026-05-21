using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class AboutWindow : Window
{
    // ── Easter Egg — 5× klik na logo Craba ────────────────────────────────────
    private int _logoClickCount = 0;
    private DateTime _lastLogoClick = DateTime.MinValue;
    private DispatcherTimer? _progressFadeTimer;

    // Hvězdičky — vizuální odpočet kliků
    private static readonly string[] _progressStages =
    {
        "",           // 0 kliků — skrytý
        "★ ☆ ☆ ☆ ☆",  // 1
        "★ ★ ☆ ☆ ☆",  // 2
        "★ ★ ★ ☆ ☆",  // 3
        "★ ★ ★ ★ ☆",  // 4
        "🚀",          // 5 — spuštění
    };

    public AboutWindow()
    {
        InitializeComponent();
        BtnClose.Click += (_, _) => Close();
        ApplyLocalization();
        _ = InitWebViewAsync();
    }

    private void ApplyLocalization()
    {
        bool cs = L10n.Current == L10n.Language.CS;

        TxtShortcut_T.Text        = "Start/Stop OpenClaw TUI";
        TxtShortcut_G.Text        = cs ? "Start/Stop Gateway" : "Start/Stop Gateway";
        TxtShortcut_R.Text        = "Restart Gateway";
        TxtShortcut_C.Text        = cs ? "Vyčistit soubory" : "Cleaning Tool";
        TxtShortcut_Settings.Text = cs ? "Nastavení" : "Settings";
        TxtShortcut_L.Text        = cs ? "Živá data Gateway logu" : "Gateway live log";
        TxtShortcut_F1.Text       = cs ? "O aplikaci" : "About";
        TxtShortcut_AltF4.Text    = cs ? "Zavřít aplikaci" : "Close application";
        BtnClose.Content          = cs ? "Zavřít" : "Close";
        BtnClose.ToolTip          = L10n.Get("Str_Tip_AboutClose");
        Title = cs ? "OpenClaw Manager — O aplikaci" : "OpenClaw Manager — About";
    }

    // ── Logo overlay click handler ─────────────────────────────────────────────
    private void LogoClickArea_MouseLeftButtonDown(object sender,
        System.Windows.Input.MouseButtonEventArgs e)
    {
        var now = DateTime.Now;

        // Reset čítače pokud uplynulo více než 2 sekundy od posledního kliku
        if ((now - _lastLogoClick).TotalSeconds > 2)
        {
            _logoClickCount = 0;
            HideProgress();
        }

        _lastLogoClick = now;
        _logoClickCount++;

        if (_logoClickCount >= 5)
        {
            // Spuštění!
            _logoClickCount = 0;
            ShowProgress(5);
            // Krátká pauza před spuštěním aby uživatel viděl 🚀
            var launchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            launchTimer.Tick += (_, _) =>
            {
                launchTimer.Stop();
                HideProgress();
                LaunchSyncWorkspaces();
            };
            launchTimer.Start();
        }
        else
        {
            ShowProgress(_logoClickCount);
            // Auto-hide po 2 sekundách pokud uživatel nekliká dál
            _progressFadeTimer?.Stop();
            _progressFadeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _progressFadeTimer.Tick += (_, _) =>
            {
                _progressFadeTimer?.Stop();
                _progressFadeTimer = null;
                _logoClickCount = 0;
                HideProgress();
            };
            _progressFadeTimer.Start();
        }
    }

    private void ShowProgress(int count)
    {
        TxtEasterEggProgress.Text       = _progressStages[count];
        TxtEasterEggProgress.Visibility = Visibility.Visible;
    }

    private void HideProgress()
    {
        TxtEasterEggProgress.Visibility = Visibility.Hidden;
        TxtEasterEggProgress.Text       = "";
    }

    // ── Sync spouštěč ─────────────────────────────────────────────────────────
    private static void LaunchSyncWorkspaces()
    {
        var exeDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(exeDir, "Sync-OpenClaw.bat"),
            Path.Combine(exeDir, "Sync-OpenClawWorkspaces.bat"),
            Path.Combine(exeDir, "scripts", "Sync-OpenClaw.bat"),
            Path.Combine(exeDir, "scripts", "Sync-OpenClawWorkspaces.bat"),
            Path.Combine(exeDir, "..", "..", "..", "scripts", "Sync-OpenClaw.bat"),
            Path.Combine(exeDir, "..", "Sync-OpenClawWorkspaces.bat"),
            @"E:\OpenClaw\OpenClawManager\scripts\Sync-OpenClaw.bat",
            @"E:\OpenClaw\OpenClawManager\Sync-OpenClawWorkspaces.bat",
            @"E:\OpenClaw\CodexWorkspace\scripts\Sync-OpenClaw.bat",
            @"E:\OpenClaw\ClaudeWorkspace\scripts\Sync-OpenClaw.bat",
            @"E:\OpenClaw\Sync-OpenClawWorkspaces.bat",
        };

        var batPath = candidates.FirstOrDefault(File.Exists);
        if (batPath == null)
        {
            MessageBox.Show(
                "Sync-OpenClawWorkspaces.bat nenalezen.\n" +
                "Očekáváno vedle EXE nebo ve složce scripts.",
                "OpenClaw Sync", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName        = batPath,
                UseShellExecute = true,
                Verb            = "runas",   // UAC — skript potřebuje admin
            });
        }
        catch (OperationCanceledException)
        {
            // Uživatel zamítl UAC — tiché zrušení
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sync selhal: {ex.Message}",
                "OpenClaw Sync", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── WebView2 logo ─────────────────────────────────────────────────────────
    private async Task InitWebViewAsync()
    {
        try
        {
            await SvgView.EnsureCoreWebView2Async();
            var svgPath = FindSvgPath();
            var svgContent = svgPath != null
                ? await File.ReadAllTextAsync(svgPath)
                : FallbackSvg();
            SvgView.NavigateToString(BuildHtml(svgContent));
        }
        catch { }
    }

    private static string? FindSvgPath()
    {
        var exeDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(exeDir, "Resources", "app-logo.svg"),
            Path.Combine(exeDir, "app-logo.svg"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app-logo.svg"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private static string BuildHtml(string svgContent)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
<style>
  html, body {{
    margin: 0; padding: 0;
    background: #F0F0F0;
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100vh;
    overflow: hidden;
  }}
  svg {{
    width: 140px;
    height: 140px;
    filter: drop-shadow(0 2px 6px rgba(0,0,0,0.15));
  }}
</style>
</head>
<body>
{svgContent}
</body>
</html>";
    }

    private static string FallbackSvg() =>
        "<svg viewBox='0 0 100 40' xmlns='http://www.w3.org/2000/svg'>" +
        "<text x='50' y='28' font-size='12' fill='#333' " +
        "font-family='Consolas,monospace' text-anchor='middle'>OpenClaw</text></svg>";
}
