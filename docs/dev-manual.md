# Dev Manual — OpenClaw Manager Tool by Bloom

**Účel:** Krok za krokem postup vývoje aplikace v0.1 pro úplného začátečníka v C#.
**Cíl:** Funkční aplikace splňující rozsah v0.1 podle `spec.md`.
**Prostředí:** Windows 11, .NET 8 SDK, VS Code, Git.

---

## Obsah

1. [Stav prostředí](#1-stav-prostředí)
2. [Vytvoření projektu](#2-vytvoření-projektu)
3. [První spuštění](#3-první-spuštění)
4. [Inicializace Git repository](#4-inicializace-git-repository)
5. [Struktura projektu](#5-struktura-projektu)
6. [Hlavní okno — layout](#6-hlavní-okno--layout)
7. [Status bar — metriky](#7-status-bar--metriky)
8. [Detekce Gateway](#8-detekce-gateway)
9. [Tlačítka Gateway](#9-tlačítka-gateway)
10. [Měření latencí](#10-měření-latencí)
11. [SPUSTIT TUI sekvence](#11-spustit-tui-sekvence)
12. [Settings okno](#12-settings-okno)
13. [Cleaning Tool — light verze](#13-cleaning-tool--light-verze)
14. [Build a publish](#14-build-a-publish)

---

## 1. Stav prostředí

**Ověřeno (4.5.2026):**
- ✅ .NET 8 SDK 8.0.420
- ✅ VS Code 1.118.1
- ✅ Git 2.54.0

**Doporučená VS Code rozšíření:**
- **C# Dev Kit** (Microsoft) — hlavní extension pro C# vývoj
- **C#** (Microsoft) — nainstaluje se automaticky s Dev Kit
- **XAML** (volitelně) — lepší syntax highlighting pro XAML soubory

**Instalace rozšíření v VS Code:**
1. Otevři VS Code
2. Klikni na ikonu Extensions vlevo (Ctrl+Shift+X)
3. Vyhledej "C# Dev Kit" → klikni Install
4. Rozšíření **C#** se nainstaluje automaticky jako dependency

---

## 2. Vytvoření projektu

**Cílové umístění:** `E:\OpenClaw\OpenClawManager\`

Otevři PowerShell a spusť:

```powershell
# Přejít do nadřazené složky
cd E:\OpenClaw

# Vytvořit nový WPF projekt
dotnet new wpf -n OpenClawManager -f net8.0

# Vstoupit do projektu
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
- Zavřením okna se aplikace ukončí

**Pokud nefunguje:**
- Zkontroluj že jsi v správné složce (`pwd` musí vrátit `E:\OpenClaw\OpenClawManager`)
- Zkontroluj že existuje `OpenClawManager.csproj`

---

## 4. Inicializace Git repository

```powershell
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

## 6. Hlavní okno — layout

*(Bude doplněno během vývoje.)*

---

## 7. Status bar — metriky

*(Bude doplněno během vývoje.)*

---

## 8. Detekce Gateway

*(Bude doplněno během vývoje.)*

---

## 9. Tlačítka Gateway

*(Bude doplněno během vývoje.)*

---

## 10. Měření latencí

*(Bude doplněno během vývoje.)*

---

## 11. SPUSTIT TUI sekvence

*(Bude doplněno během vývoje.)*

---

## 12. Settings okno

*(Bude doplněno během vývoje.)*

---

## 13. Cleaning Tool — light verze

*(Bude doplněno během vývoje.)*

---

## 14. Build a publish

*(Bude doplněno během vývoje.)*

---

**Konec dokumentu — bude rozšiřován během vývoje.**
