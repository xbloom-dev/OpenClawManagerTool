# Dev Manual — OpenClaw Manager Tool by Bloom

**Verze:** 3.0 (aktualizováno pro v0.99)
**Prostředí:** Windows 11, .NET 8 SDK, VS Code, Git.

---

## Obsah

1. [Stav prostředí](#1-stav-prostředí)
2. [Vytvoření projektu](#2-vytvoření-projektu)
3. [První spuštění](#3-první-spuštění)
4. [Inicializace Git repository](#4-inicializace-git-repository)
5. [Struktura projektu](#5-struktura-projektu)
6. [Otevření projektu v VS Code](#6-otevření-projektu-v-vs-code)
7. [Hlavní okno — layout](#7-hlavní-okno--layout)
8. [Status bar — metriky (async)](#8-status-bar--metriky-async)
9. [Detekce Gateway](#9-detekce-gateway)
10. [Tlačítka Gateway](#10-tlačítka-gateway)
11. [Měření latencí](#11-měření-latencí)
12. [SPUSTIT TUI sekvence](#12-spustit-tui-sekvence)
13. [Settings okno](#13-settings-okno)
14. [Cleaning Tool](#14-cleaning-tool)
15. [Lokalizace EN/CS](#15-lokalizace-encs)
16. [Embedded TUI (ConPTY + WebView2)](#16-embedded-tui-conpty--webview2)
17. [Splash screen architektura](#17-splash-screen-architektura)
18. [Theme systém](#18-theme-systém)
19. [Token Manager](#19-token-manager)
20. [Gateway Log + Živý log](#20-gateway-log--živý-log)
21. [Build a publish](#21-build-a-publish)
22. [Git workflow](#22-git-workflow)

---

## 1. Stav prostředí

**Ověřeno (13.5.2026):**
- ✅ .NET 8 SDK 8.0.420
- ✅ VS Code 1.118.1 + C# Dev Kit
- ✅ Git 2.54.0
- ✅ WebView2 Runtime (součást Windows 11)

**Pozor — antivirus:**
Přidej výjimku pro celou složku projektu `E:\OpenClaw\OpenClawManager\`.
Norton během buildu maže `.dll` soubory — vede k "Access denied" chybě.

V Nortonu: Settings → Antivirus → Scans and Risks → Items to Exclude → Add Folder → `E:\OpenClaw\OpenClawManager\` → Include Subfolders. Totéž v Auto-Protect, SONAR, Download Intelligence.

---

## 2. Vytvoření projektu

```powershell
cd E:\OpenClaw\OpenClawManager
dotnet new wpf -f net8.0
```

---

## 3. První spuštění

```powershell
dotnet run
```

První build ~30s. Pokud PowerShell "mlčí" po zavření okna — Ctrl+C.

---

## 4. Inicializace Git repository

```powershell
cd E:\OpenClaw\OpenClawManager
git init
@"
bin/
obj/
.vs/
*.user
*.suo
"@ | Out-File -FilePath .gitignore -Encoding UTF8
git add .
git commit -m "Initial WPF project skeleton"
```

---

## 5. Struktura projektu

```
OpenClawManager/
├── App.xaml / App.xaml.cs              ← entry point: L10n, ThemeService, favicon
├── MainWindow.xaml / .cs               ← hlavní okno
├── MainWindow.Splash.cs                ← partial: splash screen logika
├── MainWindow.Theme.cs                 ← partial: theme switching + ikony
├── OpenClawManager.csproj              ← verze 0.99, ikona, WebView2, resources
├── Views/
│   ├── TerminalControl.xaml / .cs      ← embedded TUI (WebView2 + ConPTY)
│   ├── CleaningWindow.xaml / .cs       ← Cleaning Tool (7 kroků)
│   ├── SettingsWindow.xaml / .cs       ← cesty, jazyk, theme, vault path
│   ├── AboutWindow.xaml / .cs          ← SVG logo + zkratky
│   ├── GatewayLogWindow.xaml / .cs     ← log viewer + Kopírovat + Živá data
│   ├── LiveLogWindow.xaml / .cs        ← FileSystemWatcher live log
│   ├── StopGatewayDialog.xaml / .cs    ← potvrzovací dialog pro Stop
│   ├── TokenManagerWindow.xaml / .cs   ← Token Manager hlavní okno
│   ├── TokenEditWindow.xaml / .cs      ← Add/Edit token dialog
│   └── TokenImportWindow.xaml / .cs    ← Import tokenů ze souboru
├── Services/
│   ├── GatewayService.cs               ← Start/Stop/Restart + command validation
│   ├── ProcessDetector.cs              ← WMI detekce node.exe Gateway
│   ├── LogMonitor.cs                   ← polling log, ANSI strip, latency parse
│   ├── LatencyTracker.cs               ← sliding avg 10×, _lastMs O(1), offset
│   ├── ResourceMonitor.cs              ← RAM/VRAM/CPU async (MeasureAsync)
│   ├── CleanupService.cs               ← logika 7 kroků čištění
│   ├── SettingsService.cs              ← load/save AppSettings JSON
│   ├── AdminService.cs                 ← kontrola admin práv
│   ├── ScheduledTaskService.cs         ← enable/disable Windows Task
│   ├── ConPtyProcess.cs                ← P/Invoke ConPTY wrapper
│   ├── L10n.cs                         ← ResourceDictionary přepínání + IsCzech
│   ├── ThemeService.cs                 ← Apply() + ThemeChanged event + GetIconUri()
│   └── TokenService.cs                 ← vault CRUD, DPAPI encrypt/decrypt
├── Models/
│   ├── AppSettings.cs                  ← SchemaVersion, Language, Theme, cesty
│   ├── AppTheme.cs                     ← enum Legacy / Modern
│   ├── TokenEntry.cs                   ← Id, Value, Description, CreatedAt
│   ├── TokenVault.cs                   ← Version, Tokens list
│   ├── GatewayState.cs
│   ├── CleanupResult.cs
│   └── LatencyStats.cs                 ← record (LastMs, AvgMs, MaxMs, Count)
├── Resources/
│   ├── app-icon.ico                    ← ikona EXE
│   ├── app-favicon.ico                 ← ikona všech oken (EventManager v App.xaml.cs)
│   ├── app-logo.svg                    ← CopyToOutputDirectory: PreserveNewest
│   ├── splash.png                      ← Modern theme splash obrázek
│   ├── splash.mp4                      ← Modern theme splash video (volitelné)
│   ├── Terminal/
│   │   ├── xterm.min.js                ← lokální xterm.js (žádný CDN)
│   │   └── xterm.min.css
│   ├── Icons/Modern/                   ← PNG ikony pro Modern theme
│   ├── Themes/
│   │   ├── Theme.Legacy.xaml
│   │   └── Theme.Modern.xaml
│   └── Lang/
│       ├── Strings.cs.xaml             ← české texty
│       └── Strings.en.xaml             ← anglické texty
└── TokenService.Tests/
    ├── Program.cs                      ← round-trip DPAPI + audit log testy
    └── TokenService.Tests.csproj
```

---

## 6. Otevření projektu v VS Code

```powershell
cd E:\OpenClaw\OpenClawManager
code .
```

Dialog "Do you trust..." → Yes.

---

## 7. Hlavní okno — layout

`DockPanel` root → `Menu` (Top) → `StatusBar` (Bottom) → `Grid` 3 sloupce (330px / 5px splitter / *).

**Levý panel:** `ScrollViewer` + `StackPanel` → 3 `GroupBox`y:
- `GrpActions` — TUI tlačítko + Gateway Start/Stop/Restart + Otevřít + Nástroje + Údržba
- `GrpLatency` — 4 řádky (Poslední/Průměr/Maximum/Requestů)
- `GrpAppLog` — `ListBox` s posledními 100 záznamy

**Pravý panel:** `TerminalControl` (UserControl) + `SplashOverlay` (Border přes celý pravý panel)

---

## 8. Status bar — metriky (async)

`Services/ResourceMonitor.cs` — statická třída s async entry-pointem:

```csharp
// UI timer → neblokující
var snap = await ResourceMonitor.MeasureAsync();
```

`MeasureAsync()` spustí `Measure()` na thread pool přes `Task.Run()`. UI thread není blokován.

**Cache:** RAM a CPU jsou cachované na 4s (WMI dotazy trvají 50–200ms). VRAM se měří každé 2s (nvidia-smi je rychlý).

**nvidia-smi timeout:** `proc.WaitForExit(2000)` + `proc.Kill(entireProcessTree: true)` pokud timeout vyprší — zabrání zombie procesu.

**MainWindow timer:**
```csharp
private async void StatusTimer_Tick(object? sender, EventArgs e)
    => await UpdateStatusAsync();

private async Task UpdateStatusAsync()
{
    var snap = await ResourceMonitor.MeasureAsync();
    StatusRam.Text = ...;
    StatusCpu.Text = ...;
}
```

---

## 9. Detekce Gateway

`Services/ProcessDetector.cs`:
```csharp
// WMI: node.exe kde CommandLine obsahuje "openclaw" i "gateway"
var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='node.exe'";
```

`FindGatewayProcess()` → `Process?`, `IsGatewayRunning()` → `bool`.

---

## 10. Tlačítka Gateway

`Services/GatewayService.cs`:

**Start:** `powershell.exe -NoExit -NoProfile -Command "& 'openclaw' gateway"` — viditelné okno.

**Validace příkazu:** `TryValidateOpenClawCommand()` — zakázané znaky `'"\;&|`\r\n`. `EscapePowerShellSingleQuotedString()` escapuje apostrof (`'` → `''`).

**Stop:**
1. `Process.Kill(entireProcessTree: true)` na Gateway node.exe
2. Zabije PowerShell wrapper okna
3. **NEDOTÝKÁ SE Scheduled Tasku**

---

## 11. Měření latencí

`Services/LogMonitor.cs` + `Services/LatencyTracker.cs`

**Pipeline:**
1. `FileStream` s `FileShare.ReadWrite` — čte pouze nové řádky od `_fileOffset`
2. `JsonDocument.Parse()` → extrahuje `message` field
3. Regex strip ANSI: `\x1b\[[0-9;]*[mGKHFJA-Za-z]`
4. Regex latence: `res\s+✓\s+([\w\.]+)\s+(\d+)ms`

**LatencyTracker optimalizace:**
- `_lastMs` field — O(1) přístup k poslední hodnotě (místo `Queue.Last()` které je O(n))
- `Poll(logPath)` — čte nové záznamy, volat z UI timeru každé 2s
- `Reset()` — volat při restartu Gateway

---

## 12. SPUSTIT TUI sekvence

`MainWindow.xaml.cs` — `BtnStartTui_Click()`:

```
DisposeSplash()          ← skryje splash overlay, ShowWebView()
TUI běží?  → StopTui()
Gateway běží? → StartTui() přímo
Jinak:
  1. DeleteLogIfExists(logPath)
  2. LatencyTracker.Reset()
  3. GatewayService.Stop() pokud běží + await 2s
  4. GatewayService.Start()
  5. SetGatewayUiState(Starting)
  6. LogMonitor.WaitForGatewayReady(180s) → async
  7. SetGatewayUiState(Running)
  8. Terminal.StartTui()
```

---

## 13. Settings okno

`Views/SettingsWindow.xaml/.cs`

**Sekce:**
1. Jazyk / Language — `RbLangCS` / `RbLangEN`
2. Cesty — OpenClaw složka, Temp složka, openclaw příkaz, PowerShell adresář, Token Manager vault
3. Theme — `RbThemeLegacy` / `RbThemeModern`

**Validace při uložení:**
```csharp
if (!GatewayService.TryValidateOpenClawCommand(cmd, out var err))
    // zobrazit chybu, neukládat
```

---

## 14. Cleaning Tool

`Views/CleaningWindow.xaml/.cs` + `Services/CleanupService.cs`

7 checkboxů + slider (Keep Sessions, výchozí 10) + log panel.

| Krok | Výchozí | Popis |
|---|---|---|
| 1 | ✅ | Staré Gateway logy |
| 2 | ✅ | Zálohy konfigurace (ponechá 2) |
| 3 | ❌ | Stability logy starší 3 dny |
| 4 | ✅ | Browser cache starší 1 den |
| 5 | ✅ | Session locky |
| 6 | ✅ | sessions.json (ponechá N nejnovějších) |
| 7 | ❌ | Token Manager *.bak (opt-in) |

Krok 7 (`CleanTokenManagerBackups`) maže `*.bak` soubory v Token Manager složce. Záměrně výchozí vypnuto.

---

## 15. Lokalizace EN/CS

`Services/L10n.cs` — statická třída:

```csharp
L10n.Apply(Language.CS);
string s = L10n.Get("Str_BtnCleaningTool");
bool cs = L10n.IsCzech; // helper property, O(1)
```

Token Manager okna používají helper `T(cs, en)`:
```csharp
private bool Cs => L10n.IsCzech;
private string T(string cs, string en) => Cs ? cs : en;
```

---

## 16. Embedded TUI (ConPTY + WebView2)

`Views/TerminalControl.xaml/.cs`

**WebView2 viditelnost:**
- Výchozí stav: `Visibility="Hidden"` (ne Collapsed — WebView2 potřebuje HWND pro init)
- `OnWebViewReady()` — nenastavuje Visible; pouze aktualizuje StatusText
- `ShowWebView()` — volá MainWindow po skrytí SplashOverlay; teprve pak se WebView zobrazí
- Důvod: WebView2 HWND (Win32 okno) překryje jakýkoli WPF content jakmile je Visible — musí se zobrazit až po skrytí splash videa

**Veřejné API:**
```csharp
public void StartTui()         // spustí ConPTY + skryje SplashBorder
public void StopTui()          // zastaví ConPTY + zobrazí SplashBorder
public void HideSplashBorder() // volá MainWindow v Modern theme
public void ShowSplashBorder() // volá MainWindow při přepnutí Legacy→Modern
public void ShowWebView()      // volá MainWindow po DisposeSplash()
public bool IsTuiRunning       // property
public event Action<bool>? TuiStateChanged
```

**xterm.js:** lokální soubory z `Resources/Terminal/xterm.min.js` + `.css` načtené přes `Application.GetResourceStream()`. Žádný CDN.

**Buffering:** stdout z ConPTY se sbírá do `_outputBuffer` (lock) a `DispatcherTimer` každých 10ms flushuje dávkově do xterm.js přes `PostWebMessageAsString`.

**Unicode v JS:** všechny non-ASCII znaky v JS heredoc jako `\uXXXX` escape (např. `\u010cek\u00e1` = Čeká).

---

## 17. Splash screen architektura

Splash má dvě nezávislé vrstvy:

**SplashBorder** (v `TerminalControl.xaml`) — ASCII art pro Legacy theme. `Visibility="Visible"` při startu, překrývá WebView dokud uživatel neklikne na TUI.

**SplashOverlay** (v `MainWindow.xaml`) — video/PNG pro Modern theme. `Border` přes celý pravý panel s `Grid.Column="2"`.

**Pravidlo oddělení:** SplashBorder patří Legacy, SplashOverlay patří Modern. V Modern theme se `SplashBorder` schová voláním `Terminal.HideSplashBorder()` z `InitSplash()`.

**Tok v Modern theme:**
```
App start → InitSplash()
  → SplashOverlay.Visible
  → Terminal.HideSplashBorder()
  → StartSplashVideo(mp4) nebo PNG fallback

Video doběhne → SplashMedia_MediaEnded()
  → StopSplashVideo()       ← Volume=0, Stop(), Source=null
  → SplashMedia.Collapsed   ← video schovat
  → SplashImage zůstane     ← PNG freeze frame

Uživatel klikne TUI → BtnStartTui_Click()
  → DisposeSplash()
      → StopSplashVideo()
      → SplashOverlay.Collapsed
      → Terminal.HideSplashBorder()
      → Terminal.ShowWebView()   ← teprve nyní WebView Visible
  → Terminal.StartTui()
```

**Tok v Legacy theme:**
```
App start → InitSplash()
  → SplashOverlay.Collapsed  ← žádné video/PNG
  → SplashBorder zůstane Visible (ASCII art)

Uživatel klikne TUI → DisposeSplash()
  → Terminal.HideSplashBorder()
  → Terminal.ShowWebView()
  → Terminal.StartTui()
```

**Přepnutí tématu za běhu** (`OnThemeChanged` v `MainWindow.Theme.cs`):
- Modern → Legacy: `StopSplashVideo()`, `SplashOverlay.Collapsed`, pokud TUI neběží → `ShowSplashBorder()`
- Legacy → Modern: `HideSplashBorder()`, `SplashOverlay.Visible` pokud TUI neběží

---

## 18. Theme systém

`Services/ThemeService.cs` — statická třída:

```csharp
ThemeService.Apply(AppTheme.Modern);    // přepne ResourceDictionary
ThemeService.ThemeChanged += handler;   // event při změně
Uri? uri = ThemeService.GetIconUri(AppTheme.Modern, "start"); // PNG ikona
```

`_themeResourcePaths` dictionary — cesty k XAML souborům témat.
`_iconFolderNames` dictionary — složky PNG ikon.

**MainWindow.Theme.cs** — partial class:
- `InitTheme()` — subscribe na `ThemeChanged`, aplikovat při startu
- `ApplyModernUi()` — `SetButtonIcon()` pro každé tlačítko
- `ApplyLegacyUi()` — `RestoreButtonLegacy()` vrátí emoji TextBlock
- `SetButtonIcon()` — nahradí první child (emoji TextBlock) za `Image` s PNG
- `RestoreButtonLegacy()` — opačná operace

---

## 19. Token Manager

`Services/TokenService.cs` + `Models/TokenEntry.cs` + `Models/TokenVault.cs`

### DPAPI šifrování

```csharp
// Entropy = SHA256 z konstantního stringu — cross-app protection
private static readonly byte[] DpapiEntropy =
    SHA256.HashData(Encoding.UTF8.GetBytes("OpenClawManager.TokenVault.v1"));

// P/Invoke CryptProtectData / CryptUnprotectData
private static byte[] CryptProtect(byte[] data)   => CryptData(data, protect: true);
private static byte[] CryptUnprotect(byte[] data) => CryptData(data, protect: false);
```

Vault verze: `TokenVault.Version = 1`. Validace: `vault.Version > CurrentVaultVersion` (forward-only tolerance).

Migrace plaintext → DPAPI: `LoadVault()` detekuje nešifrovaný vault a po prvním `SaveVault()` ho přepíše šifrovanou verzí.

### Vault API

```csharp
TokenVault LoadVault(string path)
void SaveVault(TokenVault vault, string path)
TokenFileOperationResult RedactFile(string path, IEnumerable<TokenEntry> tokens)
TokenFileOperationResult RestoreFile(string path, IEnumerable<TokenEntry> tokens)
VerifyResult VerifyFile(string path, IEnumerable<TokenEntry> tokens)
bool IsVaultTrackedByGit(string vaultPath)  // s cache v TokenManagerWindow
```

### TokenManagerWindow

- `SettingsService.SettingsChanged` subscription — obnoví vault path při změně nastavení
- `_isTrackedByGit` cache — `IsVaultTrackedByGit()` se volá jen při `LoadTokens()`, ne při každé akci
- `VaultWarningBanner` — červený Border viditelný pokud vault leží v rizikovém umístění

### Testy

```powershell
dotnet run --project TokenService.Tests\TokenService.Tests.csproj
```

Pokrývá: vault CRUD, DPAPI round-trip, audit log neobsahuje hodnoty.

---

## 20. Gateway Log + Živý log

### GatewayLogWindow

Konstruktor: `GatewayLogWindow(string logPath, int defaultLines = 20)`

Kopírovat — `Clipboard.SetText()` + `DoubleAnimation` fade 200ms, zobrazení 1.8s.

### LiveLogWindow

Non-modální (`Show()`). `FileSystemWatcher` → `OnLogChanged` → `Dispatcher.BeginInvoke(ReadNewLines)`.

**Timer oprava:** `_highlightClearTimer` je persistent instance field — Stop + re-subscribe před každým restartem. Eliminuje vytváření nového timeru pro každý nový řádek (původní verze způsobovala GC tlak při rychlém logování).

```csharp
_highlightClearTimer ??= new DispatcherTimer { ... };
_highlightClearTimer.Stop();
_highlightClearTimer.Tick -= HighlightClearTimer_Tick;
_highlightClearTimer.Tick += HighlightClearTimer_Tick;
_highlightClearTimer.Start();
```

---

## 21. Build a publish

```powershell
# Testy
.\run-tests.ps1

# Debug build
dotnet build

# Release
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
# Output: bin\Release\net8.0\win-x64\publish\OpenClawManager.exe (~70 MB)
```

**csproj klíčové položky:**
```xml
<AssemblyVersion>0.99.0.0</AssemblyVersion>
<Version>0.99.0</Version>
<ApplicationIcon>Resources\app-icon.ico</ApplicationIcon>
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2792.45" />
<PackageReference Include="System.Management" Version="8.0.0" />
```

**Smoke test checklist:**
1. Start app → status bar `v0.99`
2. Start Gateway → TUI → ověřit terminál
3. Gateway log + Live Log
4. Cleaning Tool → Náhled (preview)
5. Token Manager → init vault → add → redact → verify → restore
6. Nastavení → změna jazyka → přepnutí tématu
7. O aplikaci (F1)
8. Offline test (Wi-Fi off → terminál musí naběhnout)

---

## 22. Git workflow

```powershell
# Průběžný commit
git add .
git commit -m "popis změny"

# Tag verze
git tag -a v0.99 -m "v0.99 - pre-release"
git log --oneline
git tag
```

**Commit message konvence:**
```
fix: popis opravy
feat: popis nové funkce
perf: výkonnostní optimalizace
docs: změna dokumentace
polish: UI/UX vylepšení
```

---

**Konec dokumentu v3.0**
