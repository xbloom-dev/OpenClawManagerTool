using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using OpenClawManager.Services;
using OpenClawManager.ViewModels;

namespace OpenClawManager.Views;

public partial class AboutWindow : Window
{
    private const string CommandPrefix = "command:";
    private const string ToolsScriptName = "OpenClaw-Tools.ps1";
    private readonly IAppEnvironment _env;

    public AboutWindow()
        : this(new AboutViewModel(), new AppEnvironment())
    {
    }

    public AboutWindow(AboutViewModel viewModel, IAppEnvironment env)
    {
        _env = env;
        InitializeComponent();
        DataContext = viewModel;
        ModernPaletteRuntimeStyles.ApplyIfModernPalette(this);
        BtnClose.Click += (_, _) => Close();
        _ = InitWebViewAsync();
    }

    // ── WebView2 logo ─────────────────────────────────────────────────────────
    private async Task InitWebViewAsync()
    {
        try
        {
            await SvgView.EnsureCoreWebView2Async();
            SvgView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            SvgView.CoreWebView2.WebMessageReceived += SvgView_WebMessageReceived;
            var svgPath = FindSvgPath();
            var svgContent = svgPath != null
                ? await File.ReadAllTextAsync(svgPath)
                : FallbackSvg();
            SvgView.NavigateToString(BuildHtml(svgContent, GetLogoBackgroundCss()));
        }
        catch { }
    }

    private void SvgView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        if (string.IsNullOrWhiteSpace(message) ||
            !message.StartsWith(CommandPrefix, StringComparison.Ordinal))
            return;

        var command = message[CommandPrefix.Length..].Trim().ToLowerInvariant();
        if (ExecuteToolsCommand(command))
            return;

        var mainWindow = Owner as MainWindow ?? Application.Current.MainWindow as MainWindow;
        if (mainWindow == null)
            return;

        Dispatcher.BeginInvoke(new Action(() => mainWindow.ExecuteAboutCommand(command)));
    }

    private bool ExecuteToolsCommand(string command)
    {
        switch (command)
        {
            case "admin":
            case "root":
                LaunchOpenClawTools();
                return true;
            case "sync":
                LaunchOpenClawTools("sync");
                return true;
            case "diag":
            case "status":
                LaunchOpenClawTools("diag");
                return true;
            case "acl":
            case "build":
            case "test":
            case "check":
                LaunchOpenClawTools(command);
                return true;
            default:
                return false;
        }
    }

    private void LaunchOpenClawTools(string action = "")
    {
        try
        {
            var scriptPath = FindToolsScriptPath();
            if (scriptPath == null)
            {
                MessageBox.Show(
                    L10n.Format("Str_About_ToolsScriptMissing", ToolsScriptName),
                    L10n.Get("Str_About_ToolsTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var actionArgs = string.IsNullOrWhiteSpace(action) ? "" : $" -Action {QuoteArgument(action)}";
            var psArguments = $"-NoProfile -ExecutionPolicy Bypass -File {QuoteArgument(scriptPath)} -NoAdminPrompt{actionArgs}";
            var scriptDirectory = Path.GetDirectoryName(scriptPath) ?? _env.AppBaseDirectory;

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = psArguments,
                Verb = "runas",
                UseShellExecute = true,
                WorkingDirectory = scriptDirectory,
                WindowStyle = ProcessWindowStyle.Normal,
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                L10n.Format("Str_About_ToolsStartError", ex.Message),
                L10n.Get("Str_About_ToolsTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string? FindToolsScriptPath()
    {
        var exeDir = _env.AppBaseDirectory;
        var candidates = new[]
        {
            Path.Combine(exeDir, "Scripts", ToolsScriptName),
            Path.Combine(exeDir, ToolsScriptName),
            Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "Scripts", ToolsScriptName)),
            Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "Scripts", ToolsScriptName))
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string QuoteArgument(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

    private string? FindSvgPath()
    {
        var exeDir = _env.AppBaseDirectory;
        var candidates = new[]
        {
            Path.Combine(exeDir, "Resources", "app-logo.svg"),
            Path.Combine(exeDir, "app-logo.svg")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    private string GetLogoBackgroundCss()
    {
        if (Background is SolidColorBrush windowBrush)
            return ToCssColor(windowBrush.Color);

        var themedBrush = ThemeService.GetBrush("Theme.Brush.WindowBackground", Colors.Transparent);
        if (themedBrush is SolidColorBrush solidBrush)
            return ToCssColor(solidBrush.Color);

        return "#000000";
    }

    private static string ToCssColor(Color color)
    {
        if (color.A == 0)
            return "transparent";

        if (color.A == 255)
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        var alpha = (color.A / 255.0).ToString("0.###", CultureInfo.InvariantCulture);
        return $"rgba({color.R},{color.G},{color.B},{alpha})";
    }

    private static string BuildHtml(string svgContent, string background)
    {
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
  const aliases = new Map([
    ['easteregg', 'help'],
    ['egg', 'help'],
    ['about', 'help']
  ]);
  const acceptedInputs = new Set([...commands, ...aliases.keys()]);
  const maxCommandLength = Math.max(...Array.from(acceptedInputs).map(command => command.length));
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
  }});

  function executeCommand(command) {{
    command = (command || '').trim().toLowerCase();
    command = aliases.get(command) || command;
    if (!commands.has(command)) {{
      renderPrompt('unknown');
      buffer = '';
      resetPromptSoon(1200);
      return;
    }}

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
