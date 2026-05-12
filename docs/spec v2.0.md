# OpenClaw Manager Tool by Bloom — Specifikace

**Verze dokumentu:** 2.0
**Datum:** 12. května 2026
**Autor:** Bloom
**Status:** v0.4 — aktuální stav

---

## 1. Účel a vize

**OpenClaw Manager Tool** je desktopová Windows aplikace pro kompletní management OpenClaw setupu. Jejím primárním uživatelem je vývojář / power user který používá OpenClaw denně a potřebuje:

- **Diagnostický nástroj** — když "se něco děje", aplikace dovolí rychle restartovat Gateway, znovu spustit TUI, vyčistit staré soubory, a změřit zda se chování zlepšilo
- **Měření výkonu** — sledování latencí odpovědí pro objektivní hodnocení rychlosti (po update OpenClaw, po změně konfigurace, po restartu, atd.)
- **Údržbové centrum** — Cleaning Tool, Token Manager a další nástroje pro správu OpenClaw souborů a konfigurace

**Není** to nástroj pro každodenní spouštění TUI — pro to slouží `OpenClaw.bat` zástupce na ploše a Scheduled Task který spouští Gateway při přihlášení.

---

## 2. Klíčové vlastnosti

- Portable Windows aplikace (ZIP, žádný installer)
- Single EXE soubor s minimální RAM stopou (~70 MB self-contained)
- Embedded TUI terminál přímo v aplikaci (WebView2 + xterm.js + ConPTY)
- Lokalizace: čeština a angličtina (přepínač v Nastavení)
- Žádné externí závislosti pro běh (self-contained .NET 8)

---

## 3. Technický stack

| Komponenta | Volba |
|---|---|
| Jazyk | C# 12 |
| Framework | .NET 8 (LTS) |
| UI | WPF (Windows Presentation Foundation) |
| IDE | VS Code + .NET 8 SDK |
| Architektura | Code-behind |
| Distribuce | Self-contained single-file EXE (~70 MB) |
| Embedded terminál | WebView2 + xterm.js + ConPTY (vlastní P/Invoke wrapper) |
| Lokalizace | ResourceDictionary (Strings.cs.xaml / Strings.en.xaml) |
| Cílový OS | Windows 10/11 |

---

## 4. Layout aplikace

### 4.1 Hlavní okno

Layout: dvě svislé panely vedle sebe, menu nahoře, status bar dole.

```
┌─ OpenClaw Manager Tool by Bloom ─────────────────────────────────────────┐
│  Otevřít   Nastavení   Nápověda                                          │
├──────────────────────────┬───────────────────────────────────────────────┤
│  ┌─ Akce ─────────────┐  │                                               │
│  │ ▶ OPENCLAW TUI     │  │                                               │
│  │   (restart GW)     │  │                                               │
│  │ Gateway:           │  │                                               │
│  │ [Start] [Stop]     │  │                                               │
│  │ [    Restart     ] │  │           E M B E D D E D                     │
│  │                    │  │                                               │
│  │ Otevřít:           │  │           T E R M I N Á L                     │
│  │ [PowerShell]       │  │                                               │
│  │ [Gateway log]      │  │           ( OpenClaw TUI )                    │
│  │                    │  │                                               │
│  │ Nástroje:          │  │                                               │
│  │ [Vyčistit soubory] │  │                                               │
│  │ [Správce API klíčů]│  │                                               │
│  │                    │  │                                               │
│  │ Údržba:            │  │                                               │
│  │ [Opravit konfigur.]│  │                                               │
│  └────────────────────┘  │                                               │
│  ┌─ Měření latence ───┐  │                                               │
│  │ Poslední:  1.2 s   │  │                                               │
│  │ Průměr 10×: 2.3 s  │  │                                               │
│  │ Maximum:   8.5 s   │  │                                               │
│  │ Requestů:  47      │  │                                               │
│  └────────────────────┘  │                                               │
│  ┌─ Log aplikace ─────┐  │                                               │
│  │ [15:30] GW zastaven│  │                                               │
│  │ [15:30] Spouštím.. │  │                                               │
│  │ [15:30] GW ready   │  │                                               │
│  └────────────────────┘  │                                               │
├──────────────────────────┴───────────────────────────────────────────────┤
│ Gateway: ● běží │ PID: 25396 │ uptime: 0:05:23 │ RAM: 12.5/63.8 GB │ ... │
└──────────────────────────────────────────────────────────────────────────┘
```

