using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class AboutWindow : Window
{
    private const string SyncWebMessage = "sync-workspaces";

    public AboutWindow()
    {
        InitializeComponent();
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);
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
        Title = cs ? "O aplikaci" : "About";
    }

    // Sync launcher
    private static void LaunchSyncWorkspaces()
    {
        try
        {
            var batPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "Sync-Workspaces.bat");

            if (!File.Exists(batPath))
            {
                batPath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "Scripts",
                    "Sync-Workspaces.bat"));
            }

            if (!File.Exists(batPath))
            {
                batPath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Scripts",
                    "Sync-Workspaces.bat"));
            }

            if (!File.Exists(batPath))
            {
                MessageBox.Show(
                    $"Script was not found at path:\n{batPath}",
                    "OpenClaw Sync",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
            });
        }
        catch (OperationCanceledException)
        {
            // User cancelled UAC.
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error while starting sync script:\n{ex.Message}",
                "OpenClaw Sync",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ── WebView2 logo ─────────────────────────────────────────────────────────
    private async Task InitWebViewAsync()
    {
        try
        {
            await SvgView.EnsureCoreWebView2Async();
            SvgView.CoreWebView2.WebMessageReceived += SvgView_WebMessageReceived;
            var svgPath = FindSvgPath();
            var svgContent = svgPath != null
                ? await File.ReadAllTextAsync(svgPath)
                : FallbackSvg();
            SvgView.NavigateToString(BuildHtml(svgContent));
        }
        catch { }
    }

    private void SvgView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (e.TryGetWebMessageAsString().Equals(SyncWebMessage, StringComparison.Ordinal))
        {
            LaunchSyncWorkspaces();
        }
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
        var darkLogoBackground = SettingsService.Current.Theme is
            OpenClawManager.Models.AppTheme.Dark or
            OpenClawManager.Models.AppTheme.StandardDark or
            OpenClawManager.Models.AppTheme.HighContrast;
        var background = darkLogoBackground ? "#191919" : "#F0F0F0";

        return $@"<!DOCTYPE html>
<html>
<head>
<style>
  html, body {{
    margin: 0; padding: 0;
    background: {background};
    display: flex;
    align-items: center;
    justify-content: center;
    height: 100vh;
    overflow: hidden;
    user-select: none;
    cursor: default;
  }}
  svg {{
    width: 140px;
    height: 140px;
    filter: drop-shadow(0 2px 6px rgba(0,0,0,0.15));
  }}
  #secretPrompt {{
    position: fixed;
    left: 49px;
    top: 69px;
    width: 46px;
    height: 18px;
    color: #9cf18a;
    font: 11px Consolas, monospace;
    white-space: pre;
    outline: none;
    user-select: none;
  }}
</style>
<script>
  let buffer = '';
  let resetTimer = null;
  const secret = 'admin';

  function renderPrompt(text) {{
    const prompt = document.getElementById('secretPrompt');
    if (prompt) prompt.textContent = '> ' + text + '_';
  }}

  function resetPromptSoon(delay) {{
    clearTimeout(resetTimer);
    resetTimer = setTimeout(() => {{
      buffer = '';
      renderPrompt('');
    }}, delay);
  }}

  document.addEventListener('DOMContentLoaded', () => {{
    renderPrompt('');
    document.body.tabIndex = 0;
    document.body.focus();
  }});

  document.addEventListener('pointerdown', () => document.body.focus());
  document.addEventListener('keydown', (event) => {{
    if (event.key === 'Backspace') {{
      buffer = buffer.slice(0, -1);
      renderPrompt(buffer);
      resetPromptSoon(3000);
      event.preventDefault();
      return;
    }}

    if (event.key === 'Escape') {{
      buffer = '';
      renderPrompt('');
      event.preventDefault();
      return;
    }}

    if (event.key.length !== 1 || !/^[a-zA-Z]$/.test(event.key)) return;

    buffer = (buffer + event.key.toLowerCase()).slice(-secret.length);
    renderPrompt(buffer);
    resetPromptSoon(3000);

    if (buffer === secret) {{
      renderPrompt('admin sync');
      window.chrome.webview.postMessage('{SyncWebMessage}');
      resetPromptSoon(1200);
    }}
  }});
</script>
</head>
<body>
{svgContent}
<div id='secretPrompt' aria-hidden='true'></div>
</body>
</html>";
    }

    private static string FallbackSvg() =>
        "<svg viewBox='0 0 100 40' xmlns='http://www.w3.org/2000/svg'>" +
        "<text x='50' y='28' font-size='12' fill='#333' " +
        "font-family='Consolas,monospace' text-anchor='middle'>OpenClaw</text></svg>";
}
