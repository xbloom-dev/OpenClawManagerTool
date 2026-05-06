# Dev Manual — OpenClaw Manager Tool by Bloom

**Účel:** Krok za krokem postup vývoje aplikace v0.1 pro úplného začátečníka v C#.
**Cíl:** Funkční aplikace splňující rozsah v0.1 podle `spec.md`.
**Prostředí:** Windows 11, .NET 8 SDK, VS Code, Git.

---

## Obsah

1. [Stav prostředí](#1-stav-prostředí) ✅
2. [Vytvoření projektu](#2-vytvoření-projektu) ✅
3. [První spuštění](#3-první-spuštění) ✅
4. [Inicializace Git repository](#4-inicializace-git-repository) ✅
5. [Struktura projektu](#5-struktura-projektu) ✅
6. [Otevření projektu v VS Code](#6-otevření-projektu-v-vs-code) ✅
7. [Hlavní okno — layout](#7-hlavní-okno--layout) ✅
8. [Status bar — metriky](#8-status-bar--metriky) ✅
9. [Detekce Gateway](#9-detekce-gateway) ✅
10. [Tlačítka Gateway](#10-tlačítka-gateway) ✅
11. [Měření latencí](#11-měření-latencí)
12. [SPUSTIT TUI sekvence](#12-spustit-tui-sekvence)
13. [Settings okno](#13-settings-okno)
14. [Cleaning Tool — light verze](#14-cleaning-tool--light-verze)
15. [Build a publish](#15-build-a-publish)

---

## 1. Stav prostředí

**Ověřeno (5.5.2026):**
- ✅ .NET 8 SDK 8.0.420
- ✅ VS Code 1.118.1 + C# Dev Kit
- ✅ Git 2.54.0

**Pozor — antivirus:**
Pokud máš Norton (nebo jiný antivirus), **přidej výjimku pro celou složku projektu** `E:\OpenClaw\OpenClawManager\`.
Norton během buildu maže `.dll` soubory které kompilátor vytváří, což vede k chybě "Access denied" při `dotnet run`.

V Nortonu:
- Settings → Antivirus → Scans and Risks → Items to Exclude from Scans
- *Také:* Items to Exclude from Auto-Protect, Script Control, SONAR, and Download Intelligence Detection
- Add Folder → `E:\OpenClaw\OpenClawManager\` → Include Subfolders

**VS Code rozšíření:**
- **C# Dev Kit** (Microsoft) — hlavní extension pro C# vývoj (instalace přes Extensions panel, Ctrl+Shift+X)
- **C#** (Microsoft) — nainstaluje se automaticky jako dependency

---

## 2. Vytvoření projektu

**Cílové umístění:** `E:\OpenClaw\OpenClawManager\`

**Důležité:** Pokud složka `OpenClawManager` už existuje (např. obsahuje `docs/`), použij příkaz **bez** `-n` parametru — projekt se vytvoří v aktuální složce s názvem dle složky:

```powershell
cd E:\OpenClaw\OpenClawManager
dotnet new wpf -f net8.0
```

Pokud složka neexistuje, použij standardní formu:

```powershell
cd E:\OpenClaw
dotnet new wpf -n OpenClawManager -f net8.0
cd OpenClawManager
```

**Co se vytvoří:**

```
E:\OpenClaw\OpenClawManager\
├── App.xaml                  ← entry point (XAML)
├── App.xaml.cs               ← entry point (C# code-behind)
├── AssemblyInfo.cs           ← metadata
├── MainWindow.xaml           ← hlavní okno (XAML)
├── MainWindow.xaml.cs        ← hlavní okno (C# code-behind)
├── OpenClawManager.csproj    ← projektový soubor
└── obj/, bin/                ← build artefakty (negitujeme)
```

---

## 3. První spuštění

```powershell
# Z E:\OpenClaw\OpenClawManager
dotnet run
```

**Co očekávat:**
- První build trvá ~30 sekund (stahují se package, kompiluje se .NET)
- Otevře se prázdné okno s názvem "MainWindow"
- Zavřením okna se aplikace ukončí, prompt se vrátí do PowerShellu

**Pokud nefunguje:**
- `error MSB3883: Could not find file ... refint\OpenClawManager.dll` → Norton/antivirus blokuje, viz sekci 1 (přidání výjimky)
- `error CS2012: Access to the path ... is denied` → totéž, antivirus
- Pokud po zavření okna PowerShell "mlčí", stiskni **Ctrl+C** pro vrácení promptu

**Po úspěšném testu:**
```powershell
# Vyčistit build artefakty (optional, ale doporučeno před první commit)
dotnet clean
```

---

## 4. Inicializace Git repository

```powershell
cd E:\OpenClaw\OpenClawManager

# Inicializace
git init

# Vytvořit .gitignore
@"
bin/
obj/
.vs/
*.user
*.suo
"@ | Out-File -FilePath .gitignore -Encoding UTF8

# První commit
git add .
git commit -m "Initial WPF project skeleton"
```

**Pozor:** Při `git add` se mohou objevit `warning: in the working copy of 'X', LF will be replaced by CRLF` — to je normální chování na Windows, není to chyba.

**Pokud Git nezná tvé jméno/email:**
```powershell
git config --global user.name "Tvé Jméno"
git config --global user.email "tvuj@email.cz"
```

---

## 5. Struktura projektu

Cílová struktura podle spec.md sekce 7:

```
OpenClawManager/
├── App.xaml / .cs
├── MainWindow.xaml / .cs
├── Views/
│   ├── CleaningWindow.xaml / .cs
│   └── SettingsWindow.xaml / .cs
├── Services/
│   ├── GatewayService.cs
│   ├── CleanupService.cs
│   ├── ProcessDetector.cs
│   ├── LogMonitor.cs
│   ├── LatencyTracker.cs
│   └── ResourceMonitor.cs
├── Models/
│   ├── AppSettings.cs
│   ├── GatewayState.cs
│   └── LatencyStats.cs
└── Resources/
    └── icon.ico
```

**Vytvoření složek:**

```powershell
mkdir Views
mkdir Services
mkdir Models
mkdir Resources
```

---

## 6. Otevření projektu v VS Code

```powershell
cd E:\OpenClaw\OpenClawManager
code .
```

Tečka říká VS Code aby otevřel **aktuální složku** jako workspace.

**Při prvním otevření:**
1. Dialog "Do you trust the authors of the files in this folder?" → klikni **"Yes, I trust the authors"**
2. Pokud se objeví doporučení k instalaci C# Dev Kit a ještě ho nemáš → Install
3. Případně tě může VS Code požádat o přihlášení k Microsoft účtu (C# Dev Kit) — pro osobní použití zdarma, můžeš přeskočit nebo přihlásit

**Co bys měl vidět:**
- Vlevo Explorer se seznamem souborů (App.xaml, MainWindow.xaml, atd.)
- Po kliknutí na `MainWindow.xaml.cs`: barevné zvýraznění C# kódu
- IntelliSense (po napsání tečky se objeví nabídka)

**Pokud něco nefunguje:**
- Zkontroluj že máš nainstalované **C# Dev Kit** v Extensions (Ctrl+Shift+X)
- Pokud kód nemá barvy, zkus restartovat VS Code

---

## 7. Hlavní okno — layout

**Soubor:** `MainWindow.xaml`

Layout dle spec.md sekce 4.5:

- **DockPanel** root
- **Menu** nahoře (Soubor / Nastavení / Nápověda)
- **StatusBar** dole (RAM / VRAM / CPU / Gateway / verze)
- **Grid** se třemi sloupci (320px levý panel, 5px splitter, zbytek pravý panel)
- **Levý panel:** ScrollViewer + StackPanel se 4 GroupBoxy (Gateway, Akce, Měření latence, Log aplikace)
- **Pravý panel:** Border s placeholderem "Coming in v0.3"

**Klíčová pojmenování (x:Name) pro code-behind:**
- `GatewayDot`, `GatewayStatusText`, `GatewayDetails`, `GatewayPid`, `GatewayUptime`
- `BtnStartTui`, `BtnGatewayStart`, `BtnGatewayStop`, `BtnGatewayRestart`, `BtnCleaningTool`, `BtnTokenManager`, `BtnDoctorFix`
- `LatencyLast`, `LatencyAvg`, `LatencyMax`, `LatencyCount`
- `AppLog` (ListBox)
- `StatusRam`, `StatusVram`, `StatusCpu`, `StatusGatewayDot`, `StatusGatewayText`

---

## 8. Status bar — metriky

**Soubor:** `Services/ResourceMonitor.cs`

Statická třída s metodou `Measure()` vracející `ResourceSnapshot` record s:
- `RamUsedGb`, `RamTotalGb` (přes Win32_OperatingSystem)
- `CpuPercent` (přes Win32_Processor LoadPercentage, průměr přes jádra)
- `VramUsedGb`, `VramTotalGb` (přes nvidia-smi, nullable pokud GPU není)

V `MainWindow.xaml.cs`:
- `DispatcherTimer` s intervalem 2 sekundy
- Při každém tiku: zavolat `ResourceMonitor.Measure()` a aktualizovat StatusBar TextBlocky
- VRAM se schová (`Visibility.Collapsed`) pokud `VramUsedGb` je null

**Důležité:**
- WMI dotazy fungují i na české lokalizaci Windows (na rozdíl od `Get-Counter`)
- `nvidia-smi` má timeout 2s — neblokuje aplikaci pokud GPU chybí

---

## 9. Detekce Gateway

**Soubor:** `Services/ProcessDetector.cs`

Statická třída se dvěma metodami:
- `FindGatewayProcess()` — vrací `Process?` (nullable), pokud Gateway běží
- `IsGatewayRunning()` — `bool`, zkratka pro rychlou kontrolu

WMI dotaz: `SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='node.exe'`

Filtruje výsledky kde `CommandLine` obsahuje současně `"openclaw"` a `"gateway"` —
to vyloučí jiné node.exe procesy (např. samotný TUI, jiné Node aplikace).

**Použití v MainWindow:**
- Timer (2s) volá `UpdateGatewayStatus()` který:
  - Aktualizuje barvu tečky (zelená/šedá)
  - Aktualizuje PID a uptime
  - Aktivuje/deaktivuje tlačítka Stop a Restart
  - Sleduje změnu PID — pokud se změní, zaznamená nový start time pro uptime

---

## 10. Tlačítka Gateway

**Soubor:** `Services/GatewayService.cs`

Statická třída s metodami:
- `Start()` — spustí `openclaw gateway` v novém PowerShell okně (`-NoExit`, viditelné)
- `Stop()` — zabije node.exe + PowerShell wrapper okno
- `Restart()` — Stop + 2s pauza + Start
- `RunDoctorFix()` — spustí `openclaw doctor --fix` v novém PowerShell okně

### Stop logika — důležitá rozhodnutí

**Co Stop dělá:**
1. Najde běžící Gateway node.exe (přes `ProcessDetector.FindGatewayProcess()`)
2. Zabije ho s `Process.Kill(entireProcessTree: true)` + čeká max 5s
3. Zabije všechna PowerShell okna která mají `openclaw gateway` v command line (uklid desktopu)

**Co Stop ZÁMĚRNĚ NEDĚLÁ:**
- **Nevolá `openclaw gateway stop`** — tento příkaz zastaví aktuální běh tasku, ale je to redundantní (zabití node.exe stačí)
- **Nedotýká se Scheduled Tasku "OpenClaw Gateway"** — uživatel chce automatické spouštění při přihlášení zachovat

**Proč ne `openclaw stop`?**
- Příkaz `openclaw stop` v OpenClaw 2026.5.2 **neexistuje** (vrátil "unknown command")
- Pro stop existuje `openclaw gateway stop`, ale ten zastaví Scheduled Task — což nepotřebujeme protože už zabíjíme samotný proces

**Bezpečnostní filtr v KillPowerShellGatewayWrappers:**
- Hledá `openclaw gateway` v command line PowerShell oken
- **Vynechá** okna obsahující `openclaw gateway stop` — pojistka proti budoucímu sebezabití

### Stop dialog

V `MainWindow.xaml.cs`:
```csharp
var result = MessageBox.Show(
    "Opravdu zastavit Gateway?\nVšechny aktivní TUI sessions budou přerušeny.",
    "Zastavit Gateway",
    MessageBoxButton.YesNo,
    MessageBoxImage.Question);
```

Dvě tlačítka (Yes/No), žádné třetí. Uživatel rozhodl že čisté UX > flexibilita.
Pokud uživatel chce vidět co Gateway dělal, použije menu Soubor → Otevřít Gateway log.

---

## 11. Měření latencí

*(Bude doplněno během vývoje.)*

---

## 12. SPUSTIT TUI sekvence

*(Bude doplněno během vývoje.)*

---

## 13. Settings okno

*(Bude doplněno během vývoje.)*

---

## 14. Cleaning Tool — light verze

*(Bude doplněno během vývoje.)*

---

## 15. Build a publish

*(Bude doplněno během vývoje.)*

---

**Konec dokumentu — bude rozšiřován během vývoje.**
