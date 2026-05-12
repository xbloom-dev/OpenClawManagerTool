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
/// Terminal embedded v aplikaci â€” WebView2 + xterm.js + ConPTY.
///
/// Architektura:
/// [openclaw tui proces] â†â†’ [ConPtyProcess] â†â†’ [WebView2 PostWebMessage] â†â†’ [xterm.js]
///
/// Stdout z ConPTY â†’ xterm.js pĹ™es term.write()
/// Stdin do ConPTY â† xterm.js pĹ™es onData event + window.chrome.webview.postMessage()
///
/// FixnĂ­ velikost 120Ă—40 (ĹľĂˇdnĂ˝ resize ve v0.3).
/// </summary>
public partial class TerminalControl : UserControl
{
    private ConPtyProcess? _conpty;
    private bool _webViewReady = false;
    private bool _webViewFailed = false;

    // Buffering optimalizace â€” sbĂ­rĂˇme output z ConPTY do bufferu
    // a flushujeme do xterm.js dĂˇvkovÄ› (eliminuje per-keystroke lag).
    private readonly System.Text.StringBuilder _outputBuffer = new();
    private readonly object _bufferLock = new();
    private DispatcherTimer? _flushTimer;
    private DispatcherTimer? _webViewReadyTimeoutTimer;

    /// <summary>
    /// VyvolĂˇ se kdyĹľ TUI proces nastartuje nebo skonÄŤĂ­.
    /// MainWindow ho pouĹľĂ­vĂˇ pro pĹ™epĂ­nĂˇnĂ­ tlaÄŤĂ­tka mezi "Start TUI" a "Stop TUI".
    /// </summary>
    public event Action<bool>? TuiStateChanged;

    /// <summary>
    /// True pokud TUI proces aktuĂˇlnÄ› bÄ›ĹľĂ­.
    /// </summary>
    public bool IsTuiRunning => _conpty != null && _conpty.IsRunning;

    /// <summary>
    /// HTML strĂˇnka s lokĂˇlnĂ­m xterm.js + bridge na C#.
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

        using var reader = new StreamReader(resource.Stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static string BuildTerminalHtmlFromResources()
    {
        var css = LoadTerminalResource("Resources/Terminal/xterm.min.css");
        var js = LoadTerminalResource("Resources/Terminal/xterm.min.js")
            .Replace("</script>", "<\\/script>", StringComparison.OrdinalIgnoreCase);
        return BuildTerminalHtml(js, css);
    }

    public TerminalControl()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeWebView();
        Unloaded += (_, _) => Cleanup();

        // Flush timer â€” kaĹľdĂ˝ch 10ms poĹˇle nashromĂˇĹľdÄ›nĂ˝ buffer do xterm.js
        // TĂ­m sluÄŤujeme rychle pĹ™Ă­chozĂ­ output do dĂˇvek mĂ­sto zprĂˇvy-na-znak.
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
        {
            // PostWebMessageAsString je rychlejĹˇĂ­ neĹľ PostWebMessageAsJson
            // (ĹľĂˇdnĂ˝ JSON parsing na JS stranÄ›). String pĹ™Ă­jde jako event.data string.
            WebView.CoreWebView2.PostWebMessageAsString(toSend);
        }
    }