**Rozměry:**
- Levý panel: šířka 330 px, fixní
- Pravý panel: vyplní zbytek, minimum 600 px
- Spodní status bar: výška 24 px
- Default velikost: 1400 × 720 px
- Minimální velikost: 900 × 500 px

### 4.2 Levý panel — tlačítka a tooltipy

**TUI tlačítko (3 režimy):**
- Gateway neběží → zelené, spustí celou sekvenci (Gateway + TUI)
- Gateway běží → zelené, spustí jen TUI
- TUI běží → červené, zastaví TUI (Gateway zůstane)
- Tooltip popisuje všechny 3 režimy + zkratka Ctrl+T

**Gateway tlačítka:**

| Tlačítko | Aktivní kdy | Zkratka |
|---|---|---|
| Start | Gateway neběží | Ctrl+G |
| Stop | Gateway běží | Ctrl+G |
| Restart | Gateway běží | Ctrl+R |

**Otevřít:**
- PowerShell — otevře v pracovním adresáři z Nastavení
- Gateway log — otevře GatewayLogWindow (výchozí: posledních 20 řádků)

**Nástroje:**
- Vyčistit soubory (dříve "Cleaning Tool") — Ctrl+Shift+C
- Správce API klíčů (dříve "Token Manager") — dostupné v v0.5

**Údržba:**
- Opravit konfiguraci (dříve "doctor --fix") — spustí `openclaw "doctor --fix"`

**Barvy tlačítek:**
- `#D0FFD0` — zelená (TUI start, Dry-RUN/Náhled)
- `#FFD0D0` — červená (TUI stop, Spustit, Opravit konfiguraci)

### 4.3 Pravý panel — embedded terminál

**v0.1–v0.2:** Placeholder "Embedded terminal coming in v0.3"
**v0.3+:** `TerminalControl` — WebView2 + xterm.js + ConPTY P/Invoke wrapper
- Spouští `openclaw tui` jako child process přes ConPTY
- Plná podpora VT100/ANSI escape sekvencí
- Resize spolu s oknem
- 120×35 znaků (fixní rozlišení terminálu)

### 4.4 Status bar

`Gateway: ● stav | PID: X | uptime: H:MM:SS | RAM: X/Y GB | VRAM: X/Y GB | CPU: X% | v0.4`

Stavy Gateway indikátoru:
- ● zelená — běží
- ● oranžová — spouští se
- ● červená — selhalo
- ● šedá — neběží

VRAM se skryje pokud GPU není dostupná.

### 4.5 Hlavní menu

**Otevřít:**
- OpenClaw složku / Temp složku / Gateway log (submenu 10/20/30/50/100/celý) / PowerShell
- Konec

**Nastavení:**
- Otevřít Nastavení... (Ctrl+,)

**Nápověda:**
- O aplikaci... (F1)
- Otevřít web OpenClaw

### 4.6 Klávesové zkratky

| Zkratka | Akce |
|---|---|
| Ctrl+T | Start/Stop TUI |
| Ctrl+G | Start/Stop Gateway |
| Ctrl+R | Restart Gateway |
| Ctrl+Shift+C | Vyčistit soubory |
| Ctrl+, | Nastavení |
| F1 | O aplikaci |
| Alt+F4 | Konec |

---

## 5. Modální okna

### 5.1 Cleaning Tool (Vyčistit soubory)

6 kroků čištění s checkboxy:

| Krok | Cesta | Pravidlo |
|---|---|---|
| 1 — Logy | `%LOCALAPPDATA%\Temp\openclaw\openclaw-*.log` | LastWriteTime < dnešek |
| 2 — Zálohy | `~\.openclaw\openclaw.json.bak*` | Ponechat 2 nejnovější |
| 3 — Stability | `~\.openclaw\logs\stability\*` | Starší než 3 dny (výchozí OFF) |
| 4 — Browser cache | `~\.openclaw\browser-data\*` (rekurzivně) | Starší než 1 den |
| 5 — Session locky | `~\.openclaw\agents\*\sessions\*.lock` | Všechny |
| 6 — sessions.json | `~\.openclaw\agents\<agent>\sessions.json` | Zachovat N nejnovějších |

Tlačítka:
- **Náhled** (zelená `#D0FFD0`) — dry-run, nic nemaže
- **Spustit** (červená `#FFD0D0`) — skutečné mazání
- **Zavřít**

Sessions JSON parser akceptuje array i object strukturu, timestamps v ISO 8601 i Unix epoch.
Před zápisem vždy vytvoří `.bak`, při chybě rollback.

