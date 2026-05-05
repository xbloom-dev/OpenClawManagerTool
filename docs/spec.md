# OpenClaw Manager Tool by Bloom — Specifikace

**Verze dokumentu:** 1.0
**Datum:** 4. května 2026
**Autor:** Bloom
**Status:** Draft pro v0.1

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
- Single EXE soubor s minimální RAM stopou (~30-50 MB)
- Embedded TUI terminál přímo v aplikaci (target stav)
- Lokalizace: čeština (UI texty)
- Žádné externí závislosti pro běh (self-contained .NET 8)

---

## 3. Technický stack

| Komponenta | Volba |
|---|---|
| Jazyk | C# 12 |
| Framework | .NET 8 (LTS do listopadu 2026) |
| UI | WPF (Windows Presentation Foundation) |
| IDE | VS Code + .NET 8 SDK |
| Architektura | Code-behind (ne MVVM v první verzi) |
| Distribuce | Self-contained single-file EXE (~70 MB) |
| Embedded terminál | EasyWindowsTerminalControl (ConPTY backend) |
| Cílový OS | Windows 10/11 |

**Důvody klíčových voleb:**
- **C# + WPF**: Profesionální desktop aplikace, nízká RAM stopa, AI asistenti mají s touto kombinací silnou znalost
- **.NET 8 self-contained**: Aplikace běží i bez nainstalovaného .NET runtime na PC uživatele
- **Code-behind**: Pro malou aplikaci a začátečníka v C# srozumitelnější než MVVM
- **EasyWindowsTerminalControl**: Plně podporuje VT100/ANSI escape sekvence (klíčové pro OpenClaw TUI)

---

## 4. Layout aplikace

### 4.1 Hlavní okno

Layout: dvě svislé panely vedle sebe.

```
┌─ OpenClaw Manager Tool by Bloom ─────────────────────────────────────────┐
│  Soubor   Nastavení   Nápověda                                           │
├──────────────────────────┬───────────────────────────────────────────────┤
│                          │                                               │
│   ┌─ Gateway ─────────┐  │                                               │
│   │ ●  Běží           │  │                                               │
│   │ PID: 25396        │  │                                               │
│   │ Uptime: 0:05:23   │  │                                               │
│   └───────────────────┘  │                                               │
│                          │                                               │
│   ┌─ Akce ────────────┐  │                                               │
│   │ ╔═══════════════╗ │  │           E M B E D D E D                     │
│   │ ║ SPUSTIT TUI   ║ │  │                                               │
│   │ ║ (restart GW)  ║ │  │           T E R M I N Á L                     │
│   │ ╚═══════════════╝ │  │                                               │
│   │                   │  │           ( OpenClaw TUI )                    │
│   │ Gateway:          │  │                                               │
│   │ [ Start  ]        │  │                                               │
│   │ [ Stop   ]        │  │                                               │
│   │ [ Restart]        │  │                                               │
│   │                   │  │                                               │
│   │ Nástroje:         │  │                                               │
│   │ [ Cleaning Tool ] │  │                                               │
│   │ [ Token Manager ] │  │                                               │
│   │                   │  │                                               │
│   │ Údržba:           │  │                                               │
│   │ [ doctor --fix ]  │  │                                               │
│   └───────────────────┘  │                                               │
│                          │                                               │
│   ┌─ Měření latence ──┐  │                                               │
│   │ Poslední:  1.2s   │  │                                               │
│   │ Průměr:    2.3s   │  │                                               │
│   │ Max:       8.5s   │  │                                               │
│   │ Requestů:  47     │  │                                               │
│   └───────────────────┘  │                                               │
│                          │                                               │
│   ┌─ Log aplikace ────┐  │                                               │
│   │ [15:30] Gateway   │  │                                               │
│   │ zastaven          │  │                                               │
│   │ [15:30] Spouštím  │  │                                               │
│   │ Gateway...        │  │                                               │
│   │ [15:30] Gateway   │  │                                               │
│   │ ready (21s)       │  │                                               │
│   └───────────────────┘  │                                               │
│                          │                                               │
├──────────────────────────┴───────────────────────────────────────────────┤
│  RAM: 12.5/63.8 GB │ VRAM: 0.8/15.9 GB │ CPU: 3% │ Gateway: ●  v0.1      │
└──────────────────────────────────────────────────────────────────────────┘
```

