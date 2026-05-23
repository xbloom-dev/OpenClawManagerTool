using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using OpenClawManager.Services;

namespace OpenClawManager.Views;

/// <summary>
/// Terminal embedded v aplikaci — WebView2 + xterm.js + ConPTY.
///
/// Architektura:
/// [openclaw tui proces] ↔ [ConPtyProcess] ↔ [WebView2 PostWebMessage] ↔ [xterm.js]
///
/// Stdout z ConPTY → xterm.js přes term.write()
/// Stdin do ConPTY ← xterm.js přes onData event + window.chrome.webview.postMessage()
///
/// Viditelnost:
/// - WebView: Collapsed při startu → Visible při _webViewReady = true (xterm.js připraven)
/// - SplashBorder: viditelný při startu → Collapsed při _webViewReady → Visible po StopTui
/// </summary>
public partial class TerminalControl : UserControl
{
    private ConPtyProcess? _conpty;
    private bool _webViewReady = false;
    private bool _webViewFailed = false;
    private bool _isDisposed;

    // Buffering — sbíráme output z ConPTY do bufferu a flushujeme dávkově (10ms timer).
    private readonly System.Text.StringBuilder _outputBuffer = new();
    private readonly object _bufferLock = new();
    private DispatcherTimer? _flushTimer;
    private DispatcherTimer? _webViewReadyTimeoutTimer;
    private string _shellBackgroundHex = "#191919";

    public event Action<bool>? TuiStateChanged;

    public bool IsTuiRunning => _conpty != null && _conpty.IsRunning;

