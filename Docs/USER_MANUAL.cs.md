# Uživatelský manuál — OpenClaw Manager Tool by Bloom

*Čeština · [English version](USER_MANUAL.md)*

**Verze aplikace:** v2.0
**Datum:** 25. května 2026

---

## Obsah

1. [Co je OpenClaw Manager](#1-co-je-openclaw-manager)
2. [Instalace a spuštění](#2-instalace-a-spuštění)
3. [Témata a splash screen](#3-témata-a-splash-screen)
4. [Hlavní okno](#4-hlavní-okno)
5. [Klávesové zkratky](#5-klávesové-zkratky)
6. [Spuštění OpenClaw TUI](#6-spuštění-openclaw-tui)
7. [Řízení Gateway](#7-řízení-gateway)
8. [Měření latencí](#8-měření-latencí)
9. [Gateway log](#9-gateway-log)
10. [Správce API klíčů (Token Manager)](#10-správce-api-klíčů-token-manager)
11. [Cleaning Tool — Vyčistit soubory](#11-cleaning-tool--vyčistit-soubory)
12. [Nastavení](#12-nastavení)
13. [O aplikaci](#13-o-aplikaci)
14. [Časté situace a řešení](#14-časté-situace-a-řešení)

---

## 1. Co je OpenClaw Manager

OpenClaw Manager je diagnostický a údržbový nástroj pro [OpenClaw](https://github.com/openclaw/openclaw) prostředí. Slouží hlavně tehdy, **když něco nefunguje správně** — umožňuje rychle restartovat Gateway, vyčistit staré soubory, změřit latence a diagnostikovat problém.

> **Co je OpenClaw?** OpenClaw je open-source AI-agent framework. Jeho **Gateway** je lokální démon, který propojuje agenty s AI poskytovateli (Anthropic, OpenAI, Ollama…), a **TUI** je terminálový dashboard pro sledování session, logů a využití modelů. Tento nástroj je nezávislá nadstavba — spravuje existující instalaci OpenClaw přes grafické rozhraní a není oficiálně spojen s projektem OpenClaw.

**Není** to náhrada za každodenní spouštění OpenClaw — pro normální provoz stačí zástupce nebo Scheduled Task. OpenClaw Manager se hodí, když potřebuješ:

- Restartovat Gateway po update nebo havárii
- Zjistit jak rychle Gateway odpovídá (měření latencí)
- Vyčistit staré logy a session soubory
- Bezpečně spravovat API klíče a tokeny (včetně přenosné zálohy)
- Opravit poškozenou konfiguraci

---

## 2. Instalace a spuštění

### Požadavky

- Windows 10 nebo 11 (x64)
- WebView2 Runtime (součást Windows 11; pro Windows 10 stáhnout z microsoft.com)
- OpenClaw nainstalovaný a funkční

### Spuštění

Spusť `OpenClawManager.exe` z runtime balíčku. Žádná instalace není potřeba — aplikace je přenositelná (portable). Terminál používá lokální xterm.js soubory — internet ani CDN nejsou potřeba.

### Nastavení cest při prvním spuštění

Při prvním spuštění zkontroluj v **Nastavení** (Ctrl+,) že cesty odpovídají tvému prostředí:

| Položka | Výchozí hodnota |
|---|---|
| OpenClaw složka | `~\.openclaw` |
| Temp složka (Gateway logy) | `%LOCALAPPDATA%\Temp\openclaw` |
| openclaw příkaz | `openclaw` (PATH lookup) |
| PowerShell pracovní adresář | `%APPDATA%\npm` |
| Trezor (vault) | `%USERPROFILE%\.token-manager\secrets.json` |

Pokud OpenClaw nespustíš přes `openclaw` v PATH, uprav „openclaw příkaz" na plnou cestu, například `C:\Users\jmeno\AppData\Roaming\npm\openclaw.cmd`.

---

## 3. Témata a splash screen

Téma změníš v **Nastavení**. OpenClaw Manager v2.0 nabízí **sedm témat**:

| Téma | Popis |
|---|---|
| **Legacy** | Klasické jednodušší rozhraní s emoji ikonami v tlačítkách. Splash je ASCII art. |
| **Modern** | Bitmap ikony, tmavě fialové splash pozadí, témovaná sekundární okna. |
| **Standard Dark** | Tmavá varianta výchozího vzhledu. |
| **Modern Dark** | Tmavé téma s moderní sadou ikon. |
| **Modern Light** | Světlé moderní téma. |
| **High Contrast** | Vysoký kontrast pro lepší čitelnost. |
| **Crab Cute** | Hravá sada ikon a barev. |

Modern témata používají bitmapové ikony a témovaná sekundární okna; Legacy zachovává jednodušší klasický vzhled.

### Splash screen

Chování splash panelu při startu závisí na tématu:

**Legacy:** v pravém panelu se zobrazí ASCII art splash — zmizí po kliknutí na **OpenClaw TUI**.

**Modern (a odvozená témata):**
1. Přehraje se `splash.mp4` (pokud existuje v `Resources/`)
2. Po doběhnutí videa zůstane statický `splash.png` jako freeze frame
3. Overlay zmizí po kliknutí na **OpenClaw TUI**

Pokud `splash.mp4` chybí nebo selže přehrávání, zobrazí se rovnou `splash.png`. Pokud chybí i `splash.png`, zobrazí se ASCII art jako fallback.

### Přepínání za běhu

Změna tématu se projeví okamžitě po uložení Nastavení. Při přepnutí mezi splash styly (Modern ↔ Legacy) před spuštěním TUI se video zastaví / ASCII art přepne podle nového tématu.

---

## 4. Hlavní okno

Okno je rozděleno na dvě části:

**Levý panel (330 px)** — ovládání, měření latencí, log aplikace
**Pravý panel** — embedded terminál (OpenClaw TUI)

### Sekce Akce

#### Tlačítko OpenClaw TUI

Hlavní tlačítko aplikace. Má **3 režimy** podle aktuálního stavu:

| Stav | Barva | Co udělá |
|---|---|---|
| Gateway neběží | 🟢 zelená | Spustí Gateway + počká na ready + spustí TUI |
| Gateway běží | 🟢 zelená | Spustí jen TUI (Gateway nechá běžet) |
| TUI běží | 🔴 červená | Zastaví TUI (Gateway nechá běžet) |

#### Gateway tlačítka

- **Start** — spustí `openclaw gateway` na pozadí v novém PowerShell okně
- **Stop** — zastaví běžící Gateway (zobrazí potvrzovací dialog)
- **Restart** — zastaví a znovu spustí Gateway; TUI se odpojí

#### Otevřít

- **PowerShell** — otevře PowerShell v pracovním adresáři nastaveném v Nastavení
- **Gateway log** — otevře dialog s výpisem Gateway logu
- **Živá data** (Ctrl+L) — otevře živé sledování Gateway logu

#### Nástroje

- **Vyčistit soubory** — otevře Cleaning Tool
- **Správce API klíčů** — správa trezoru tokenů a API klíčů

#### Údržba

- **Opravit konfiguraci** — spustí `openclaw "doctor --fix"` v PowerShellu

### Status bar (dole)

`Gateway: ● stav | PID: X | uptime: H:MM:SS | RAM: X/Y GB | VRAM: X/Y GB | CPU: X% | v2.0`

| Barva tečky | Stav |
|---|---|
| 🟢 zelená | Gateway běží |
| 🟠 oranžová | Gateway se spouští |
| 🔴 červená | Gateway selhalo |
| ⚫ šedá | Gateway neběží |

RAM, CPU a VRAM se aktualizují každé 2 sekundy na pozadí — UI se nezasekává.

---

## 5. Klávesové zkratky

| Zkratka | Akce |
|---|---|
| **Ctrl+T** | Start/Stop OpenClaw TUI |
| **Ctrl+G** | Start nebo Stop Gateway |
| **Ctrl+R** | Restart Gateway |
| **Ctrl+L** | Živá data Gateway logu |
| **Ctrl+Shift+C** | Otevřít Vyčistit soubory |
| **Ctrl+,** | Otevřít Nastavení |
| **F1** | O aplikaci |
| **Alt+F4** | Konec |

---

## 6. Spuštění OpenClaw TUI

### Standardní postup

1. Klikni na tlačítko **▶ OpenClaw TUI** (nebo Ctrl+T)
2. Aplikace automaticky:
   - Smaže starý Gateway log (pro čisté měření latencí)
   - Spustí Gateway
   - Čeká na „gateway ready" (max 3 minuty)
   - Spustí TUI v embedded terminálu vpravo
3. Průběh sleduj v „Log aplikace" vlevo dole

### Pokud Gateway již běží

Kliknutí spustí TUI přímo bez restartu Gateway.

### Zastavení TUI

Klikni na tlačítko **■ Zastavit OpenClaw TUI** (červené). Gateway zůstane běžet.

### Zavření aplikace s běžícím Gateway

Při zavření se zobrazí dialog se třemi možnostmi:
- **Ano** — zastaví Gateway a zavře aplikaci
- **Ne** — zavře aplikaci, Gateway nechá běžet na pozadí
- **Zrušit** — vrátí se zpět do aplikace

---

## 7. Řízení Gateway

### Spuštění Gateway

Tlačítko **Start** nebo Ctrl+G. Gateway se spustí v novém PowerShell okně. Aplikace čeká na „gateway ready" — status bar zobrazuje „spouští se..." dokud Gateway není připraven.

### Zastavení Gateway

Tlačítko **Stop** nebo Ctrl+G (pokud Gateway běží). Zobrazí se potvrzovací dialog.

**Poznámka:** Zastavení Gateway přeruší všechny aktivní TUI sessions. Scheduled Task pro automatické spouštění při přihlášení se **nezmění** — Gateway se znovu spustí při příštím přihlášení.

### Restart Gateway

Tlačítko **Restart** nebo Ctrl+R. TUI se odpojí, Gateway se zastaví a znovu spustí. Po úspěšném restartu se TUI automaticky znovu spustí.

---

## 8. Měření latencí

Sekce **Měření latence** v levém panelu zobrazuje rychlost odpovědí Gateway:

| Hodnota | Popis |
|---|---|
| Poslední | Latence posledního requestu |
| Průměr 10× | Klouzavý průměr posledních 10 requestů |
| Maximum | Nejvyšší naměřená latence od startu/restartu |
| Requestů | Celkový počet requestů od startu/restartu |

**Jak číst hodnoty:**
- 🟢 zelená — pod 1 000 ms (rychlá odpověď)
- černá — 1 000–5 000 ms (normální)
- 🔴 červená — nad 5 000 ms (pomalá odpověď, možná přetížení)

Hodnoty se začnou zobrazovat až po prvním requestu odeslaném přes TUI.

---

## 9. Gateway log

Otevři přes **Gateway log** tlačítko nebo přes menu **Otevřít → Gateway log**.

### Zobrazení logu

Vyber počet zobrazených řádků (výchozí: posledních 20) a klikni **Aktualizovat**. Nahoře se zobrazuje cesta k souboru, velikost a počet řádků.

### Kopírování do schránky

Klikni **Kopírovat** — celý obsah se zkopíruje do schránky. Po kliknutí se na 2 sekundy zobrazí „✓ Zkopírováno".

### Živé sledování (Ctrl+L)

Klikni **Živá data** pro otevření živého okna:

- Otevře se vedle hlavní aplikace (nezamkne ovládání)
- Automaticky detekuje nové záznamy
- Nejstarší záznamy se průběžně odstraňují (vždy N posledních řádků)
- Nové řádky jsou označeny `► ` po dobu 2 sekund
- Tlačítko **Kopírovat** zkopíruje čistý obsah (bez `► ` prefixů)

---

## 10. Správce API klíčů (Token Manager)

Otevři tlačítkem **Správce API klíčů** v levém panelu. Slouží k bezpečnému ukládání API klíčů a citlivých hodnot, k maskování souborů před jejich sdílením a k přenosné záloze trezoru.

### Trezor (vault) a bezpečnost

Trezor je soubor `secrets.json` v cestě nastavené v Nastavení (výchozí `%USERPROFILE%\.token-manager\`). Hodnoty tokenů jsou šifrované přes **Windows DPAPI** pro aktuálního uživatele. Trezor zkopírovaný na jiný počítač nebo pod jiný účet nelze dešifrovat.

Správce API klíčů zobrazí varování, pokud trezor leží v rizikovém umístění:
- uvnitř Git repozitáře
- uvnitř `.openclaw`
- v cloud-synchronizované složce (OneDrive, Dropbox, iCloud)
- ve sdílené nebo projektové složce

**Doporučení:** používej `%USERPROFILE%\.token-manager\` mimo projekt, Git a cloud sync.

### Inicializace trezoru

Při prvním otevření klikni **Inicializovat trezor**. Pokud existuje starší nešifrovaný trezor, aplikace ho automaticky zmigruje na šifrovanou verzi při prvním uložení.

### Přidání a správa tokenů

Klikni **Přidat** a vyplň:
- **ID** — unikátní identifikátor bez mezer (např. `OPENAI_KEY`) — používá se jako zastupný text `[REDACTED_OPENAI_KEY]`
- **Hodnota** — samotný tajný klíč
- **Popis** — volitelná poznámka

**Upravit** — nechej Hodnotu prázdnou pokud chceš zachovat stávající hodnotu.
**Rotovat** — zadá novou hodnotu bez ztráty ID a popisu.
**Odstranit** — odstraní token z trezoru.

### Záloha a obnova trezoru (`.ocvault`)

Trezor je svázaný s tvým Windows profilem (DPAPI), takže ho nejde jen tak zkopírovat na jiný počítač. Pro přenos nebo bezpečnostní zálohu slouží přenosný formát `.ocvault`:

- **Backup (Záloha)** — exportuje trezor do souboru `.ocvault` chráněného **heslem**, které zadáš. Soubor je šifrovaný přes **PBKDF2-SHA256** (200 000 iterací) a **AES-256-GCM** — nezávisle na DPAPI, takže ho lze obnovit i na jiném počítači.
- **Restore (Obnova)** — načte `.ocvault` po zadání hesla a znovu ho uloží do lokálního DPAPI trezoru pro aktuální Windows profil.

Heslo zadáváš v samostatném dialogu (`PasswordPromptWindow`). Při exportu i importu se použijí Save/Open dialogy pro výběr `.ocvault` souboru.

> ⚠️ **Bezpečnost zálohy:** `.ocvault` soubor a jeho heslo uchovávej odděleně a na bezpečném místě. Kdokoliv s oběma získá přístup k tvým tokenům. Zálohu neukládej do Git repozitáře ani do cloud sync složky vedle hesla.

### Maskování a obnova souborů

**Workflow maskování:**
1. Vyber token a klikni **Náhled** — zobrazí jak bude soubor vypadat po maskování
2. Klikni **[REDACT]** — nahradí hodnoty zastupným textem `[REDACTED_ID]`
3. Před odesláním klikni **Ověřit** — zkontroluje že soubor neobsahuje žádnou plaintext hodnotu

**Obnova:** klikni **Obnovit** — nahradí zastupné texty zpět hodnotami. Automaticky se vytvoří záložní `.bak` soubor.

### Tlačítka přehled

| Tlačítko | Barva | Funkce |
|---|---|---|
| **[REDACT]** | 🟢 zelené, tučné | Maskuje citlivé hodnoty v souboru |
| **Ověřit** | 🔵 modré, tučné | Ověří že soubor neobsahuje plaintext hodnotu |
| **Náhled** | 🔵 modré | Zobrazí náhled maskování bez zápisu |
| **Procházet** | 🔵 modré | Výběr souboru |
| **Backup** | 🔵 modré | Exportuje trezor do `.ocvault` chráněného heslem |
| **Restore** | 🔵 modré | Obnoví trezor z `.ocvault` |
| **Obnovit** | 🔴 červené | Obnoví zastupné texty na hodnoty |
| **Zavřít** | 🔴 červené | Zavře okno |

### Pomocné funkce

- **Zastupný text** — zkopíruje `[REDACTED_ID]` do schránky pro ruční vložení
- **Složka** — otevře adresář trezoru v Průzkumníku
- **.gitignore** — přidá cestu k trezoru do nejbližšího `.gitignore`
- **Import** — načte tokeny ze souboru (JSON nebo `KLÍČ=HODNOTA`)

---

## 11. Cleaning Tool — Vyčistit soubory

Otevři přes tlačítko **Vyčistit soubory** nebo Ctrl+Shift+C.

### Co lze vyčistit

| Krok | Co maže | Výchozí |
|---|---|---|
| 1 — Gateway logy | Staré log soubory (ne dnešní) | ✅ zapnuto |
| 2 — Zálohy konfigurace | `.bak` soubory (ponechá 2 nejnovější) | ✅ zapnuto |
| 3 — Stability logy | Logy starší než 3 dny | ❌ vypnuto |
| 4 — Browser cache | Cache starší než 1 den | ✅ zapnuto |
| 5 — Session locky | Zámkové soubory sessions | ✅ zapnuto |
| 6 — sessions.json | Stará session data (ponechá N nejnovějších) | ✅ zapnuto |
| 7 — Zálohy trezoru | `*.bak` v Token Manager složce | ❌ vypnuto (opt-in) |

Posuvníkem u kroku 6 nastav kolik sessions zachovat (výchozí: 10). Krok 7 je záměrně vypnutý — zálohy trezoru jsou záchrana při selhání obnovy.

### Doporučený postup

1. Klikni **Náhled** (modré tlačítko) — zobrazí co by se smazalo, nic neudělá
2. Zkontroluj výpis
3. Klikni **Spustit** (zelené tlačítko) — skutečné smazání

> ⚠️ **Upozornění:** Spustit trvale smaže vybrané soubory. Akci nelze vrátit.

---

## 12. Nastavení

Otevři přes menu **Nastavení → Otevřít Nastavení...** nebo Ctrl+,.

### Jazyk / Language

Přepni mezi **Čeština** a **English**. Změna se projeví po uložení.

### Cesty

| Pole | Popis |
|---|---|
| OpenClaw složka | Kde OpenClaw ukládá konfiguraci (`~\.openclaw`) |
| Temp složka | Kde jsou uloženy Gateway logy |
| openclaw příkaz | Příkaz nebo cesta k `openclaw` spustitelnému souboru |
| PowerShell pracovní adresář | Adresář kde se otevře PowerShell |
| Trezor (vault) | Cesta k `secrets.json` — šifrováno přes Windows DPAPI |

Tlačítka **Procházet...** otevřou dialog pro výběr složky. Pole „openclaw příkaz" validuje zakázané znaky — hodnota se zakázanými shell znaky (`<`, `>`, `%`, `^`, `&`, `|`) nejde uložit.

### Téma

Přepni mezi sedmi tématy (Legacy, Modern, Standard Dark, Modern Dark, Modern Light, High Contrast, Crab Cute). Změna se projeví okamžitě — viz kapitola [3. Témata a splash screen](#3-témata-a-splash-screen).

### Tlačítka

- **Uložit** — uloží a zavře
- **Reset na výchozí** — obnoví výchozí hodnoty (vyžaduje potvrzení)
- **Zrušit** — zavře bez uložení

Nastavení se ukládá do `%APPDATA%\OpenClawManager\settings.json`.

---

## 13. O aplikaci

Otevři přes menu **Nápověda → O aplikaci...** nebo F1. Zobrazuje logo, verzi, technický stack a přehled klávesových zkratek.

---

## 14. Časté situace a řešení

### Gateway se nespustí

**Příznak:** Status bar zůstane na „spouští se..." déle než 3 minuty.

**Řešení:**
1. Otevři **Gateway log** a zkontroluj poslední řádky
2. Zkontroluj v Nastavení zda je správný „openclaw příkaz"
3. Spusť **Opravit konfiguraci**
4. Zkus restartovat Gateway

### TUI se nezobrazí (prázdná černá plocha vpravo)

**Řešení:**
1. Počkej 2–3 sekundy — TUI se inicializuje
2. Klikni do oblasti terminálu vpravo
3. Pokud pořád prázdné — zastav TUI a spusť znovu

### Latence se nezobrazují (pomlčky)

**Příčina:** Latence se měří až po prvním requestu odeslaném v TUI.

**Řešení:** Napiš zprávu do TUI a počkej na odpověď.

### Správce API klíčů hlásí „trezor nenalezen"

**Řešení:** Klikni **Inicializovat trezor**. Pokud ses přihlásil pod jiným Windows účtem, trezor nelze dešifrovat — obnov ho ze zálohy `.ocvault` (Restore) nebo vytvoř nový a přidej tokeny znovu.

### Obnova `.ocvault` hlásí špatné heslo

**Příčina:** Heslo neodpovídá tomu, kterým byla záloha vytvořena.

**Řešení:** Zkontroluj heslo. `.ocvault` nelze obnovit bez správného hesla — žádná zadní vrátka neexistují.

### Správce API klíčů zobrazí varování o rizikovém umístění

**Řešení:** Otevři Nastavení → změň cestu k trezoru → uložit.

### Aplikace hlásí „Složka neexistuje"

**Řešení:** Otevři Nastavení (Ctrl+,) a oprav cestu tlačítkem **Procházet...**

### Gateway log je prázdný nebo nenalezen

**Příčina:** Gateway nebyl spuštěn přes tuto aplikaci.

**Řešení:** Spusť Gateway přes tlačítko **Start** nebo přes hlavní TUI tlačítko.

### Po restartu PC Gateway neběží

**Příčina:** Scheduled Task „OpenClaw Gateway" je zakázaný nebo nebyl vytvořen.

**Řešení:** Otevři Cleaning Tool → sekce „OpenClaw Gateway" → Enable Scheduled Task.

---

**Konec dokumentu — verze aplikace v2.0**
