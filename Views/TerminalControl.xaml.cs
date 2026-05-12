using System.IO;
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
/// [openclaw tui proces] ←→ [ConPtyProcess] ←→ [WebView2 PostWebMessage] ←→ [xterm.js]
///
/// Stdout z ConPTY → xterm.js přes term.write()
/// Stdin do ConPTY ← xterm.js přes onData event + window.chrome.webview.postMessage()
///
/// Fixní velikost 120×40 (žádný resize ve v0.3).
/// </summary>
public partial class TerminalControl : UserControl
{
    private ConPtyProcess? _conpty;
    private bool _webViewReady = false;

    // Buffering optimalizace — sbíráme output z ConPTY do bufferu
    // a flushujeme do xterm.js dávkově (eliminuje per-keystroke lag).
    private readonly System.Text.StringBuilder _outputBuffer = new();
    private readonly object _bufferLock = new();
    private DispatcherTimer? _flushTimer;

    /// <summary>
    /// Vyvolá se když TUI proces nastartuje nebo skončí.
    /// MainWindow ho používá pro přepínání tlačítka mezi "Start TUI" a "Stop TUI".
    /// </summary>
    public event Action<bool>? TuiStateChanged;

    /// <summary>
    /// True pokud TUI proces aktuálně běží.
    /// </summary>
    public bool IsTuiRunning => _conpty != null && _conpty.IsRunning;

    /// <summary>
    /// HTML stránka s xterm.js + bridge na C#.
    ///
    /// Bridge funguje takto:
    /// - Příchozí z C#: WebView2 PostWebMessageAsString → 'message' event → term.write(data)
    /// - Odchozí do C#: term.onData → window.chrome.webview.postMessage(data)
    /// </summary>
    private const string TerminalHtml = """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/xterm@5.3.0/css/xterm.min.css" />
            <script src="https://cdn.jsdelivr.net/npm/xterm@5.3.0/lib/xterm.min.js"></script>
            <style>
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
                /* Skrýt nativní scrollbar — xterm.js má vlastní viewport */
                ::-webkit-scrollbar { display: none; }
                /* Hladký xterm.js scrollbar */
                .xterm-viewport::-webkit-scrollbar { display: none; }
                .xterm-viewport { scrollbar-width: none; }
            </style>
        </head>
        <body>
            <div id="terminal"></div>
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
                term.writeln('\x1b[90mČeká se na ConPTY...\x1b[0m');

                // === Bridge: C# → xterm.js ===
                // PostWebMessageAsString posílá raw string (output z procesu)
                // PostWebMessageAsJson posílá JSON objekty (control zprávy)
                window.chrome.webview.addEventListener('message', (event) => {
                    if (typeof event.data === 'string') {
                        // Raw output z procesu — nejrychlejší cesta
                        term.write(event.data);
                        // Po každém output skoč na bottom (kde je prompt)
                        term.scrollToBottom();
                    } else if (event.data && event.data.type === 'clear') {
                        term.clear();
                    }
                });

                // === Bridge: xterm.js → C# (uživatel píše) ===
                term.onData(data => {
                    // Když uživatel píše, automaticky scrolluj na bottom
                    // (jinak se prompt schová pokud user scrolloval nahoru)
                    term.scrollToBottom();
                    window.chrome.webview.postMessage({ type: 'input', data: data });
                });

                term.focus();

                // Signál C#-koře že xterm.js je připraven
                window.chrome.webview.postMessage({ type: 'ready' });
            </script>
        </body>
        </html>
        """;

    public TerminalControl()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitializeWebView();
        Unloaded += (_, _) => Cleanup();

        // Flush timer — každých 10ms pošle nashromážděný buffer do xterm.js
        // Tím slučujeme rychle příchozí output do dávek místo zprávy-na-znak.
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
            // PostWebMessageAsString je rychlejší než PostWebMessageAsJson
            // (žádný JSON parsing na JS straně). String příjde jako event.data string.
            WebView.CoreWebView2.PostWebMessageAsString(toSend);
        }
    }

    private async Task InitializeWebView()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenClawManager",
                "WebView2");

            Directory.CreateDirectory(userDataFolder);

            StatusText.Text = "Inicializuji WebView2...";

            var env = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            await WebView.EnsureCoreWebView2Async(env);

            // Bridge: zprávy z xterm.js (input od uživatele, ready signal)
            WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            StatusText.Text = "Načítám xterm.js...";
            WebView.NavigateToString(TerminalHtml);

            WebView.CoreWebView2.NavigationCompleted += (_, e) =>
            {
                if (e.IsSuccess)
                {
                    // WebView zůstává Hidden — SplashBorder je viditelný.
                    // WebView se zobrazí až v StartTui() kdy splash schováme.
                    StatusText.Text = "Klikni na „OpenClaw TUI\" pro spuštění.";
                }
                else
                {
                    StatusText.Text = $"Chyba načtení: {e.WebErrorStatus}";
                }
            };
        }
        catch (Exception ex)
        {
            StatusText.Text = $"WebView2 chyba: {ex.Message}";
        }
    }

    /// <summary>
    /// Zprávy z xterm.js přes JavaScript bridge.
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
                    break;

                case "input":
                    var data = doc.RootElement.GetProperty("data").GetString();
                    _conpty?.WriteInput(data ?? "");
                    break;
            }
        }
        catch
        {
            // Špatně formátovaná zpráva — ignorovat
        }
    }

    /// <summary>
    /// Spustí openclaw tui v ConPTY a propojí s xterm.js.
    /// </summary>
    public void StartTui()
    {
        if (_conpty != null && _conpty.IsRunning)
            return;

        if (!_webViewReady)
        {
            // WebView ještě není připraven — zkusíme za chvíli
            Dispatcher.BeginInvoke(new Action(() => StartTui()),
                System.Windows.Threading.DispatcherPriority.Background);
            return;
        }

        try
        {
            var openclawCmd = SettingsService.Current.OpenClawCommand;

            // Vyčistit terminál před novým spuštěním (smaze předchozí výstup)
            WebView.CoreWebView2.PostWebMessageAsJson("{\"type\":\"clear\"}");

            _conpty = new ConPtyProcess();
            _conpty.OutputReceived += OnConPtyOutput;
            _conpty.ProcessExited += OnConPtyExited;

            // openclaw je .cmd skript, takže ho musíme spouštět přes cmd.exe /c
            var cmdLine = $"cmd.exe /c \"\"{openclawCmd}\" tui\"";
            _conpty.Start(cmdLine);

            // Schovat splash a zobrazit WebView — TUI startuje
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
            StatusText.Text = "TUI ukončeno. Klikni na „OpenClaw TUI\" pro nový start.";
            SplashBorder.Visibility = Visibility.Visible;

            TuiStateChanged?.Invoke(false);
        }
    }

    private void OnConPtyOutput(string data)
    {
        // Místo okamžitého posílání přes WebView2 (pomalé)
        // sbíráme do bufferu — DispatcherTimer flushne každých 10ms.
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

        // Notifikovat MainWindow že TUI skončilo + zobrazit splash
        Dispatcher.BeginInvoke(new Action(() =>
        {
            StatusText.Text = "TUI ukončeno. Klikni na „OpenClaw TUI\" pro nový start.";
            SplashBorder.Visibility = Visibility.Visible;
            TuiStateChanged?.Invoke(false);
        }));
    }

    private void Cleanup()
    {
        _flushTimer?.Stop();
        _flushTimer = null;
        _conpty?.Dispose();
        _conpty = null;
    }
}
