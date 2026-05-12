using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
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

    // Buffering — sbíráme output z ConPTY do bufferu a flushujeme dávkově (10ms timer).
    private readonly System.Text.StringBuilder _outputBuffer = new();
    private readonly object _bufferLock = new();
    private DispatcherTimer? _flushTimer;
    private DispatcherTimer? _webViewReadyTimeoutTimer;

    public event Action<bool>? TuiStateChanged;

    public bool IsTuiRunning => _conpty != null && _conpty.IsRunning;

    /// <summary>
    /// Sestaví HTML stránku s lokálním xterm.js + bridge na C#.
    ///
    /// DŮLEŽITÉ: všechny non-ASCII znaky v JS string literálech musí být
    /// jako \uXXXX unicode escape — heredoc string je UTF-8 v C#, ale
    /// WebView2 NavigateToString ho předává Chromiu jako UTF-16 string
    /// bez BOM, a inline text v script bloku se může interpretovat chybně
    /// pokud obsahuje raw UTF-8 multi-byte sekvence.
    /// </summary>
    private static string BuildTerminalHtml(string xtermJs, string xtermCss) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <style>
                {{xtermCss}}
                html, body {
                    margin: 0;
                    padding: 0;
                    background: #1e1e1e;
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
                        background: '#1e1e1e',
                        foreground: '#dcdcdc',
                        cursor: '#dcdcdc'
                    },
                    cursorBlink: true,
                    scrollback: 5000
                });
                term.open(document.getElementById('terminal'));

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

    private static string BuildTerminalHtmlFromResources()
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

            WebView.CoreWebView2.NavigationCompleted += async (_, e) =>
            {
                if (!e.IsSuccess)
                {
                    _webViewFailed = true;
                    StatusText.Text = $"Chyba nacitani: {e.WebErrorStatus}";
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
                        ? "Terminal se nenacetl. Lokalni xterm.js se nespustil."
                        : "Terminal did not load. Local xterm.js did not start.";
                }
            };

            StatusText.Text = L10n.IsCzech
                ? "Nacitam lokalni xterm.js..."
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
                    ? "Terminal se nena\u010detl. Chyb\u00ed lok\u00e1ln\u00ed xterm.js assety."
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
        if (_webViewReady) return; // idempotentní — oba handlery mohou dorazit

        _webViewReady = true;
        _webViewFailed = false;
        _webViewReadyTimeoutTimer?.Stop();
        _webViewReadyTimeoutTimer = null;

        // Zobrazit WebView, schovat splash
        WebView.Visibility = Visibility.Visible;
        SplashBorder.Visibility = Visibility.Collapsed;

        StatusText.Text = L10n.IsCzech
            ? "Klikni na \"OpenClaw TUI\" pro spusteni."
            : "Click \"OpenClaw TUI\" to start.";
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
                    ? "Termin\u00e1l nen\u00ed p\u0159ipraven. Chyb\u00ed lok\u00e1ln\u00ed xterm.js assety."
                    : "Terminal is not ready. Local xterm.js assets are missing.")
                : (L10n.IsCzech
                    ? "Termin\u00e1l se je\u0161t\u011b na\u010d\u00edt\u00e1. Zkus to pros\u00edm za chv\u00edli."
                    : "Terminal is still loading. Please try again shortly.");
            return;
        }

        try
        {
            WebView.CoreWebView2.PostWebMessageAsJson("{\"type\":\"clear\"}");

            _conpty = new ConPtyProcess();
            _conpty.OutputReceived += OnConPtyOutput;
            _conpty.ProcessExited  += OnConPtyExited;

            _conpty.Start(GatewayService.BuildCmdExeCommand("tui"));

            // WebView je již Visible od OnWebViewReady — jen schovat splash
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
            ? "TUI ukon\u010deno. Klikni na \u201eOpenClaw TUI\u201c pro nov\u00fd start."
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
            _outputBuffer.Append("\r\n\x1b[90m[Proces ukon\u010den]\x1b[0m\r\n");
        }

        Dispatcher.BeginInvoke(new Action(() =>
        {
            StatusText.Text = L10n.IsCzech
                ? "TUI ukon\u010deno. Klikni na \u201eOpenClaw TUI\u201c pro nov\u00fd start."
                : "TUI stopped. Click \"OpenClaw TUI\" to start again.";
            SplashBorder.Visibility = Visibility.Visible;
            TuiStateChanged?.Invoke(false);
        }));
    }

    private void Cleanup()
    {
        _flushTimer?.Stop();
        _flushTimer = null;
        _webViewReadyTimeoutTimer?.Stop();
        _webViewReadyTimeoutTimer = null;
        _conpty?.Dispose();
        _conpty = null;
    }
}