**Rozměry:**
- Levý panel (ovládání): šířka ~320 px, fixní
- Pravý panel (terminál): vyplní zbytek okna, minimum 600 px na šířku
- Spodní status bar: výška ~24 px, plná šířka okna
- Default velikost okna: 1280 × 800 px
- Minimální velikost okna: 800 × 500 px

### 4.2 Levý panel — ovládací prvky

**Sekce Gateway (status):**
- Indikátor stavu: zelená tečka + "Běží" / šedá tečka + "Neběží"
- PID procesu (pokud běží)
- Uptime od spuštění (formát H:MM:SS)
- Aktualizace každé 2 sekundy

**Sekce Akce:**
- **SPUSTIT TUI** — primární velké tlačítko (zvýrazněné)
  - Vždy provede: stop Gateway → start Gateway → spustit TUI
  - Během běhu zašedlé, popisek: "Restartuji Gateway..."
- **Gateway: Start** — aktivní jen pokud Gateway neběží
- **Gateway: Stop** — aktivní jen pokud Gateway běží, vyžaduje potvrzení dialogem
- **Gateway: Restart** — aktivní jen pokud Gateway běží
- **Cleaning Tool** — otevře modální okno
- **Token Manager** — otevře modální okno
- **doctor --fix** — spustí `openclaw doctor --fix` v externím CLI okně

**Sekce Měření latence:**
- Data z Gateway logu — parsování `res` záznamů s milisekundami
- **Poslední**: čas posledního dokončeného requestu
- **Průměr**: aritmetický průměr posledních 10 requestů
- **Max**: maximální zaznamenaná latence v aktuální session
- **Počet requestů**: celkem od startu Gateway

**Sekce Log aplikace:**
- Read-only textová oblast
- Posledních ~20 řádků akcí aplikace (start/stop/cleanup events)
- Auto-scroll na nejnovější řádek
- Časové razítko `[HH:mm]`

### 4.3 Pravý panel — terminál

**v0.1:** Externí TUI okno (jako současný `Start-OpenClaw.ps1`), v aplikaci pouze placeholder text "Embedded terminal coming in v0.3"

**v0.3+:** Embedded `EasyWindowsTerminalControl`:
- Spouští `openclaw tui` jako child process
- Plná podpora VT100/ANSI escape sekvencí
- Resize spolu s oknem aplikace
- Dark theme (kompatibilní s defaultem OpenClaw TUI)
- Když TUI neběží: prázdný terminál nebo placeholder text

### 4.4 Status bar (spodní lišta)

Vždy viditelná, aktualizace každé 2 sekundy:

| Prvek | Zdroj |
|---|---|
| RAM: X.X/Y.Y GB | `Win32_OperatingSystem` (FreePhysicalMemory, TotalVisibleMemorySize) |
| VRAM: X.X/Y.Y GB | `nvidia-smi --query-gpu=memory.used,memory.total` (pokud GPU dostupné, jinak skryto) |
| CPU: X% | `Win32_Processor` LoadPercentage (průměr přes všechna jádra) |
| Gateway: ● běží / ○ neběží | CIM dotaz (viz 4.1 Gateway status) |
| v0.X | Verze aplikace |

---

### 4.5 Detailní rozložení hlavního okna

#### 4.5.1 Hierarchie layoutu

```
MainWindow
└── DockPanel (root)
    ├── Menu (Dock=Top)                  ← horní menu lišta
    ├── StatusBar (Dock=Bottom)          ← spodní stavová lišta
    └── Grid (Fill)                      ← hlavní obsah
        ├── ColumnDefinition Width="320"  ← levý panel (fixní)
        ├── ColumnDefinition Width="5"    ← splitter
        └── ColumnDefinition Width="*"    ← pravý panel (rozšiřitelný)
        │
        ├── ScrollViewer (col 0)         ← levý panel s ovládáním
        │   └── StackPanel (vertical)
        │       ├── GroupBox "Gateway"
        │       ├── GroupBox "Akce"
        │       ├── GroupBox "Měření latence"
        │       └── GroupBox "Log aplikace"
        │
        ├── GridSplitter (col 1)         ← možnost změnit šířku panelů
        │
        └── Border (col 2)               ← pravý panel — terminál
            └── TerminalControl (v0.3+)  ← nebo placeholder ve v0.1
```

#### 4.5.2 Levý panel — sekce po sekci

**A) Sekce "Gateway" (GroupBox, výška ~110 px)**

```
┌─ Gateway ────────────────────┐
│                              │
│   ●  Běží                    │  ← StatusIndicator + Label
│   PID:    25396              │  ← Label (zarovnaný)
│   Uptime: 0:05:23            │  ← Label (zarovnaný)
│                              │
└──────────────────────────────┘
```