### 5.2 Správce API klíčů (Token Manager)

Dostupné v **v0.5**. Aktuálně zobrazí informační dialog.
Implementace dle `token-manager-spec-v2.md`.

### 5.3 Nastavení

Sekce:
- **Jazyk / Language** — RadioButton CS / EN (přepnutí po uložení)
- **Cesty** — OpenClaw složka, Temp složka, openclaw příkaz, PowerShell pracovní adresář
- **Soubor s nastavením** — informativní zobrazení cesty

Persistence: `%APPDATA%\OpenClawManager\settings.json`

### 5.4 O aplikaci

- SVG logo (WebView2, světlé pozadí `#F0F0F0`)
- Název, verze, stack (.NET 8, WPF, WebView2 + ConPTY)
- Tabulka klávesových zkratek
- Lokalizováno (CS/EN)

### 5.5 Gateway Log

- Dropdown pro počet řádků (10/20/30/50/100/celý, výchozí 20)
- Tlačítka nahoře: Aktualizovat, Kopírovat (s 2s fade feedback "✓ Zkopírováno")
- Status bar s cestou k souboru, velikostí, počtem řádků
- Tlačítka dole: **Živá data**, Zavřít

### 5.6 Živý log (LiveLogWindow)

Non-modální okno — otevírá se přes "Živá data" v Gateway Log dialogu (tento se zavře).

- `FileSystemWatcher` — reaguje okamžitě na každý zápis do logu
- Sliding window N řádků (N = výběr z předchozího Gateway Log dialogu)
- Nové řádky označeny prefixem `► ` po dobu 2s
- Status bar: počet řádků, počet nových od startu
- Tlačítko Kopírovat (kopíruje čistý obsah bez `► ` prefixů)

---

## 6. Detekce a řízení procesů

### 6.1 Detekce Gateway

WMI/CIM dotaz: `SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='node.exe'`
Filtr: CommandLine obsahuje `"openclaw"` i `"gateway"` (vyloučí jiné node.exe procesy).

### 6.2 Gateway ready detekce

Polling Gateway log souboru `%LOCALAPPDATA%\Temp\openclaw\openclaw-YYYY-MM-DD.log` každých 500ms.
Hledá `"gateway ready"` v JSON `message` poli. Timeout 180s.

### 6.3 Měření latencí

Parsování Gateway logu: JSON parse → `message` field → ANSI strip → regex `res\s+✓\s+([\w\.]+)\s+(\d+)ms`.
Klouzavý průměr posledních 10 requestů. Reset při restartu Gateway.
Barvy hodnot: červená > 5000ms, zelená < 1000ms.

### 6.4 Spouštění Gateway

```csharp
Process.Start(new ProcessStartInfo {
    FileName = "powershell.exe",
    Arguments = "-NoExit -NoProfile -Command \"& openclaw gateway\"",
    UseShellExecute = true
});
```

### 6.5 Stop Gateway

1. Zabije node.exe Gateway proces (`Kill(entireProcessTree: true)`)
2. Zabije PowerShell wrapper okna s `openclaw gateway` v command line
3. **Nedotýká se Scheduled Tasku** — zachovává automatické spouštění

### 6.6 TUI (embedded, v0.3+)

ConPTY P/Invoke wrapper (`ConPtyProcess.cs`). WebView2 + xterm.js terminál v pravém panelu.
120×35 znaků. `TuiStateChanged` event pro aktualizaci TUI tlačítka.

---

## 7. Architektura kódu

```
OpenClawManager/
├── OpenClawManager.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / .cs
├── Views/
│   ├── TerminalControl.xaml / .cs       (embedded TUI)
│   ├── CleaningWindow.xaml / .cs        (Cleaning Tool)
│   ├── TokenManagerWindow.xaml / .cs    (stub, v0.5)
│   ├── SettingsWindow.xaml / .cs
│   ├── AboutWindow.xaml / .cs           (SVG logo + zkratky)
│   ├── GatewayLogWindow.xaml / .cs      (log viewer)
│   ├── LiveLogWindow.xaml / .cs         (živé sledování)
│   └── StopGatewayDialog.xaml / .cs
├── Services/
│   ├── GatewayService.cs
│   ├── ProcessDetector.cs
│   ├── LogMonitor.cs                    (JSON parse + ANSI strip)
│   ├── LatencyTracker.cs                (file offset, sliding avg)
│   ├── ResourceMonitor.cs
│   ├── CleanupService.cs
│   ├── SettingsService.cs
│   ├── AdminService.cs
│   ├── ScheduledTaskService.cs
│   ├── ConPtyProcess.cs                 (P/Invoke ConPTY wrapper)
│   └── L10n.cs                          (ResourceDictionary lokalizace)
├── Models/
│   ├── AppSettings.cs                   (+ Language property)
│   ├── GatewayState.cs
│   ├── CleanupResult.cs
│   └── LatencyStats.cs
└── Resources/
    ├── app-icon.ico
    ├── app-logo.svg                     (krab, kopírován vedle EXE)
    └── Lang/
        ├── Strings.cs.xaml              (čeština)
        └── Strings.en.xaml             (angličtina)
```

