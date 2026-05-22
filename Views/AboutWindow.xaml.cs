using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class AboutWindow : Window
{
    private const string CommandPrefix = "command:";

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

    private static void LaunchOpenClawTools(string action = "")
    {
        try
        {
            var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "OpenClaw-Tools.ps1");

            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "Scripts",
                    "OpenClaw-Tools.ps1"));
            }

            if (!File.Exists(scriptPath))
            {
                scriptPath = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "Scripts",
                    "OpenClaw-Tools.ps1"));
            }

            if (!File.Exists(scriptPath))
            {
                MessageBox.Show(
                    $"OpenClaw tools script was not found at path:\n{scriptPath}",
                    "OpenClaw Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var actionArgs = string.IsNullOrWhiteSpace(action) ? "" : $" -Action {QuoteArgument(action)}";
            var psArguments = $"-NoExit -NoProfile -ExecutionPolicy Bypass -File {QuoteArgument(scriptPath)}{actionArgs}";
            var scriptDirectory = Path.GetDirectoryName(scriptPath) ?? AppContext.BaseDirectory;

            if (TryStartWindowsTerminal(psArguments, scriptDirectory))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = psArguments,
                UseShellExecute = true,
                WorkingDirectory = scriptDirectory,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error while starting OpenClaw tools:\n{ex.Message}",
                "OpenClaw Tools",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool TryStartWindowsTerminal(string powershellArguments, string workingDirectory)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "wt.exe",
                Arguments = $"-w new powershell.exe {powershellArguments}",
                UseShellExecute = true,
                WorkingDirectory = workingDirectory,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string QuoteArgument(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

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
        var message = e.TryGetWebMessageAsString();
        if (!message.StartsWith(CommandPrefix, StringComparison.Ordinal))
            return;

        var command = message[CommandPrefix.Length..].Trim().ToLowerInvariant();
        switch (command)
        {
            case "admin":
            case "root":
                LaunchOpenClawTools();
                break;
            case "sync":
                LaunchOpenClawTools("sync");
                break;
            case "diag":
            case "status":
                LaunchOpenClawTools("diag");
                break;
            case "acl":
                LaunchOpenClawTools("acl");
                break;
            case "build":
                LaunchOpenClawTools("build");
                break;
            case "test":
                LaunchOpenClawTools("test");
                break;
            case "check":
                LaunchOpenClawTools("check");
                break;
            default:
                if (Owner is MainWindow mainWindow)
                    mainWindow.ExecuteAboutCommand(command);
                break;
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
  #terminal rect[x='20'][y='26'] {{
    opacity: 0;
  }}
</style>
<script>
  const commands = new Set([
    'admin', 'root', 'sync', 'diag', 'status', 'acl', 'build', 'test', 'check',
    'replay', 'exit', 'legacy', 'dark', 'light', 'modern', 'crab', 'logs',
    'tokens', 'settings', 'help'
  ]);
  const maxCommandLength = Math.max(...Array.from(commands).map(command => command.length));
  let buffer = '';
  let resetTimer = null;

  function renderPrompt(text) {{
    const prompt = document.querySelector('#terminal text');
    if (!prompt) return;
    prompt.textContent = text ? '> ' + text + '_' : '>_';
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
    if (event.key === 'Enter') {{
      executeCommand(buffer);
      event.preventDefault();
      return;
    }}

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

    if (event.key.length !== 1 || !/^[a-zA-Z0-9_-]$/.test(event.key)) return;

    buffer = (buffer + event.key.toLowerCase()).slice(-maxCommandLength);
    renderPrompt(buffer);
    resetPromptSoon(3000);

    if (commands.has(buffer)) executeCommand(buffer);
  }});

  function executeCommand(command) {{
    command = (command || '').trim().toLowerCase();
    if (!commands.has(command)) return;
    renderPrompt(command);
    window.chrome.webview.postMessage('{CommandPrefix}' + command);
    buffer = '';
    resetPromptSoon(1200);
  }}
</script>
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