**Stavy indikátoru:**
- ● zelená + "Běží" — Gateway proces detekován
- ● žlutá + "Spouštím..." — startup probíhá (mezi click Start a "gateway ready")
- ● červená + "Selhalo" — startup vypršel timeout
- ○ šedá + "Neběží" — Gateway proces neexistuje

**Auto-skrytí PID/Uptime** když Gateway neběží (řádky se schovají, ne jen prázdné).

---

**B) Sekce "Akce" (GroupBox, výška ~280 px)**

```
┌─ Akce ───────────────────────┐
│                              │
│  ┌────────────────────────┐  │  ← primární tlačítko
│  │   ▶  SPUSTIT TUI       │  │     (vyšší než ostatní, ~40 px)
│  │      (restart GW)      │  │
│  └────────────────────────┘  │
│                              │
│  Gateway:                    │  ← Label "Gateway:" (sub-header)
│  ┌──────────┐ ┌──────────┐   │
│  │  Start   │ │   Stop   │   │  ← dvě tlačítka vedle sebe
│  └──────────┘ └──────────┘   │
│  ┌────────────────────────┐  │
│  │       Restart          │  │  ← jedno tlačítko na celou šířku
│  └────────────────────────┘  │
│                              │
│  Nástroje:                   │
│  ┌────────────────────────┐  │
│  │    Cleaning Tool       │  │
│  └────────────────────────┘  │
│  ┌────────────────────────┐  │
│  │    Token Manager       │  │
│  └────────────────────────┘  │
│                              │
│  Údržba:                     │
│  ┌────────────────────────┐  │
│  │    doctor --fix        │  │
│  └────────────────────────┘  │
│                              │
└──────────────────────────────┘
```

**Pravidla aktivnosti tlačítek:**

| Tlačítko          | Aktivní když                   | Neaktivní když                |
|-------------------|--------------------------------|-------------------------------|
| SPUSTIT TUI       | Vždy (kromě běhu sekvence)     | Během "restartuji Gateway..." |
| Gateway: Start    | Gateway neběží                 | Gateway běží                  |
| Gateway: Stop     | Gateway běží                   | Gateway neběží                |
| Gateway: Restart  | Gateway běží                   | Gateway neběží                |
| Cleaning Tool     | Vždy                           | —                             |
| Token Manager     | Vždy                           | —                             |
| doctor --fix      | Vždy                           | —                             |

**Tooltipy (hover popisy):**
- SPUSTIT TUI: "Zastaví Gateway, znovu ho spustí, pak otevře TUI. Použij když je něco rozbité."
- Gateway: Start: "Spustí openclaw gateway v novém okně"
- Gateway: Stop: "Korektně zastaví běžící Gateway (openclaw stop)"
- Gateway: Restart: "Stop + Start Gateway"
- Cleaning Tool: "Otevře nástroj pro čištění starých logů, sessions, cache"
- Token Manager: "Správa API klíčů a redakce konfiguračních souborů"
- doctor --fix: "Spustí openclaw doctor --fix pro opravu konfigurace"

---

**C) Sekce "Měření latence" (GroupBox, výška ~110 px)**

```
┌─ Měření latence ─────────────┐
│                              │
│   Poslední:    1.2 s         │
│   Průměr 10×:  2.3 s         │
│   Maximum:     8.5 s         │
│   Requestů:    47            │
│                              │
└──────────────────────────────┘
```

**Detaily:**
- Pravé zarovnání hodnot (sloupce: label vlevo, hodnota vpravo)
- Když Gateway neběží: všechny hodnoty zobrazí "—"
- Když latence > 5s: barva hodnoty červená
- Když latence < 1s: barva hodnoty zelená
- Mezi: defaultní (černá / dle systému)
- Tooltip nad sekcí: "Měřeno z Gateway logu (res záznamy). Resetuje se při restartu Gateway."

---

**D) Sekce "Log aplikace" (GroupBox, expand do zbývající výšky)**

```
┌─ Log aplikace ───────────────┐
│                              │
│  [15:30:01] Gateway zastaven │
│  [15:30:02] Spouštím Gateway │
│  [15:30:23] Gateway ready    │
│  [15:30:23] Spouštím TUI     │
│  [15:30:45] TUI připojen     │
│                          ▲   │  ← scrollbar pokud potřeba
│                          ▼   │
└──────────────────────────────┘
```

