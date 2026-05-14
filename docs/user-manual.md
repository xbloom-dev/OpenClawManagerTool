# Uživatelský manuál — OpenClaw Manager Tool by Bloom

**Verze aplikace:** v0.99
**Verze dokumentu:** 2.0
**Datum:** 13. května 2026

---

## Obsah

1. [Co je OpenClaw Manager](#1-co-je-openclaw-manager)
2. [Instalace a spuštění](#2-instalace-a-spuštění)
3. [Hlavní okno](#3-hlavní-okno)
4. [Klávesové zkratky](#4-klávesové-zkratky)
5. [Spuštění OpenClaw TUI](#5-spuštění-openclaw-tui)
6. [Řízení Gateway](#6-řízení-gateway)
7. [Měření latencí](#7-měření-latencí)
8. [Gateway log](#8-gateway-log)
9. [Cleaning Tool — Vyčistit soubory](#9-cleaning-tool--vyčistit-soubory)
10. [Nastavení](#10-nastavení)
11. [O aplikaci](#11-o-aplikaci)
12. [Token Manager](#12-token-manager)
13. [Témata a splash screen](#13-témata-a-splash-screen)
14. [Časté situace a řešení](#14-časté-situace-a-řešení)

---

## 1. Co je OpenClaw Manager

OpenClaw Manager je diagnostický a údržbový nástroj pro OpenClaw setup. Slouží hlavně tehdy, **když něco nefunguje správně** — umožňuje rychle restartovat Gateway, vyčistit staré soubory, změřit latence a diagnostikovat problém.

**Není** to náhrada za každodenní `OpenClaw.bat` — pro normální provoz stačí zástupce na ploše nebo Scheduled Task. OpenClaw Manager se hodí, když potřebuješ:

- Restartovat Gateway po update nebo havárii
- Zjistit jak rychle Gateway odpovídá (měření latencí)
- Vyčistit staré logy a session soubory
- Spravovat API klíče a tokeny bezpečně přes Token Manager
- Opravit poškozenou konfiguraci

---

## 2. Instalace a spuštění

### Požadavky

- Windows 10 nebo 11
- WebView2 Runtime (součást Windows 11; pro Windows 10 stáhnout z microsoft.com)
- OpenClaw nainstalovaný a funkční

### Spuštění

Spusť `OpenClawManager.exe`. Žádná instalace není potřeba — aplikace je přenositelná (portable). Terminál používá lokální xterm.js soubory ze složky `Resources/Terminal` — internet ani CDN nejsou potřeba.

### Nastavení cest při prvním spuštění

Při prvním spuštění zkontroluj v **Nastavení** (Ctrl+,) že cesty odpovídají tvému setupu:

| Položka | Výchozí hodnota |
|---|---|
| OpenClaw složka | `~\.openclaw` |
| Temp složka (Gateway logy) | `%LOCALAPPDATA%\Temp\openclaw` |
| openclaw příkaz | `openclaw` (PATH lookup) |
| PowerShell pracovní adresář | `%APPDATA%\npm` |
| Token Manager vault | `%USERPROFILE%\.token-manager\secrets.json` |

Pokud OpenClaw nespustíš přes `openclaw` v PATH, uprav "openclaw příkaz" na plnou cestu, například `C:\Users\jmeno\AppData\Roaming\npm\openclaw.cmd`.

---

## 3. Hlavní okno

Okno je rozděleno na dvě části:

**Levý panel (330 px)** — ovládání, měření latencí, log aplikace
**Pravý panel** — embedded terminál (OpenClaw TUI)

### Levý panel — sekce Akce

#### Tlačítko OpenClaw TUI

Hlavní tlačítko aplikace. Má **3 režimy** podle aktuálního stavu:

| Stav | Barva | Co udělá |
|---|---|---|
| Gateway neběží | 🟢 zelená | Spustí Gateway + počká na ready + spustí TUI |
| Gateway běží | 🟢 zelená | Spustí jen TUI (Gateway nechá běžet) |
| TUI běží | 🔴 červená | Zastaví TUI (Gateway nechá běžet) |

Text pod tlačítkem vždy ukazuje co se stane při kliknutí.

#### Gateway tlačítka

- **Start** — spustí `openclaw gateway` na pozadí v novém PowerShell okně
- **Stop** — zastaví běžící Gateway (zobrazí potvrzovací dialog)
- **Restart** — zastaví a znovu spustí Gateway; TUI se odpojí

#### Otevřít

- **PowerShell** — otevře PowerShell v pracovním adresáři nastaveném v Nastavení
- **Gateway log** — otevře dialog s výpisem Gateway logu

#### Nástroje

- **Vyčistit soubory** — otevře Cleaning Tool pro údržbu souborů OpenClaw
- **Správce API klíčů** — správa lokálního vaultu tokenů, redakce, obnova a ověření souborů

#### Údržba

- **Opravit konfiguraci** — spustí `openclaw "doctor --fix"` v PowerShellu

### Status bar (dole)

`Gateway: ● stav | PID: X | uptime: H:MM:SS | RAM: X/Y GB | VRAM: X/Y GB | CPU: X% | v0.99`

| Barva tečky | Stav |
|---|---|
| 🟢 zelená | Gateway běží |
| 🟠 oranžová | Gateway se spouští |
| 🔴 červená | Gateway selhalo |
| ⚫ šedá | Gateway neběží |

RAM, CPU a VRAM se aktualizují každé 2 sekundy asynchronně — nezasekávají UI.

---

## 4. Klávesové zkratky

| Zkratka | Akce |
|---|---|
| **Ctrl+T** | Start/Stop OpenClaw TUI |
| **Ctrl+G** | Start nebo Stop Gateway |
| **Ctrl+R** | Restart Gateway |
| **Ctrl+Shift+C** | Otevřít Vyčistit soubory |
| **Ctrl+,** | Otevřít Nastavení |
| **F1** | O aplikaci |
| **Alt+F4** | Konec |

---

## 5. Spuštění OpenClaw TUI

### Standardní postup

1. Klikni na tlačítko **▶ OpenClaw TUI** (nebo Ctrl+T)
2. Aplikace automaticky:
   - Smaže starý Gateway log (pro čisté měření latencí)
   - Spustí Gateway
   - Čeká na "gateway ready" (max 3 minuty)
   - Spustí TUI v embedded terminálu vpravo
3. Průběh sleduj v "Log aplikace" vlevo dole

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

## 6. Řízení Gateway

### Spuštění Gateway

Tlačítko **Start** nebo Ctrl+G. Gateway se spustí v novém PowerShell okně. Aplikace čeká na signál "gateway ready" — status bar zobrazuje "spouští se..." dokud Gateway není připraven.

### Zastavení Gateway

Tlačítko **Stop** nebo Ctrl+G (pokud Gateway běží). Zobrazí se potvrzovací dialog.

**Poznámka:** Zastavení Gateway přeruší všechny aktivní TUI sessions. Scheduled Task pro automatické spouštění při přihlášení se **nezmění** — Gateway se znovu spustí při příštím přihlášení.

### Restart Gateway

Tlačítko **Restart** nebo Ctrl+R. TUI se odpojí, Gateway se zastaví a znovu spustí. Po úspěšném restartu se TUI automaticky znovu spustí.

---

## 7. Měření latencí

Sekce **Měření latence** v levém panelu zobrazuje rychlost odpovědí Gateway:

| Hodnota | Popis |
|---|---|
| Poslední | Latence posledního requestu |
| Průměr 10× | Klouzavý průměr posledních 10 requestů |
| Maximum | Nejvyšší naměřená latence od startu/restartu |
| Requestů | Celkový počet requestů od startu/restartu |

**Jak číst hodnoty:**
- 🟢 zelená — pod 1000 ms (rychlá odpověď)
- černá — 1000–5000 ms (normální)
- 🔴 červená — nad 5000 ms (pomalá odpověď, možná přetížení)

**Kdy se hodnoty resetují:** při každém restartu Gateway nebo spuštění přes hlavní TUI tlačítko.

**Kdy se hodnoty aktualizují:** automaticky každé 2 sekundy ze Gateway logu. Hodnoty se začnou zobrazovat až po prvním requestu v TUI.

---

## 8. Gateway log

Otevři přes **Gateway log** tlačítko nebo přes menu **Otevřít → Gateway log**.

### Zobrazení logu

V horní části vyber počet zobrazených řádků (výchozí: posledních 20). Klikni **Aktualizovat** pro obnovení obsahu. Nahoře se zobrazuje cesta k souboru, velikost a počet zobrazených řádků.

### Kopírování do schránky

Klikni **Kopírovat** — celý zobrazený obsah se zkopíruje do schránky Windows. Po kliknutí se na 2 sekundy zobrazí "✓ Zkopírováno".

### Živé sledování

Klikni **Živá data** pro otevření okna s živým sledováním logu:

- Okno se otevře vedle hlavní aplikace (nezamkne ovládání)
- Automaticky detekuje nové záznamy a přidává je dole
- Nejstarší záznamy se průběžně odstraňují (zobrazuje se vždy N posledních řádků)
- Nové řádky jsou označeny `► ` po dobu 2 sekund
- Tlačítko **Kopírovat** zkopíruje čistý obsah (bez `► ` prefixů)
- Status bar ukazuje počet řádků a počet nových záznamů od otevření

---

## 9. Cleaning Tool — Vyčistit soubory

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
| 7 — Token Manager zálohy | `*.bak` v Token Manager složce | ❌ vypnuto (opt-in) |

Posuvníkem u kroku 6 nastav kolik nejnovějších sessions zachovat (výchozí: 10).

Krok 7 je záměrně vypnutý — zálohy Token Manageru jsou záchrana pro případ selhání restore operace. Mazej je vědomě.

### Doporučený postup

1. Klikni **Náhled** (zelené tlačítko) — zobrazí co by se smazalo, nic neudělá
2. Zkontroluj výpis v logu
3. Klikni **Spustit** (červené tlačítko) — skutečné smazání

> ⚠️ **Upozornění:** Spustit trvale smaže vybrané soubory. Akci nelze vrátit (kromě sessions.json, kde se vytváří záloha).

### sessions.json — bezpečnost

Před každou úpravou `sessions.json` se automaticky vytvoří záložní soubor `.bak`. Pokud dojde k chybě při zápisu, soubor se obnoví ze zálohy.

---

## 10. Nastavení

Otevři přes menu **Nastavení → Otevřít Nastavení...** nebo Ctrl+,.

### Jazyk / Language

Přepni mezi **Čeština** a **English**. Změna se projeví po kliknutí na **Uložit** — celá aplikace se přepne do vybraného jazyka.

### Cesty

| Pole | Popis |
|---|---|
| OpenClaw složka | Kde OpenClaw ukládá konfiguraci (`~\.openclaw`) |
| Temp složka | Kde jsou uloženy Gateway logy |
| openclaw příkaz | Příkaz nebo cesta k `openclaw` spustitelnému souboru |
| PowerShell pracovní adresář | Adresář kde se otevře PowerShell z tlačítka PowerShell |
| Token Manager vault | Cesta k `secrets.json` — uloženo šifrovaně přes DPAPI |

Tlačítka **Procházet...** otevřou dialog pro výběr složky. Pole "openclaw příkaz" validuje zakázané znaky (`'`, `"`, `;`, `&`, `|`, `` ` ``) — takové hodnoty nejdou uložit.

### Tlačítka

- **Uložit** — uloží nastavení a zavře dialog
- **Reset na výchozí** — obnoví všechny hodnoty na výchozí (vyžaduje potvrzení)
- **Zrušit** — zavře bez uložení

Nastavení se ukládá do `%APPDATA%\OpenClawManager\settings.json`.

---

## 11. O aplikaci

Otevři přes menu **Nápověda → O aplikaci...** nebo F1. Zobrazuje logo, verzi, technický stack a přehled klávesových zkratek.

---

## 12. Token Manager

Token Manager otevřeš tlačítkem **Správce API klíčů** v levém panelu. Slouží k bezpečnému ukládání API klíčů a jiných citlivých hodnot a k redakci souborů před jejich sdílením.

### Vault a bezpečnost

Vault je JSON soubor `secrets.json` v cestě nastavené v Nastavení (výchozí `%USERPROFILE%\.token-manager\`). Hodnoty tokenů jsou šifrované přes **Windows DPAPI** pro aktuálního Windows uživatele. Vault zkopírovaný na jiný počítač nebo pod jiný účet nejde dešifrovat — je vázaný na konkrétní Windows profil.

Pokud existuje starší vault s nešifrovanými hodnotami, aplikace ho automaticky přemigruje na šifrovanou verzi při prvním uložení.

Token Manager zobrazí varování, pokud vault leží v rizikovém umístění:
- uvnitř Git repozitáře
- uvnitř `.openclaw`
- v cloud-synchronizované složce (OneDrive, Dropbox, iCloud)
- ve sdílené nebo projektové složce

**Doporučení:** používej `%USERPROFILE%\.token-manager\` nebo jinou složku chráněnou Windows účtem, mimo projekt, Git a cloud sync.

### Inicializace vaultu

Při prvním otevření Token Manageru klikni **Inicializovat vault**. Vytvoří se prázdný šifrovaný soubor na cestě z Nastavení.

### Přidání a správa tokenů

Klikni **Přidat** a vyplň:
- **ID** — unikátní identifikátor bez mezer (např. `OPENAI_KEY`), používá se jako placeholder `[REDACTED_OPENAI_KEY]`
- **Hodnota** — samotný tajný klíč (zobrazí se jako PasswordBox)
- **Popis** — volitelná poznámka

Existující token upravíš přes **Upravit** — nechej Hodnotu prázdnou pokud chceš zachovat stávající hodnotu. Tlačítko **Odstranit** odstraní token ze vaultu. Tlačítko **Rotovat** umožní zadat novou hodnotu bez ztráty ID a popisu.

### Redakce a obnova souborů

Token Manager slouží k bezpečnému sdílení souborů, které by jinak obsahovaly plaintext klíče.

**Workflow redakce:**
1. Vyber token v seznamu a klikni **Náhled** — zobrazí jak bude soubor vypadat po redakci
2. Klikni **Redact** — v souboru nahradí hodnoty tokenu textem `[REDACTED_ID]`
3. Před odesláním souboru klikni **Verify** — ověří, že soubor neobsahuje žádnou plaintext hodnotu

**Obnova souboru:**
- Klikni **Restore** — nahradí placeholdery zpět skutečnými hodnotami
- Při restore se automaticky vytvoří záložní `.bak` soubor; při chybě se soubor obnoví ze zálohy

### Pomocné funkce

- **Placeholder** — zkopíruje text `[REDACTED_ID]` do schránky pro ruční vložení do souborů
- **Složka** — otevře adresář vault souboru v Průzkumníku
- **.gitignore** — přidá cestu k vaultu do nejbližšího `.gitignore` souboru

### Import tokenů

Tlačítkem **Import** lze načíst tokeny ze souboru. Soubor může být JSON nebo prostý text s páry `KLÍČ=HODNOTA`. Duplikátní ID jsou přeskočena nebo (volitelně) přepsána.

---

## 13. Témata a splash screen

Téma změníš v **Nastavení**. K dispozici jsou dvě témata:

**Legacy** — jednodušší klasické rozhraní s emoji ikonami v tlačítkách. Při startu aplikace se zobrazí ASCII art splash screen v pravém panelu, který zmizí po kliknutí na **OpenClaw TUI**.

**Modern** — bitmap ikony v tlačítkách, splash panel při startu. Splash se chová takto:
1. Přehraje se `splash.mp4` (pokud existuje v `Resources/`)
2. Po doběhnutí videa zůstane statický `splash.png` jako freeze frame
3. Overlay zmizí po kliknutí na **OpenClaw TUI**

Pokud `splash.mp4` neexistuje nebo selže přehrávání, zobrazí se rovnou `splash.png`.

**Přepnutí tématu za běhu** — změna se projeví okamžitě po uložení Nastavení bez restartu aplikace. Přepnutí Modern → Legacy: video se zastaví, splash overlay zmizí a zobrazí se ASCII art. Přepnutí Legacy → Modern: ASCII art zmizí a zobrazí se PNG splash.

Nastavení tématu se ukládá do `%APPDATA%\OpenClawManager\settings.json`.

---

## 14. Časté situace a řešení

### Gateway se nespustí

**Příznak:** Status bar zůstane na "spouští se..." déle než 3 minuty, pak se změní na "selhalo".

**Řešení:**
1. Otevři **Gateway log** a zkontroluj poslední řádky — hledej chybové hlášky
2. Zkontroluj v Nastavení zda je správný "openclaw příkaz"
3. Spusť `openclaw "doctor --fix"` tlačítkem **Opravit konfiguraci**
4. Zkus restartovat Gateway

### TUI se nezobrazí (prázdná černá plocha vpravo)

**Příznak:** Gateway běží, TUI tlačítko změní text na "Zastavit", ale terminál je prázdný.

**Řešení:**
1. Chvíli počkej — TUI se inicializuje ~2–3 sekundy po startu
2. Klikni do oblasti terminálu vpravo
3. Pokud pořád prázdné — zastav TUI tlačítkem a spusť znovu

### Latence se nezobrazují (pomlčky)

**Příčina:** Latence se měří z Gateway logu až po prvním requestu přes WebSocket.

**Řešení:** Napiš zprávu do TUI a počkej na odpověď — latence se začnou zobrazovat.

### Token Manager hlásí "vault nenalezen"

**Řešení:** Otevři Token Manager → klikni **Inicializovat vault**. Pokud ses přihlásil pod jiným Windows účtem, vault nelze dešifrovat — vytvoř nový a přidej tokeny znovu.

### Token Manager zobrazí varování o rizikovém umístění

**Řešení:** Přesuň vault do bezpečné složky: otevři Nastavení → změň cestu k Token Manager vaultu → uložit. Token Manager se automaticky obnoví.

### Aplikace hlásí "Složka neexistuje"

**Řešení:** Otevři Nastavení (Ctrl+,) a oprav cestu k příslušné složce tlačítkem Procházet...

### Gateway log je prázdný nebo nenalezen

**Příčina:** Gateway nebyl spuštěn přes tuto aplikaci. Log soubor se vytváří v `%LOCALAPPDATA%\Temp\openclaw\`.

**Řešení:** Spusť Gateway přes tlačítko Start nebo přes hlavní TUI tlačítko.

### Po restartu PC Gateway neběží

**Příčina:** Scheduled Task "OpenClaw Gateway" je zakázaný nebo nebyl vytvořen.

**Řešení:** Otevři Cleaning Tool → sekce "OpenClaw Gateway" → Enable Scheduled Task. Nebo spusť Gateway ručně přes Start tlačítko.

---

**Konec dokumentu v2.0**