---

## 8. Lokalizace

Systém: `ResourceDictionary` (`Resources/Lang/Strings.cs.xaml`, `Strings.en.xaml`).
Přepínač: `L10n.Apply(Language)` v `Services/L10n.cs`.
Přepnutí: Settings → Jazyk/Language → Uložit → `ApplyLocalization()` v každém otevřeném okně.

Přeloženo: všechna tlačítka, menu, tooltipy, log zprávy, status bar texty, dialogy.

CS názvy tlačítek:
- "Vyčistit soubory" (EN: "Cleaning Tool")
- "Správce API klíčů" (EN: "Token Manager")
- "Opravit konfiguraci" (EN: "doctor --fix")
- "Náhled" (EN: "Dry-RUN")

---

## 9. Distribuce

### 9.1 Build

```powershell
cd E:\OpenClaw\OpenClawManager
dotnet build          # debug
dotnet run            # spustit
```

### 9.2 Release (self-contained single-file EXE)

```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `bin\Release\net8.0\win-x64\publish\OpenClawManager.exe` (~70 MB)

Distribuce jako ZIP s `OpenClawManager.exe` + `README.md`.

### 9.3 Verzování

Sémantické: `MAJOR.MINOR.PATCH`. Aktuální: **v0.4.0**.

---

## 10. Plán vývoje

### ✅ v0.1 — Skeleton
Layout, status bar, Gateway detekce, Start/Stop/Restart, SPUSTIT TUI (externí okno), Settings (cesty).

### ✅ v0.2 — Cleaning Tool
Modální okno, 6 kroků, dry-run, live log, Gateway detekce před cleanupem.

### ✅ v0.3 — Embedded TUI + Sessions cleanup
ConPTY + WebView2 + xterm.js terminál v pravém panelu. Krok 6 sessions.json s backup/rollback. Admin elevation. Scheduled Task management.

### ✅ v0.4 — Polish (aktuální)
- Oprava parsování latencí (JSON + ANSI strip + file offset tracking)
- Lokalizace EN/CS (ResourceDictionary, přepínač v Settings)
- Dialog "O aplikaci" se SVG logem (WebView2, světlé pozadí)
- Barvy tlačítek (#D0FFD0 zelená / #FFD0D0 červená)
- Token Manager aktivní (info dialog o v0.5)
- Tooltipy na všech tlačítkách ve všech oknech
- Klávesové zkratky kompletní
- Menu plně lokalizováno
- Gateway Log: Kopírovat (2s feedback) + Živá data tlačítko
- LiveLogWindow: FileSystemWatcher, sliding window, zvýraznění nových řádků

### 🔲 v0.5 — GUI redesign
- Skinovatelnost (Legacy / Modern theme) via ResourceDictionary
- Splash screen video (MediaElement, MP4)

### 🔲 v0.6 — Token Manager
Implementace dle `token-manager-spec-v2.md`:
- Vault management (add/edit/remove/rotate)
- File operations (redact/restore/verify)
- Round-trip test (SHA-256)

### 🔲 v1.0 — Stable Release
Bug fixes, polishing, README, ikona, testování na čistém Windows.

---

## 11. Hodnoty a principy

**Bezpečnost před komfortem:**
- Cleaning Tool defaultně konzervativní (krok 3 vypnutý)
- sessions.json vždy `.bak` před zápisem + rollback
- Token Manager redact nedestruktivní

**Transparentnost:**
- Každá akce logována do "Log aplikace"
- Dry-run ukáže co by smazal

**Robustnost:**
- Selhání jednoho kroku Cleaning Tool neshodí ostatní
- Graceful degradation: bez GPU skryje VRAM
- Gateway log nedostupný → latence se neměří, aplikace funguje

**Lokální data:**
- Žádná telemetrie, žádný internet
- Settings v `%APPDATA%`, ne v cloudu

---

**Konec dokumentu v2.0**