**Detaily:**
- ListBox nebo TextBox (ReadOnly) s vertikální scrollbarem
- Auto-scroll na nejnovější řádek (pokud zapnuto v Settings)
- Time prefix `[HH:mm:ss]` u každého řádku
- Drží posledních 100 řádků (starší se ořežou)
- Pravé tlačítko myši → kontext menu: "Kopírovat vše", "Vyčistit"
- Drobnější font (~11 px) aby se vešlo víc řádků

---

#### 4.5.3 Pravý panel — terminál

**v0.1 (placeholder):**

```
┌──────────────────────────────────────────────────────────┐
│                                                          │
│                                                          │
│                                                          │
│        Embedded terminal coming in v0.3                  │
│                                                          │
│        TUI nyní běží v samostatném okně.                 │
│                                                          │
│        Klikni "SPUSTIT TUI" pro otevření.                │
│                                                          │
│                                                          │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

Vystředěný text v Border kontejneru. Šedá barva textu.

**v0.3+ (embedded terminál):**

```
┌──────────────────────────────────────────────────────────┐
│ openclaw tui - ws://127.0.0.1:18789 - agent main         │
│ session agent:main:main                                  │
│                                                          │
│ > ahoj                                                   │
│ Ahoj! Jak ti můžu dnes pomoct?                           │
│                                                          │
│ > _                                                      │
│                                                          │
│                                                          │
│ connected | idle                                         │
│ agent main | session main | tokens 19k/33k (58%)         │
└──────────────────────────────────────────────────────────┘
```

`TerminalControl` (z `EasyWindowsTerminalControl`) vyplní celou plochu pravého panelu. Reaguje na resize okna.

---

#### 4.5.4 Status bar (spodní lišta)

```
├──────────────────────────────────────────────────────────────────┤
│ RAM: 12.5/63.8 GB │ VRAM: 0.8/15.9 GB │ CPU: 3% │ Gateway: ●     │ v0.1
└──────────────────────────────────────────────────────────────────┘
```

**Layout (zleva doprava):**
- RAM: 12.5/63.8 GB
- separator (svislá čára)
- VRAM: 0.8/15.9 GB (skryto pokud GPU nedostupná)
- separator
- CPU: 3%
- separator
- Gateway: ● běží / ○ neběží
- (vpravo, tlačený k pravé straně) v0.1

**Detaily:**
- Výška ~24 px
- Drobnější font (~11 px)
- Hodnoty se aktualizují každé 2 sekundy
- Indikátor Gateway je interaktivní (klik = focus do sekce Gateway v levém panelu)

---

#### 4.5.5 Horní menu

```
Soubor   Nastavení   Nápověda
```

**Soubor:**
- Otevřít OpenClaw složku v Průzkumníkovi
- Otevřít Temp složku v Průzkumníkovi
- Otevřít Gateway log
- ───
- Konec (Alt+F4)

**Nastavení:**
- Otevřít Nastavení... (Ctrl+,)

**Nápověda:**
- O aplikaci...
- Otevřít web OpenClaw

---

#### 4.5.6 Klávesové zkratky

| Zkratka       | Akce                          |
|---------------|-------------------------------|
| Ctrl+T        | SPUSTIT TUI                   |
| Ctrl+G        | Start/Stop Gateway (toggle)   |
| Ctrl+R        | Restart Gateway               |
| Ctrl+Shift+C  | Otevřít Cleaning Tool         |
| Ctrl+Shift+T  | Otevřít Token Manager         |
| Ctrl+,        | Otevřít Nastavení             |
| F1            | O aplikaci                    |
| Alt+F4        | Konec                         |

---

#### 4.5.7 Chování při různých stavech

**Při startu aplikace:**
1. Aplikace se otevře
2. Status bar metriky se začnou aktualizovat (~2s)
3. Detekce běžícího Gateway (CIM dotaz)
4. Pokud Gateway běží → status sekce aktualizována, latence se začne měřit z aktuálního logu
5. Log aplikace: "[HH:mm:ss] Aplikace spuštěna. Gateway: [stav]"

**Během "SPUSTIT TUI" sekvence:**
1. Tlačítko se zašedne, popisek "Restartuji Gateway..."
2. Log aplikace: "[HH:mm:ss] Zastavuji Gateway..."
3. Po stop → "[HH:mm:ss] Spouštím Gateway..."
4. Status indikátor přejde do žlutá "Spouštím..."
5. Po "gateway ready" → "[HH:mm:ss] Gateway ready (Xs)"
6. Status indikátor přejde do zelená "Běží"
7. Spuštění TUI (externí okno ve v0.1, embedded ve v0.3+)
8. Po detekci připojení → "[HH:mm:ss] TUI připojen (Xs)"
9. Tlačítko se vrátí do aktivního stavu

**Při zavření hlavního okna:**
- Pokud Gateway běží:
  - Settings: "Zobrazit varování při zavření okna pokud Gateway běží" → ANO
  - Dialog: "Gateway stále běží. Co udělat?"
  - Možnosti: [ Zastavit Gateway a zavřít ]  [ Zavřít a nechat běžet ]  [ Zrušit ]
- Pokud Gateway neběží: aplikace se zavře bez dotazu

---

#### 4.5.8 Modální okna — společná pravidla

Všechna modální okna (Cleaning Tool, Token Manager, Settings):
- Otevírají se jako `ShowDialog()` — blokují hlavní okno
- Vystředěné nad hlavním oknem (`WindowStartupLocation="CenterOwner"`)
- Mají tlačítko "Zavřít" / "Zrušit" v pravém spodním rohu
- Esc = zavřít okno
- Ikona stejná jako hlavní okno
- Title: "OpenClaw Manager — [název okna]"
- Resize povolen, ale s rozumnými minimálními rozměry

---

## 5. Modální okna

### 5.1 Cleaning Tool

```
┌─ Cleaning Tool ──────────────────────────────────────────────────┐
│                                                                  │
│  Vyberte kroky čištění:                                          │
│                                                                  │
│  ☑  [1] Staré logy (starší než dnešek)                           │
│  ☑  [2] Zálohy konfigurace (ponechat poslední 2)                 │
│  ☐  [3] Stability reporty (starší než 3 dny)                     │
│  ☑  [4] Browser cache (starší než 1 den)                         │
│  ☑  [5] Session locky                                            │
│  ☑  [6] sessions.json — zachovat:  ━━━●──────  10                │
│         (slider 1-50, aktivní jen při zaškrtnutí kroku 6)        │
│                                                                  │
│  ──────────────────────────────────────────────────────────      │
│                                                                  │
│  Průběh / Log:                                                   │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  [14:32:01] [1/6] Logy - smazáno: 12 souborů               │  │
│  │  [14:32:02] [2/6] Zálohy - smazáno: 3 soubory              │  │
│  │  ...                                                       │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  Celkem ušetřeno:  45.3 MB                                       │
│                                                                  │
│              [ Dry-run ]  [ Spustit ]  [ Zavřít ]                │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

