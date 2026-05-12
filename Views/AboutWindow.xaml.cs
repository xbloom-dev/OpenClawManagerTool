using System.IO;
using System.Reflection;
using System.Windows;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

public partial class AboutWindow : Window
{
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
        TxtShortcut_F1.Text       = cs ? "O aplikaci" : "About";
        BtnClose.Content          = cs ? "Zavřít" : "Close";
        Title = cs ? "OpenClaw Manager — O aplikaci" : "OpenClaw Manager — About";
    }

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
        var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
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
