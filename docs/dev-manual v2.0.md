# Dev Manual — OpenClaw Manager Tool by Bloom

**Verze:** 2.0 (aktualizováno pro stabilizační build v0.95)
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
8. [Status bar — metriky](#8-status-bar--metriky)
9. [Detekce Gateway](#9-detekce-gateway)
10. [Tlačítka Gateway](#10-tlačítka-gateway)
11. [Měření latencí](#11-měření-latencí)
12. [SPUSTIT TUI sekvence](#12-spustit-tui-sekvence)
13. [Settings okno](#13-settings-okno)
14. [Cleaning Tool](#14-cleaning-tool)
15. [Lokalizace EN/CS](#15-lokalizace-encs)
16. [Embedded TUI (ConPTY)](#16-embedded-tui-conpty)
17. [Gateway Log + Živý log](#17-gateway-log--živý-log)
18. [Build a publish](#18-build-a-publish)
19. [Git workflow](#19-git-workflow)

---

## 1. Stav prostředí

**Ověřeno (12.5.2026):**
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
├── App.xaml / App.xaml.cs           ← entry point, L10n.Apply() při startu
├── MainWindow.xaml / .cs            ← hlavní okno, ApplyLocalization()
├── OpenClawManager.csproj           ← verze, ikona, WebView2, resources
├── Views/
│   ├── TerminalControl.xaml / .cs   ← embedded TUI (WebView2 + ConPTY)
│   ├── CleaningWindow.xaml / .cs    ← Cleaning Tool (6 kroků)
│   ├── SettingsWindow.xaml / .cs    ← cesty, jazyk
│   ├── AboutWindow.xaml / .cs       ← SVG logo + zkratky
│   ├── GatewayLogWindow.xaml / .cs  ← log viewer + Kopírovat + Živá data
│   ├── LiveLogWindow.xaml / .cs     ← FileSystemWatcher live log
│   └── StopGatewayDialog.xaml / .cs ← potvrzovací dialog pro Stop
├── Services/
│   ├── GatewayService.cs            ← Start/Stop/Restart/DoctorFix
│   ├── ProcessDetector.cs           ← WMI detekce node.exe Gateway
│   ├── LogMonitor.cs                ← polling log, ANSI strip, latency parse
│   ├── LatencyTracker.cs            ← sliding avg 10×, file offset
│   ├── ResourceMonitor.cs           ← RAM/VRAM/CPU (WMI + nvidia-smi)
│   ├── CleanupService.cs            ← logika 6 kroků čištění
│   ├── SettingsService.cs           ← load/save AppSettings JSON
│   ├── AdminService.cs              ← kontrola admin práv
│   ├── ScheduledTaskService.cs      ← enable/disable Windows Task
│   ├── ConPtyProcess.cs             ← P/Invoke ConPTY wrapper
│   └── L10n.cs                      ← ResourceDictionary přepínání
├── Models/
│   ├── AppSettings.cs               ← cesty + Language ("CS"/"EN")
│   ├── GatewayState.cs
│   ├── CleanupResult.cs
│   └── LatencyStats.cs              ← record (LastMs, AvgMs, MaxMs, Count)
└── Resources/
    ├── app-icon.ico
    ├── app-logo.svg                 ← CopyToOutputDirectory: PreserveNewest
    └── Lang/
        ├── Strings.cs.xaml          ← české texty (Resource)
        └── Strings.en.xaml          ← anglické texty (Resource)
```

---

## 6. Otevření projektu v VS Code

```powershell
cd E:\OpenClaw\OpenClawManager
code .
```

Dialog "Do you trust..." → Yes. C# Dev Kit potřebuje Microsoft účet (zdarma, lze přeskočit).

---

## 7. Hlavní okno — layout

`DockPanel` root → `Menu` (Top) → `StatusBar` (Bottom) → `Grid` 3 sloupce (330px / 5px splitter / *).

**Levý panel:** `ScrollViewer` + `StackPanel` → 3 `GroupBox`y:
- `GrpActions` — TUI tlačítko + Gateway Start/Stop/Restart + Otevřít + Nástroje + Údržba
- `GrpLatency` — 4 řádky (Poslední/Průměr/Maximum/Requestů)
- `GrpAppLog` — `ListBox` s posledními 100 záznamy

**Pravý panel:** `TerminalControl` (vlastní UserControl)

**Pojmenované elementy (výběr):**
- `BtnStartTui`, `BtnStartTuiSymbol`, `BtnStartTuiLabel`, `BtnStartTuiSubLabel`
- `BtnGatewayStart/Stop/Restart`, `BtnGatewayStartLabel/StopLabel/RestartLabel`
- `TxtGatewayLabel`, `TxtSectionOpen/Tools/Maintenance`
- `BtnCleaningToolLabel`, `BtnTokenManagerLabel`, `BtnDoctorFixLabel`
- `LatencyLast/Avg/Max/Count`
- `TxtLatencyLast/Avg/Max/Count`
- `AppLog` (ListBox)
- `StatusGatewayDot`, `StatusGatewayText`, `StatusGatewayPid`, `StatusGatewayUptime`
- `StatusRam`, `StatusVram`, `StatusCpu`
- Všechna menu: `MnuMenuOpen`, `MnuOpenOpenClawFolder`, `MnuOpenLog10`... atd.

---

## 8. Status bar — metriky

`Services/ResourceMonitor.cs` — statická třída, `Measure()` vrací `ResourceSnapshot`:
- RAM: `Win32_OperatingSystem` (FreePhysicalMemory, TotalVisibleMemorySize)
- CPU: `Win32_Processor` LoadPercentage průměr přes jádra (locale-independent)
- VRAM: `nvidia-smi --query-gpu=memory.used,memory.total --format=csv,noheader,nounits` (nullable)

Timer 2s v `MainWindow` → `UpdateStatus()` → aktualizace TextBlocků.

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

**Start:** `powershell.exe -NoExit -NoProfile -Command "& openclaw gateway"` — viditelné okno.

**Stop:**
1. `Process.Kill(entireProcessTree: true)` na Gateway node.exe
2. Zabije PowerShell wrapper okna s `openclaw gateway` v command line
3. **NEDOTÝKÁ SE Scheduled Tasku** (zachovává autostart)
4. **NEVOLÁ `openclaw stop`** — příkaz neexistuje (vrátí "unknown command")

**Scheduled Task v Cleaning Tool:**
- `schtasks /Change /TN "OpenClaw Gateway" /DISABLE` před cleanupem
- `schtasks /Change /TN "OpenClaw Gateway" /ENABLE` po cleanupu
- Vyžaduje admin práva

---

## 11. Měření latencí

`Services/LogMonitor.cs` + `Services/LatencyTracker.cs`

**Formát logu:** Každý řádek je JSON objekt. Zprávy v poli `message` obsahují ANSI escape kódy.

**Pipeline:**
1. `FileStream` s `FileShare.ReadWrite` — čte pouze nové řádky od posledního offsetu
2. `JsonDocument.Parse()` → extrahuje `message` field
3. Regex strip ANSI: `\x1b\[[0-9;]*[mGKHFJA-Za-z]`
4. Regex latence: `res\s+✓\s+([\w\.]+)\s+(\d+)ms`

**Příklad reálného záznamu (po JSON parse, před ANSI strip):**
```
⇄ \u001b[1mres\u001b[22m ✓ \u001b[1magents.list\u001b[22m \u001b[2m145ms\u001b[22m ...
```

**LatencyTracker:**
- `Poll(logPath)` — čte nové záznamy, volat z UI timeru každé 2s
- `Reset()` — volat při restartu Gateway
- `GetStats()` → `LatencyStats(LastMs, AvgMs, MaxMs, Count)`
- Sliding window 10 hodnot, file offset tracking (reset pokud soubor smazán)

**Barvy v UI:** červená > 5000ms, zelená < 1000ms.

---

## 12. SPUSTIT TUI sekvence

`MainWindow.xaml.cs` — `BtnStartTui_Click()`:

```
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

**TUI tlačítko stavy** (`UpdateStartTuiButton(bool tuiRunning)`):
- TUI běží → červené `#FFD0D0`, symbol "■", text "Zastavit OpenClaw TUI"
- Gateway běží → zelené `#D0FFD0`, symbol "▶", subtext "(Gateway běží)"
- Nic neběží → zelené `#D0FFD0`, symbol "▶", subtext "(restart Gateway)"

Tooltip vždy popisuje všechny 3 režimy (lokalizovaný přes `Str_Tip_StartTui`).

---

## 13. Settings okno

`Views/SettingsWindow.xaml/.cs`

**Sekce:**
1. Jazyk / Language — `RbLangCS` / `RbLangEN` (RadioButton, GroupName="Language")
2. Cesty — 4 TextBoxy + Procházet... tlačítka (`OpenFolderDialog`)
3. Info — cesta k settings.json (`SettingsService.SettingsFilePath`)

**Uložení:**
```csharp
// SettingsService.Save() → %APPDATA%\OpenClawManager\settings.json
// Po SaveDialog == true v MainWindow:
L10n.Apply(lang);
ApplyLocalization();
```

**AppSettings.cs** má property `Language` (string `"CS"` nebo `"EN"`).

---

## 14. Cleaning Tool

`Views/CleaningWindow.xaml/.cs` + `Services/CleanupService.cs`

6 checkboxů + slider (Keep Sessions, výchozí 10) + log panel.

**Sessions JSON parser** (`Invoke-SessionsCleanup` přepsáno do C#):
- Akceptuje array i object strukturu
- Timestamp klíče: `createdAt`, `timestamp`, `updatedAt`, `lastModified`, `mtime`, `created_at`
- Formáty: ISO 8601, Unix epoch sec/ms
- Bez timestampu → řazeny jako nejstarší
- Záloha `.bak` před zápisem, rollback při chybě

**Integrovaná sekce "OpenClaw Gateway"** v CleaningWindow — 3 checkboxy pro akci s Gateway při cleanupu.

**Tlačítka:** Náhled (zelená `#D0FFD0`) / Spustit (červená `#FFD0D0`) / Zavřít.

---

## 15. Lokalizace EN/CS

`Services/L10n.cs` — statická třída:

```csharp
L10n.Apply(Language.CS);  // přepne ResourceDictionary v App.Resources
string s = L10n.Get("Str_BtnCleaningTool");  // "Vyčistit soubory"
```

**Soubory:**
- `Resources/Lang/Strings.cs.xaml` — čeština
- `Resources/Lang/Strings.en.xaml` — angličtina
- Obě jako `<Resource>` v csproj

**Klíče (výběr):**
```
Str_BtnStartTui_Label, Str_BtnStartTui_Stop
Str_BtnCleaningTool, Str_BtnTokenManager, Str_BtnDoctorFix
Str_Tip_StartTui (3-režimový popis)
Str_Menu_Open, Str_Menu_Settings, Str_Menu_Help ...
Str_Log_GatewayStarting, Str_Log_GatewayReady ...
Str_Status_Running, Str_Status_Stopped ...
Str_BtnCopy, Str_BtnCopied
```

**`ApplyLocalization()`** v MainWindow.xaml.cs aktualizuje:
- GroupBoxy headers, TextBlock labels, tlačítka content, tooltipy, menu headers, status bar texty

Každé okno má vlastní `ApplyLocalization()` nebo inline CS/EN podmínku.

**App.xaml.cs** — `OnStartup()` → `L10n.Apply()` podle `SettingsService.Current.Language`.

---

## 16. Embedded TUI (ConPTY)

`Views/TerminalControl.xaml/.cs` — WebView2 UserControl:
- HTML: xterm.js + custom JS bridge
- Backend: `ConPtyProcess.cs` — P/Invoke wrapper pro Windows ConPTY API
- Rozlišení: 120×35 znaků (fixní)
- `StartTui()` / `StopTui()` — public metody
- `TuiStateChanged` event → `MainWindow.UpdateStartTuiButton()`
- `IsTuiRunning` property

`WebView2` data uložena: `%LOCALAPPDATA%\OpenClawManager\WebView2`

---

## 17. Gateway Log + Živý log

### GatewayLogWindow

Konstruktor: `GatewayLogWindow(string logPath, int defaultLines = 20)`

**Nahoře:** Zobrazit: [dropdown] + [Aktualizovat] + [Kopírovat] + [✓ Zkopírováno fade]
**Dole:** StatusBar + [Živá data] + [Zavřít]

Kopírovat — `Clipboard.SetText()` + `DoubleAnimation` fade in/out 200ms, zobrazení 1.8s.

### LiveLogWindow

Konstruktor: `LiveLogWindow(string logPath, int windowSize = 20)`

Non-modální (`Show()` místo `ShowDialog()`). Otevření: GatewayLogWindow se zavře.

```csharp
// FileSystemWatcher
_watcher = new FileSystemWatcher(dir, file) {
    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
    EnableRaisingEvents = true
};
_watcher.Changed += OnLogChanged;
// OnLogChanged → Dispatcher.BeginInvoke(ReadNewLines)
```

**Sliding window:** `Queue<string>(_windowSize)` — Enqueue nový, Dequeue nejstarší když Count >= windowSize.

**Zvýraznění:** Nové řádky dostanou prefix `► `. `DispatcherTimer` po 2s obnoví čistý obsah.

**Kopírovat:** `string.Join("\n", _lines)` — čistý obsah bez `► ` prefixů.

---

## 18. Build a publish

```powershell
# Debug build + spuštění
cd E:\OpenClaw\OpenClawManager
dotnet build
dotnet run

# Release (self-contained single EXE)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
# Output: bin\Release\net8.0\win-x64\publish\OpenClawManager.exe (~70 MB)
```

**csproj klíčové položky:**
```xml
<AssemblyVersion>0.4.0.0</AssemblyVersion>
<ApplicationIcon>Resources\app-icon.ico</ApplicationIcon>
<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.2792.45" />
<PackageReference Include="System.Management" Version="8.0.0" />
<None Include="Resources\app-logo.svg">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

---

## 19. Git workflow

```powershell
# Průběžný commit
git add .
git commit -m "popis změny"

# Tag verze (po dokončení milestone)
git tag -a v0.95 -m "v0.95 - GUI redesign + Token Manager integration"
git log --oneline   # přehled commitů
```

**Doporučená struktura commit messages:**
```
fix: oprava parsování latencí (JSON + ANSI strip)
feat: lokalizace EN/CS (ResourceDictionary)
feat: dialog O aplikaci se SVG logem
feat: LiveLogWindow (FileSystemWatcher, sliding window)
feat: Gateway Log - tlačítko Kopírovat + Živá data
polish: tooltipy na všech tlačítkách, překlad menu
```

---

**Konec dokumentu v2.0**