**Pravidla:**
- Kompletní reimplementace logiky z `cleanup.ps1` v C#
- Pokud Gateway běží při kliknutí "Spustit": dialog s nabídkou "Zastavit Gateway a pokračovat / Pokračovat bez zastavení / Zrušit"
- Scheduled Task `OpenClaw Gateway` se dočasně vypne před cleanupem (pokud aplikace běží jako admin) a po cleanupu znovu zapne **(v0.2+)**
- Pokud aplikace nemá admin práva a uživatel zvolil zastavení Gateway: nabídnout restart aplikace s admin právy **(v0.2+)**
- Slider Keep Sessions je aktivní pouze pokud je krok [6] zaškrtnutý **(krok 6 ve v0.2+)**
- "Dry-run": projde všechny kroky, vypíše do logu co by smazalo, **nic nemaže**
- "Spustit": skutečně provede mazání

**Rozsah ve v0.1 (light verze):**
- Kroky 1-5 (logy, zálohy, stability, browser cache, locky)
- Krok 6 zobrazený, ale checkbox neaktivní s popiskem "Coming in v0.2"
- Slider Keep Sessions zobrazen, ale neaktivní
- Žádný Scheduled Task management
- Žádná detekce/práce s admin právy

**Detaily kroků (převzato z `cleanup.ps1`):**

| Krok | Cesta | Pravidlo |
|---|---|---|
| 1 — Logy | `%LOCALAPPDATA%\Temp\openclaw\openclaw-*.log` | LastWriteTime < dnešek |
| 2 — Zálohy | `~\.openclaw\openclaw.json.bak*` | Ponechat 2 nejnovější |
| 3 — Stability | `~\.openclaw\logs\stability\*` | Starší než 3 dny |
| 4 — Browser cache | `~\.openclaw\browser-data\*` (rekurzivně) | Starší než 1 den |
| 5 — Session locky | `~\.openclaw\agents\*\sessions\*.lock` | Všechny |
| 6 — sessions.json | `~\.openclaw\agents\<agent>\sessions.json` | Zachovat N nejnovějších, .bak před zápisem, rollback při chybě |