    private static string GetWebViewUserDataFolder()
    {
        var processFolder = Process.GetCurrentProcess().Id.ToString();
        var primary = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenClawManager",
            "WebView2",
            processFolder);

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
        catch
        {
            return false;
        }
    }

    private async Task InitializeWebView()
    {
        try
        {
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(500);

            var userDataFolder = GetWebViewUserDataFolder();

            StatusText.Text = "Inicializuji WebView2...";

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            await WebView.EnsureCoreWebView2Async(env);

            // Bridge: messages from xterm.js (input and ready signal).
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

                var terminalReadyJson = await WebView.CoreWebView2.ExecuteScriptAsync(
                    "Boolean(window.Terminal && document.querySelector('.xterm'))");

                if (string.Equals(terminalReadyJson, "true", StringComparison.OrdinalIgnoreCase))
                {
                    _webViewReady = true;
                    _webViewFailed = false;
                    _webViewReadyTimeoutTimer?.Stop();
                    _webViewReadyTimeoutTimer = null;
                    StatusText.Text = L10n.IsCzech
                        ? "Klikni na \"OpenClaw TUI\" pro spusteni."
                        : "Click \"OpenClaw TUI\" to start.";
                }
                else
                {
                    _webViewFailed = true;
                    StatusText.Text = L10n.IsCzech
                        ? "Terminal se nenacetl. Lokalni xterm.js se nespustil."
                        : "Terminal did not load. Local xterm.js did not start.";
                }
            };

            StatusText.Text = L10n.IsCzech ? "Nacitam lokalni xterm.js..." : "Loading local xterm.js...";
            WebView.NavigateToString(BuildTerminalHtmlFromResources());
            _webViewReadyTimeoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _webViewReadyTimeoutTimer.Tick += (_, _) =>
            {
                _webViewReadyTimeoutTimer?.Stop();
                _webViewReadyTimeoutTimer = null;
                if (_webViewReady) return;

                _webViewFailed = true;
                StatusText.Text = L10n.IsCzech ? "TerminĂˇl se nenaÄŤetl. ChybĂ­ lokĂˇlnĂ­ xterm.js assety." : "Terminal did not load. Local xterm.js assets are missing.";
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
    /// ZprĂˇvy z xterm.js pĹ™es JavaScript bridge.
    /// </summary>
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
                    _webViewReady = true;
                    _webViewFailed = false;
                    _webViewReadyTimeoutTimer?.Stop();
                    _webViewReadyTimeoutTimer = null;
                    break;

                case "input":
                    var data = doc.RootElement.GetProperty("data").GetString();
                    _conpty?.WriteInput(data ?? "");
                    break;
            }
        }
        catch
        {
            // Ĺ patnÄ› formĂˇtovanĂˇ zprĂˇva â€” ignorovat
        }
    }

    /// <summary>
    /// SpustĂ­ openclaw tui v ConPTY a propojĂ­ s xterm.js.
    /// </summary>
    public void StartTui()
    {
        if (_conpty != null && _conpty.IsRunning)
            return;

        if (!_webViewReady)
        {
            StatusText.Text = _webViewFailed
                ? (L10n.IsCzech ? "TerminĂˇl nenĂ­ pĹ™ipraven. ChybĂ­ lokĂˇlnĂ­ xterm.js assety." : "Terminal is not ready. Local xterm.js assets are missing.")
                : (L10n.IsCzech ? "TerminĂˇl se jeĹˇtÄ› naÄŤĂ­tĂˇ. Zkus to prosĂ­m za chvĂ­li." : "Terminal is still loading. Please try again shortly.");
            return;
        }

        try
        {
            // VyÄŤistit terminĂˇl pĹ™ed novĂ˝m spuĹˇtÄ›nĂ­m (smaze pĹ™edchozĂ­ vĂ˝stup)
            WebView.CoreWebView2.PostWebMessageAsJson("{\"type\":\"clear\"}");

            _conpty = new ConPtyProcess();
            _conpty.OutputReceived += OnConPtyOutput;
            _conpty.ProcessExited += OnConPtyExited;

            // openclaw je .cmd skript, takĹľe ho musĂ­me spouĹˇtÄ›t pĹ™es cmd.exe /c
            _conpty.Start(GatewayService.BuildCmdExeCommand("tui"));

            // Schovat splash a zobrazit WebView â€” TUI startuje
            SplashBorder.Visibility = Visibility.Collapsed;
            WebView.Visibility = Visibility.Visible;

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
        if (_conpty != null)
        {
            _conpty.Dispose();
            _conpty = null;

            // Zobrazit splash znovu
            StatusText.Text = "TUI ukonÄŤeno. Klikni na â€žOpenClaw TUI\" pro novĂ˝ start.";
            SplashBorder.Visibility = Visibility.Visible;

            TuiStateChanged?.Invoke(false);
        }
    }

    private void OnConPtyOutput(string data)
    {
        // MĂ­sto okamĹľitĂ©ho posĂ­lĂˇnĂ­ pĹ™es WebView2 (pomalĂ©)
        // sbĂ­rĂˇme do bufferu â€” DispatcherTimer flushne kaĹľdĂ˝ch 10ms.
        lock (_bufferLock)
        {
            _outputBuffer.Append(data);
        }
    }

    private void OnConPtyExited()
    {
        lock (_bufferLock)
        {
            _outputBuffer.Append("\r\n\x1b[90m[Proces ukonÄŤen]\x1b[0m\r\n");
        }

        // Notifikovat MainWindow Ĺľe TUI skonÄŤilo + zobrazit splash
        Dispatcher.BeginInvoke(new Action(() =>
        {
            StatusText.Text = "TUI ukonÄŤeno. Klikni na â€žOpenClaw TUI\" pro novĂ˝ start.";
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