    /// <summary>
    /// Skryje ASCII art SplashBorder. Volá MainWindow v Modern theme kde
    /// SplashOverlay přebírá roli splash screenu.
    /// </summary>
    public void HideSplashBorder()
    {
        SplashBorder.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Zobrazí ASCII art SplashBorder. Volá MainWindow při přepnutí na Legacy theme.
    /// </summary>
    public void ShowSplashBorder()
    {
        SplashBorder.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Zobrazí WebView. Volá MainWindow po skrytí SplashOverlay.
    /// WebView musí zůstat Hidden dokud SplashOverlay překrývá terminál —
    /// WebView2 HWND (Win32 okno) by jinak vyskočil nad WPF overlay.
    /// </summary>
    public void ShowWebView()
    {
        WebView.Visibility = Visibility.Visible;
    }

    public void SetShellBackground(Brush brush)
    {
        Background = brush;
        SplashBorder.Background = brush;

        if (brush is SolidColorBrush solid)
        {
            _shellBackgroundHex = ToHex(solid.Color);
            ApplyWebViewBackground();
        }
    }

    public void ResetShellBackground()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0x19, 0x19, 0x19));
        Background = brush;
        SplashBorder.Background = brush;
        _shellBackgroundHex = "#191919";
        ApplyWebViewBackground();
    }

    private static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    private void ApplyWebViewBackground()
    {
        if (WebView.CoreWebView2 == null) return;

        var background = JsonSerializer.Serialize(_shellBackgroundHex);
        _ = WebView.CoreWebView2.ExecuteScriptAsync(
            $"window.openClawApplyTheme && window.openClawApplyTheme({background});");
    }

    /// <summary>
    /// Sestaví HTML stránku s lokálním xterm.js + bridge na C#.
    ///
    /// DŮLEŽITÉ: všechny non-ASCII znaky v JS string literálech musí být
    /// jako \uXXXX unicode escape — heredoc string je UTF-8 v C#, ale
    /// WebView2 NavigateToString ho předává Chromiu jako UTF-16 string
    /// bez BOM, a inline text v script bloku se může interpretovat chybně
    /// pokud obsahuje raw UTF-8 multi-byte sekvence.
    /// </summary>
    private string BuildTerminalHtml(string xtermJs, string xtermCss) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                {{xtermCss}}
                html, body {
                    margin: 0;
                    padding: 0;
                    background: {{_shellBackgroundHex}};
                    overflow: hidden;
                    height: 100vh;
                    display: flex;
                    justify-content: center;
                }
                #terminal {
                    padding: 8px;
                }
                ::-webkit-scrollbar { display: none; }
                .xterm-viewport::-webkit-scrollbar { display: none; }
                .xterm-viewport { scrollbar-width: none; }
            </style>
        </head>
        <body>
            <div id="terminal"></div>
            <script>
                {{xtermJs}}
            </script>
            <script>
                const term = new Terminal({
                    cols: 120,
                    rows: 35,
                    fontFamily: 'Consolas, "Courier New", monospace',
                    fontSize: 14,
                    theme: {
                        background: '{{_shellBackgroundHex}}',
                        foreground: '#dcdcdc',
                        cursor: '#dcdcdc'
                    },
                    cursorBlink: true,
                    scrollback: 5000
                });
                term.open(document.getElementById('terminal'));
                window.openClawTerminal = term;
                window.openClawApplyTheme = (background) => {
                    document.documentElement.style.background = background;
                    document.body.style.background = background;
                    term.options.theme = Object.assign({}, term.options.theme, { background });
                };

                // \u010cek\u00e1 se na ConPTY... = "Čeká se na ConPTY..."
                // Unicode escape nutný — viz komentář u BuildTerminalHtml
                term.writeln('\x1b[90m\u010cek\u00e1 se na ConPTY...\x1b[0m');

                window.chrome.webview.addEventListener('message', (event) => {
                    if (typeof event.data === 'string') {
                        term.write(event.data);
                        term.scrollToBottom();
                    } else if (event.data && event.data.type === 'clear') {
                        term.clear();
                    }
                });

                term.onData(data => {
                    term.scrollToBottom();
                    window.chrome.webview.postMessage({ type: 'input', data: data });
                });

                term.focus();
                window.chrome.webview.postMessage({ type: 'ready' });
            </script>
        </body>
        </html>
        """;

    private static string LoadTerminalResource(string relativePath)
    {
        var uri = new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
        var resource = Application.GetResourceStream(uri)
            ?? throw new FileNotFoundException($"Missing terminal asset: {relativePath}");

        using var reader = new StreamReader(resource.Stream, Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private string BuildTerminalHtmlFromResources()
    {
        var css = LoadTerminalResource("Resources/Terminal/xterm.min.css");
        var js  = LoadTerminalResource("Resources/Terminal/xterm.min.js")
            .Replace("</script>", "<\\/script>", StringComparison.OrdinalIgnoreCase);
        return BuildTerminalHtml(js, css);
    }

    public TerminalControl()
    {
        InitializeComponent();
        Loaded   += async (_, _) => await InitializeWebView();
        Unloaded += (_, _) => Cleanup();

        _flushTimer = new DispatcherTimer(DispatcherPriority.Send)
        {
            Interval = TimeSpan.FromMilliseconds(10)
        };
        _flushTimer.Tick += FlushBuffer;
        _flushTimer.Start();
    }

    private void FlushBuffer(object? sender, EventArgs e)
    {
        if (!_webViewReady || WebView.CoreWebView2 == null) return;

        string? toSend = null;
        lock (_bufferLock)
        {
            if (_outputBuffer.Length > 0)
            {
                toSend = _outputBuffer.ToString();
                _outputBuffer.Clear();
            }
        }

        if (toSend != null)
            WebView.CoreWebView2.PostWebMessageAsString(toSend);
    }

    private static string GetWebViewUserDataFolder()
    {
        var processFolder = Process.GetCurrentProcess().Id.ToString();
        var primary = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenClawManager", "WebView2", processFolder);

        if (TryPrepareUserDataFolder(primary))
            return primary;

        var fallback = Path.Combine(AppContext.BaseDirectory, "WebView2Data", processFolder);
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static bool TryPrepareUserDataFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, ".write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch { return false; }
    }

    private async Task InitializeWebView()
    {
        try
        {
            // Počkat až WPF dokončí layout — ApplicationIdle + malý delay
            // zabraňuje race condition kdy WebView2 inicializuje dřív než
            // má hostitelské okno stabilní HWND.
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(500);

            StatusText.Text = "Inicializuji WebView2...";

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: GetWebViewUserDataFolder());

            await WebView.EnsureCoreWebView2Async(env);

            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            WebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            StatusText.Text = L10n.IsCzech
                ? "Načítám lokální xterm.js..."
                : "Loading local xterm.js...";

            WebView.NavigateToString(BuildTerminalHtmlFromResources());

            // Timeout 10s — pokud xterm.js neodpoví přes "ready" zprávu
            _webViewReadyTimeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _webViewReadyTimeoutTimer.Tick += (_, _) =>
            {
                _webViewReadyTimeoutTimer?.Stop();
                _webViewReadyTimeoutTimer = null;
                if (_webViewReady) return;

                _webViewFailed = true;
                StatusText.Text = L10n.IsCzech
                    ? "Terminál se nenačetl. Chybí lokální xterm.js assety."
                    : "Terminal did not load. Local xterm.js assets are missing.";
            };
            _webViewReadyTimeoutTimer.Start();
        }
        catch (Exception ex)
        {
            _webViewFailed = true;
            StatusText.Text = $"WebView2 chyba: {ex.Message}";
        }
    }

    /// <summary>
    /// Centralizovaná logika pro přechod do stavu "terminál připraven".
    /// Volána z NavigationCompleted i z OnWebMessageReceived ("ready").
    /// </summary>
    private void OnWebViewReady()
    {
        if (_webViewReady) return;

        _webViewReady = true;
        _webViewFailed = false;
        _webViewReadyTimeoutTimer?.Stop();
        _webViewReadyTimeoutTimer = null;

        // WebView zůstane Hidden — zobrazí se přes ShowWebView() až MainWindow
        // skryje SplashOverlay. WebView2 HWND by jinak překryl WPF splash video.
        // SplashBorder NESKRÝVAT — zůstane viditelný dokud uživatel neklikne
        // na OpenClaw TUI (DisposeSplash() ho skryje přes HideSplashBorder()).

        StatusText.Text = L10n.IsCzech
            ? "Klikni na \"OpenClaw TUI\" pro spuštění."
            : "Click \"OpenClaw TUI\" to start.";
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_isDisposed) return;

        if (!e.IsSuccess)
        {
            _webViewFailed = true;
            StatusText.Text = $"Chyba načítání: {e.WebErrorStatus}";
            return;
        }

        var ready = await WebView.CoreWebView2.ExecuteScriptAsync(
            "Boolean(window.Terminal && document.querySelector('.xterm'))");

        if (string.Equals(ready, "true", StringComparison.OrdinalIgnoreCase))
        {
            OnWebViewReady();
        }
        else
        {
            _webViewFailed = true;
            StatusText.Text = L10n.IsCzech
                ? "Terminál se nenačetl. Lokální xterm.js se nespustil."
                : "Terminal did not load. Local xterm.js did not start.";
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.WebMessageAsJson;
            using var doc = JsonDocument.Parse(json);
            var type = doc.RootElement.GetProperty("type").GetString();

            switch (type)
            {
                case "ready":
                    Dispatcher.BeginInvoke(OnWebViewReady);
                    break;

                case "input":
                    var data = doc.RootElement.GetProperty("data").GetString();
                    _conpty?.WriteInput(data ?? "");
                    break;
            }
        }
        catch { }
    }

    public void StartTui()
    {
        if (_conpty != null && _conpty.IsRunning)
            return;

        if (!_webViewReady)
        {
            StatusText.Text = _webViewFailed
                ? (L10n.IsCzech
                    ? "Terminál není připraven. Chybí lokální xterm.js assety."
                    : "Terminal is not ready. Local xterm.js assets are missing.")
                : (L10n.IsCzech
                    ? "Terminál se ještě načítá. Zkus to prosím za chvíli."
                    : "Terminal is still loading. Please try again shortly.");
            return;
        }

        try
        {
            WebView.CoreWebView2.PostWebMessageAsJson("{\"type\":\"clear\"}");

            _conpty = new ConPtyProcess();
            _conpty.OutputReceived += OnConPtyOutput;
            _conpty.ProcessExited  += OnConPtyExited;

            _conpty.Start(App.GetService<IGatewayService>().BuildCmdExeCommand("tui"));

            // Zajistit že WebView je Visible (mohl zůstat Hidden)
            WebView.Visibility = Visibility.Visible;
            SplashBorder.Visibility = Visibility.Collapsed;

            TuiStateChanged?.Invoke(true);
        }
        catch (Exception ex)
        {
            lock (_bufferLock)
            {
                _outputBuffer.Append($"\r\n\x1b[31m[CHYBA] {ex.Message}\x1b[0m\r\n");
            }
        }
    }

    public void StopTui()
    {
        if (_conpty == null) return;

        _conpty.Dispose();
        _conpty = null;

        StatusText.Text = L10n.IsCzech
            ? "TUI ukončeno. Klikni na „OpenClaw TUI“ pro nový start."
            : "TUI stopped. Click \"OpenClaw TUI\" to start again.";
        SplashBorder.Visibility = Visibility.Visible;

        TuiStateChanged?.Invoke(false);
    }

    private void OnConPtyOutput(string data)
    {
        lock (_bufferLock)
        {
            _outputBuffer.Append(data);
        }
    }

    private void OnConPtyExited()
    {
        lock (_bufferLock)
        {
            _outputBuffer.Append("\r\n\x1b[90m[Proces ukončen]\x1b[0m\r\n");
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            StatusText.Text = L10n.IsCzech
                ? "TUI ukončeno. Klikni na „OpenClaw TUI“ pro nový start."
                : "TUI stopped. Click \"OpenClaw TUI\" to start again.";
            SplashBorder.Visibility = Visibility.Visible;
            TuiStateChanged?.Invoke(false);
        }));
    }

    private void Cleanup()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _flushTimer?.Stop();
        _flushTimer = null;
        _webViewReadyTimeoutTimer?.Stop();
        _webViewReadyTimeoutTimer = null;
        _conpty?.Dispose();
        _conpty = null;

        if (WebView.CoreWebView2 != null)
        {
            WebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            WebView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        }

        WebView.Dispose();
    }

    public void Shutdown()
    {
        Cleanup();
    }
}