**Sessions JSON parser:**
- Akceptuje jak array, tak object strukturu
- Hledá timestamp v klíčích: `createdAt`, `timestamp`, `updatedAt`, `lastModified`, `mtime`, `created_at`
- Akceptuje formáty: ISO 8601 string, Unix epoch (sec/ms)
- Sessions bez detekovatelného timestamp se řadí jako nejstarší

**Default agenti:** `main`, `researcher`, `executive`, `safety` (konfigurovatelné v Settings).

### 5.2 Token Manager

Kompletní reimplementace funkcí ze specifikace `token-manager-spec-v2.md` v C# WPF. Žádný Node.js CLI.

```
┌─ Token Manager ──────────────────────────────────────────────────┐
│                                                                  │
│  Vault:  C:\Users\test\.token-manager\secrets.json               │
│                                                                  │
│  ┌─ Tokeny ──────────────────────────────────────────────────┐   │
│  │  ID                    Description       Created     Prev. │  │
│  │  gateway_token         OpenClaw GW       2025-05    sk..ab │  │
│  │  brave_api_key         Brave Search      2025-06    BS..xx │  │
│  │  ...                                                       │  │
│  └────────────────────────────────────────────────────────────┘  │
│  [ Přidat ]  [ Upravit ]  [ Smazat ]  [ Rotovat ]                │
│                                                                  │
│  ──────────────────────────────────────────────────────────────  │
│                                                                  │
│  Operace nad souborem:                                           │
│  Soubor:  [____________________________]   [ Procházet... ]      │
│                                                                  │
│  [ Redact ]  [ Restore ]  [ Verify ]                             │
│                                                                  │
│  Výstup:                                                         │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │  Redacted 5 tokens (3 unique IDs) in openclaw.json         │  │
│  │  Output: openclaw.redacted.json                            │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│                                          [ Zavřít ]              │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

**Funkce (z `token-manager-spec-v2.md`):**
- Správa vaultu: Add / Edit / Remove / Rotate token
- Operace na souboru: Redact / Restore / Verify
- Vault formát: stejný JSON schema jako spec
- Validace ID (regex `^[a-zA-Z0-9_]{1,64}$`)
- Validace value (min. 8 znaků)
- Round-trip (redact → restore = byte-identický soubor)
- Replacement: textová substituce s escape regex metaznaků, sort by length descending, replaceAll
- Restore vždy vytvoří `.bak` před přepsáním originálu

**Rozdíly oproti specu (kvůli GUI):**
- Žádné CLI parametry, vše přes UI
- Žádné exit kódy, místo nich vizuální zpětná vazba (zelená/červená)
- "Verify" zobrazí dialog s výsledkem (Safe / Found tokens / Stale placeholders)

### 5.3 Settings

```
┌─ Nastavení ──────────────────────────────────────────────────────┐
│                                                                  │
│  Cesty                                                           │
│  ──────                                                          │
│  OpenClaw složka:                                                │
│  [ C:\Users\test\.openclaw                ]   [ Procházet... ]   │
│                                                                  │
│  Temp složka (logy):                                             │
│  [ C:\Users\test\AppData\Local\Temp\openclaw  ] [ Procházet... ] │
│                                                                  │
│  Cesta k openclaw příkazu:                                       │
│  [ openclaw                               ]   [ Procházet... ]   │
│  (nechat 'openclaw' pro PATH lookup)                             │
│                                                                  │
│  Agenti                                                          │
│  ──────                                                          │
│  Seznam agentů (čárkou oddělené):                                │
│  [ main, researcher, executive, safety   ]                       │
│                                                                  │
│  Token Manager                                                   │
│  ──────                                                          │
│  Cesta k secrets.json:                                           │
│  [ C:\Users\test\.token-manager\secrets.json ] [ Procházet... ]  │
│                                                                  │
│  Aplikace                                                        │
│  ──────                                                          │
│  ☑  Spustit Gateway automaticky při startu aplikace              │
│  ☐  Zobrazit varování při zavření okna pokud Gateway běží        │
│  ☑  Auto-scroll v Log aplikace                                   │
│                                                                  │
│              [ Uložit ]  [ Reset na výchozí ]  [ Zrušit ]        │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

**Persistence:** JSON soubor v `%APPDATA%\OpenClawManager\settings.json`. Vytvoří se při prvním uložení, do té doby aplikace používá hardcoded defaulty.

---

## 6. Detekce a řízení procesů

### 6.1 Detekce běžícího Gateway

WMI/CIM dotaz (jako v `cleanup.ps1`):

```csharp
// Pseudokód
var query = "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='node.exe'";
foreach (var process in WmiQuery(query))
{
    if (process.CommandLine?.Contains("openclaw gateway") == true)
        return process.ProcessId;
}
return null;
```

### 6.2 Detekce "Gateway ready"

Sledování souboru `<TempPath>\openclaw-YYYY-MM-DD.log`. Před startem se starý log smaže (jako v `Start-OpenClaw.ps1`). Polling každých 500 ms, hledá řetězec `"gateway ready"`.

### 6.3 Detekce "TUI connected"

Sledování stejného Gateway logu. Před startem TUI se zaznamená počet řádků. Hledá se `sessions.list` v nových řádcích po startu TUI. Detekce funguje i pro embedded TUI (Gateway log je společný zdroj).

### 6.4 Měření latencí

Pasivní sledování Gateway logu. Parsuje řádky obsahující `res ✓ <method> <ms>ms`:
- Extract metodu (`sessions.list`, `chat.send`, atd.)
- Extract čas v ms
- Vede klouzavý průměr posledních 10 requestů
- Maximum v aktuální Gateway session

### 6.5 Spouštění Gateway

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = "powershell.exe",
    Arguments = "-NoExit -NoProfile -Command \"& openclaw gateway\"",
    UseShellExecute = true,
    CreateNoWindow = false
});
```

Nové viditelné okno PowerShellu — uživatel má vizuální feedback.

### 6.6 Spouštění TUI (v0.1, externí okno)

Stejně jako Gateway, ale s argumentem `tui`. Aplikace sleduje Gateway log pro detekci připojení.

### 6.7 Spouštění TUI (v0.3+, embedded)

Přes `EasyWindowsTerminalControl.StartProcess("openclaw", "tui")`. Knihovna spravuje ConPTY a vykresluje VT100 výstup.

### 6.8 Stop Gateway

Volá `openclaw stop`. Potvrzovací dialog před akcí.

### 6.9 Scheduled Task management

Pouze v Cleaning Tool, vyžaduje admin práva:
- `Get-ScheduledTask -TaskName "OpenClaw Gateway"` (přes WMI Win32_ScheduledJob nebo Process.Start s schtasks.exe)
- `Disable-ScheduledTask` před cleanupem
- `Enable-ScheduledTask` po cleanupu

---

## 7. Architektura kódu

```
OpenClawManager/
├── OpenClawManager.csproj
├── App.xaml / App.xaml.cs                 (entry point)
├── Views/
│   ├── MainWindow.xaml / .cs              (hlavní okno)
│   ├── CleaningWindow.xaml / .cs          (Cleaning Tool)
│   ├── TokenManagerWindow.xaml / .cs      (Token Manager)
│   └── SettingsWindow.xaml / .cs          (Settings)
├── Services/
│   ├── GatewayService.cs                  (start/stop/restart, log monitoring)
│   ├── TuiService.cs                      (TUI lifecycle, terminal embed)
│   ├── CleanupService.cs                  (cleanup logika z cleanup.ps1)
│   ├── TokenService.cs                    (token vault + redact/restore/verify)
│   ├── ProcessDetector.cs                 (CIM dotazy)
│   ├── LogMonitor.cs                      (Gateway log polling)
│   ├── LatencyTracker.cs                  (parser res záznamů z logu)
│   ├── ResourceMonitor.cs                 (RAM/VRAM/CPU)
│   └── ScheduledTaskService.cs            (správa Windows tasku)
├── Models/
│   ├── AppSettings.cs                     (cesty, konfigurace)
│   ├── GatewayState.cs                    (PID, uptime, status)
│   ├── CleanupResult.cs                   (výsledek cleanupu)
│   ├── Token.cs                           (vault entry)
│   └── LatencyStats.cs                    (poslední / průměr / max)
└── Resources/
    ├── icon.ico
    └── styles.xaml                        (společné WPF styly)
```

**Princip oddělení:**
- **Views** (`*.xaml.cs`): pouze code-behind pro UI events. Žádná business logika.
- **Services**: veškerá logika (procesy, file I/O, parsing). Testovatelné samostatně.
- **Models**: data structures. Žádné metody, jen properties.

---

## 8. Distribuce

### 8.1 Vývoj
- VS Code projekt
- Build: `dotnet build`
- Run: `dotnet run` nebo F5 v VS Code

### 8.2 Release
- Build self-contained single-file:
  ```
  dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
  ```
- Output: `bin/Release/net8.0/win-x64/publish/OpenClawManager.exe`
- Velikost: ~70 MB (obsahuje .NET 8 runtime)
- Distribuce jako ZIP s:
  - `OpenClawManager.exe`
  - `README.md`
  - (volitelně) `icon.ico` (samostatně pro reference)

### 8.3 Verzování
- Sémantické: `MAJOR.MINOR.PATCH`
- v0.x: vývojové verze, breaking changes možné
- v1.0: první stabilní release
- Verze viditelná v status baru aplikace

---

## 9. Plán vývoje (fázový)

### Fáze 1 — v0.1 "Skeleton + Cleaning Tool light" (cíl: ~1.5 týdne)
- Inicializace projektu (`dotnet new wpf`)
- Hlavní okno s layoutem (levý panel + pravý placeholder + status bar)
- Status bar s metrikami (RAM/VRAM/CPU/Gateway)
- Detekce běžícího Gateway (CIM)
- Tlačítka Start / Stop / Restart Gateway (externí PowerShell okno)
- Tlačítko SPUSTIT TUI (Restart Gateway + spustit TUI v externím okně)
- Tlačítko `doctor --fix` (externí okno)
- Měření latence (parsing Gateway logu)
- Settings okno (jen čtení/zápis cest, žádné pokročilé funkce)
- **Cleaning Tool — light verze** (kroky 1-5, bez sessions.json a bez scheduled task)
- Token Manager: pouze placeholder ("Coming in v0.4")

**Akceptační kritéria v0.1:**
- Aplikace se spustí, layout je správný
- Gateway lze přes aplikaci spustit, zastavit, restartovat
- Klik SPUSTIT TUI správně provede sekvenci stop → start → tui
- Status bar zobrazuje aktuální metriky
- Latence se měří a zobrazují
- Settings se ukládají a načítají
- Cleaning Tool umí kroky 1-5 (logy, zálohy, stability, browser cache, locky)
- Cleaning Tool má dry-run a live log

### Fáze 2 — v0.2 "Cleaning Tool full"
- Krok 6 v Cleaning Tool (sessions.json parser, backup + rollback)
- Scheduled Task management (vypnutí/zapnutí před/po cleanupem)
- Detekce admin práv + nabídka restartu s admin

### Fáze 3 — v0.3 "Embedded TUI"
- EasyWindowsTerminalControl integrace
- Embedded TUI v pravém panelu (nahrazuje placeholder)
- Externí TUI okno se přestane používat

### Fáze 4 — v0.4 "Token Manager"
- Modální okno Token Manager
- Vault management (add/edit/remove/rotate)
- File operations (redact/restore/verify)
- Round-trip testy

### Fáze 5 — v1.0 "Stable Release"
- Bug fixes
- Polishing UI
- README, ikona, instalace
- Testování na čistém Windows

---

## 10. Hodnoty a principy

**Bezpečnost před komfortem:**
- Cleaning Tool defaultně volí konzervativní hodnoty
- Stability reporty (krok 3) defaultně vypnuté — vyžadují vědomé zaškrtnutí
- Sessions.json cleanup vždy vytvoří `.bak`
- Token Manager redact je nedestruktivní (default vytváří `.redacted` soubor)

**Transparentnost:**
- Každá akce se loguje do Log aplikace v okně
- Cleaning Tool zobrazuje co by smazal (dry-run)
- Žádné skryté akce

**Robustnost:**
- Selhání jednoho kroku Cleaning Tool neshodí ostatní
- Token Manager rollback při selhání zápisu
- Graceful degradation: pokud chybí GPU, VRAM se v status baru skryje
- Pokud Gateway log není dostupný, latence se neměří, ale aplikace funguje dál

**Lokální data:**
- Žádná telemetrie
- Žádné connection do internetu
- Settings v `%APPDATA%`, ne v cloudu

---

## 11. Otevřené otázky pro budoucnost

- Master password pro `secrets.json` (Token Manager)
- Audit log pro Token Manager operace
- Multi-vault podpora
- Plugin system pro vlastní cleanup pravidla
- Dark/light theme switcher
- Lokalizace do EN

---

**Konec dokumentu v1.0 — bude rozšiřován během vývoje.**
