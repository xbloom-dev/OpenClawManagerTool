# UĹľivatelskĂ˝ manuĂˇl â€” OpenClaw Manager Tool by Bloom

*ÄŚeĹˇtina Â· [English version](USER_MANUAL.md)*

**Verze aplikace:** v2.0
**Datum:** 25. kvÄ›tna 2026

---

## Obsah

1. [Co je OpenClaw Manager](#1-co-je-openclaw-manager)
2. [Instalace a spuĹˇtÄ›nĂ­](#2-instalace-a-spuĹˇtÄ›nĂ­)
3. [TĂ©mata a splash screen](#3-tĂ©mata-a-splash-screen)
4. [HlavnĂ­ okno](#4-hlavnĂ­-okno)
5. [KlĂˇvesovĂ© zkratky](#5-klĂˇvesovĂ©-zkratky)
6. [SpuĹˇtÄ›nĂ­ OpenClaw TUI](#6-spuĹˇtÄ›nĂ­-openclaw-tui)
7. [ĹĂ­zenĂ­ Gateway](#7-Ĺ™Ă­zenĂ­-gateway)
8. [MÄ›Ĺ™enĂ­ latencĂ­](#8-mÄ›Ĺ™enĂ­-latencĂ­)
9. [Gateway log](#9-gateway-log)
10. [SprĂˇvce API klĂ­ÄŤĹŻ (Token Manager)](#10-sprĂˇvce-api-klĂ­ÄŤĹŻ-token-manager)
11. [Cleaning Tool â€” VyÄŤistit soubory](#11-cleaning-tool--vyÄŤistit-soubory)
12. [NastavenĂ­](#12-nastavenĂ­)
13. [O aplikaci](#13-o-aplikaci)
14. [ÄŚastĂ© situace a Ĺ™eĹˇenĂ­](#14-ÄŤastĂ©-situace-a-Ĺ™eĹˇenĂ­)

---

## 1. Co je OpenClaw Manager

OpenClaw Manager je diagnostickĂ˝ a ĂşdrĹľbovĂ˝ nĂˇstroj pro [OpenClaw](https://github.com/openclaw/openclaw) prostĹ™edĂ­. SlouĹľĂ­ hlavnÄ› tehdy, **kdyĹľ nÄ›co nefunguje sprĂˇvnÄ›** â€” umoĹľĹuje rychle restartovat Gateway, vyÄŤistit starĂ© soubory, zmÄ›Ĺ™it latence a diagnostikovat problĂ©m.

> **Co je OpenClaw?** OpenClaw je open-source AI-agent framework. Jeho **Gateway** je lokĂˇlnĂ­ dĂ©mon, kterĂ˝ propojuje agenty s AI poskytovateli (Anthropic, OpenAI, Ollamaâ€¦), a **TUI** je terminĂˇlovĂ˝ dashboard pro sledovĂˇnĂ­ session, logĹŻ a vyuĹľitĂ­ modelĹŻ. Tento nĂˇstroj je nezĂˇvislĂˇ nadstavba â€” spravuje existujĂ­cĂ­ instalaci OpenClaw pĹ™es grafickĂ© rozhranĂ­ a nenĂ­ oficiĂˇlnÄ› spojen s projektem OpenClaw.

**NenĂ­** to nĂˇhrada za kaĹľdodennĂ­ spouĹˇtÄ›nĂ­ OpenClaw â€” pro normĂˇlnĂ­ provoz staÄŤĂ­ zĂˇstupce nebo Scheduled Task. OpenClaw Manager se hodĂ­, kdyĹľ potĹ™ebujeĹˇ:

- Restartovat Gateway po update nebo havĂˇrii
- Zjistit jak rychle Gateway odpovĂ­dĂˇ (mÄ›Ĺ™enĂ­ latencĂ­)
- VyÄŤistit starĂ© logy a session soubory
- BezpeÄŤnÄ› spravovat API klĂ­ÄŤe a tokeny (vÄŤetnÄ› pĹ™enosnĂ© zĂˇlohy)
- Opravit poĹˇkozenou konfiguraci

---

## 2. Instalace a spuĹˇtÄ›nĂ­

### PoĹľadavky

- Windows 10 nebo 11 (x64)
- WebView2 Runtime (souÄŤĂˇst Windows 11; pro Windows 10 stĂˇhnout z microsoft.com)
- OpenClaw nainstalovanĂ˝ a funkÄŤnĂ­

### SpuĹˇtÄ›nĂ­

SpusĹĄ `OpenClawManager.exe` z runtime balĂ­ÄŤku. Ĺ˝ĂˇdnĂˇ instalace nenĂ­ potĹ™eba â€” aplikace je pĹ™enositelnĂˇ (portable). TerminĂˇl pouĹľĂ­vĂˇ lokĂˇlnĂ­ xterm.js soubory â€” internet ani CDN nejsou potĹ™eba.

### NastavenĂ­ cest pĹ™i prvnĂ­m spuĹˇtÄ›nĂ­

PĹ™i prvnĂ­m spuĹˇtÄ›nĂ­ zkontroluj v **NastavenĂ­** (Ctrl+,) Ĺľe cesty odpovĂ­dajĂ­ tvĂ©mu prostĹ™edĂ­:

| PoloĹľka | VĂ˝chozĂ­ hodnota |
|---|---|
| OpenClaw sloĹľka | `~\.openclaw` |
| Temp sloĹľka (Gateway logy) | `%LOCALAPPDATA%\Temp\openclaw` |
| openclaw pĹ™Ă­kaz | `openclaw` (PATH lookup) |
| PowerShell pracovnĂ­ adresĂˇĹ™ | `%APPDATA%\npm` |
| Trezor (vault) | `%USERPROFILE%\.token-manager\secrets.json` |

Pokud OpenClaw nespustĂ­Ĺˇ pĹ™es `openclaw` v PATH, uprav â€žopenclaw pĹ™Ă­kaz" na plnou cestu, napĹ™Ă­klad `C:\Users\jmeno\AppData\Roaming\npm\openclaw.cmd`.

---

## 3. TĂ©mata a splash screen

TĂ©ma zmÄ›nĂ­Ĺˇ v **NastavenĂ­**. OpenClaw Manager v2.0 nabĂ­zĂ­ **sedm tĂ©mat**:

| TĂ©ma | Popis |
|---|---|
| **Theme.Legacy** | KlasickĂ© jednoduĹˇĹˇĂ­ rozhranĂ­ s emoji ikonami v tlaÄŤĂ­tkĂˇch. Splash je ASCII art. |
| **Theme.StandardLight** | Bitmap ikony, tmavÄ› fialovĂ© splash pozadĂ­, tĂ©movanĂˇ sekundĂˇrnĂ­ okna. |
| **Theme.StandardDark** | TmavĂˇ varianta vĂ˝chozĂ­ho vzhledu. |
| **Theme.ModernDark** | TmavĂ© tĂ©ma s modernĂ­ sadou ikon. |
| **Theme.ModernLight** | SvÄ›tlĂ© modernĂ­ tĂ©ma. |
| **Theme.HighContrast** | VysokĂ˝ kontrast pro lepĹˇĂ­ ÄŤitelnost. |
| **Theme.CrabCute** | HravĂˇ sada ikon a barev. |

Theme.StandardLight / Theme.StandardDark / Theme.ModernDark / Theme.ModernLight pouĹľĂ­vajĂ­ bitmapovĂ© ikony a tĂ©movanĂˇ sekundĂˇrnĂ­ okna; Theme.Legacy zachovĂˇvĂˇ jednoduĹˇĹˇĂ­ klasickĂ˝ vzhled.

### Splash screen

ChovĂˇnĂ­ splash panelu pĹ™i startu zĂˇvisĂ­ na tĂ©matu:

**Theme.Legacy:** v pravĂ©m panelu se zobrazĂ­ ASCII art splash â€” zmizĂ­ po kliknutĂ­ na **OpenClaw TUI**.

**Theme.StandardLight (a odvozenĂˇ tĂ©mata):**
1. PĹ™ehraje se `splash.mp4` (pokud existuje v `Resources/`)
2. Po dobÄ›hnutĂ­ videa zĹŻstane statickĂ˝ `splash.png` jako freeze frame
3. Overlay zmizĂ­ po kliknutĂ­ na **OpenClaw TUI**

Pokud `splash.mp4` chybĂ­ nebo selĹľe pĹ™ehrĂˇvĂˇnĂ­, zobrazĂ­ se rovnou `splash.png`. Pokud chybĂ­ i `splash.png`, zobrazĂ­ se ASCII art jako fallback.

### PĹ™epĂ­nĂˇnĂ­ za bÄ›hu

ZmÄ›na tĂ©matu se projevĂ­ okamĹľitÄ› po uloĹľenĂ­ NastavenĂ­. PĹ™i pĹ™epnutĂ­ mezi splash styly (Theme.StandardLight â†” Theme.Legacy) pĹ™ed spuĹˇtÄ›nĂ­m TUI se video zastavĂ­ / ASCII art pĹ™epne podle novĂ©ho tĂ©matu.

---

## 4. HlavnĂ­ okno

Okno je rozdÄ›leno na dvÄ› ÄŤĂˇsti:

**LevĂ˝ panel (330 px)** â€” ovlĂˇdĂˇnĂ­, mÄ›Ĺ™enĂ­ latencĂ­, log aplikace
**PravĂ˝ panel** â€” embedded terminĂˇl (OpenClaw TUI)

### Sekce Akce

#### TlaÄŤĂ­tko OpenClaw TUI

HlavnĂ­ tlaÄŤĂ­tko aplikace. MĂˇ **3 reĹľimy** podle aktuĂˇlnĂ­ho stavu:

| Stav | Barva | Co udÄ›lĂˇ |
|---|---|---|
| Gateway nebÄ›ĹľĂ­ | đźź˘ zelenĂˇ | SpustĂ­ Gateway + poÄŤkĂˇ na ready + spustĂ­ TUI |
| Gateway bÄ›ĹľĂ­ | đźź˘ zelenĂˇ | SpustĂ­ jen TUI (Gateway nechĂˇ bÄ›Ĺľet) |
| TUI bÄ›ĹľĂ­ | đź”´ ÄŤervenĂˇ | ZastavĂ­ TUI (Gateway nechĂˇ bÄ›Ĺľet) |

#### Gateway tlaÄŤĂ­tka

- **Start** â€” spustĂ­ `openclaw gateway` na pozadĂ­ v novĂ©m PowerShell oknÄ›
- **Stop** â€” zastavĂ­ bÄ›ĹľĂ­cĂ­ Gateway (zobrazĂ­ potvrzovacĂ­ dialog)
- **Restart** â€” zastavĂ­ a znovu spustĂ­ Gateway; TUI se odpojĂ­

#### OtevĹ™Ă­t

- **PowerShell** â€” otevĹ™e PowerShell v pracovnĂ­m adresĂˇĹ™i nastavenĂ©m v NastavenĂ­
- **Gateway log** â€” otevĹ™e dialog s vĂ˝pisem Gateway logu
- **Ĺ˝ivĂˇ data** (Ctrl+L) â€” otevĹ™e ĹľivĂ© sledovĂˇnĂ­ Gateway logu

#### NĂˇstroje

- **VyÄŤistit soubory** â€” otevĹ™e Cleaning Tool
- **SprĂˇvce API klĂ­ÄŤĹŻ** â€” sprĂˇva trezoru tokenĹŻ a API klĂ­ÄŤĹŻ

#### ĂšdrĹľba

- **Opravit konfiguraci** â€” spustĂ­ `openclaw "doctor --fix"` v PowerShellu

### Status bar (dole)

`Gateway: â—Ź stav | PID: X | uptime: H:MM:SS | RAM: X/Y GB | VRAM: X/Y GB | CPU: X% | v2.0`

| Barva teÄŤky | Stav |
|---|---|
| đźź˘ zelenĂˇ | Gateway bÄ›ĹľĂ­ |
| đźź  oranĹľovĂˇ | Gateway se spouĹˇtĂ­ |
| đź”´ ÄŤervenĂˇ | Gateway selhalo |
| âš« ĹˇedĂˇ | Gateway nebÄ›ĹľĂ­ |

RAM, CPU a VRAM se aktualizujĂ­ kaĹľdĂ© 2 sekundy na pozadĂ­ â€” UI se nezasekĂˇvĂˇ.

---

## 5. KlĂˇvesovĂ© zkratky

| Zkratka | Akce |
|---|---|
| **Ctrl+T** | Start/Stop OpenClaw TUI |
| **Ctrl+G** | Start nebo Stop Gateway |
| **Ctrl+R** | Restart Gateway |
| **Ctrl+L** | Ĺ˝ivĂˇ data Gateway logu |
| **Ctrl+Shift+C** | OtevĹ™Ă­t VyÄŤistit soubory |
| **Ctrl+,** | OtevĹ™Ă­t NastavenĂ­ |
| **F1** | O aplikaci |
| **Alt+F4** | Konec |

---

## 6. SpuĹˇtÄ›nĂ­ OpenClaw TUI

### StandardnĂ­ postup

1. Klikni na tlaÄŤĂ­tko **â–¶ OpenClaw TUI** (nebo Ctrl+T)
2. Aplikace automaticky:
   - SmaĹľe starĂ˝ Gateway log (pro ÄŤistĂ© mÄ›Ĺ™enĂ­ latencĂ­)
   - SpustĂ­ Gateway
   - ÄŚekĂˇ na â€žgateway ready" (max 3 minuty)
   - SpustĂ­ TUI v embedded terminĂˇlu vpravo
3. PrĹŻbÄ›h sleduj v â€žLog aplikace" vlevo dole

### Pokud Gateway jiĹľ bÄ›ĹľĂ­

KliknutĂ­ spustĂ­ TUI pĹ™Ă­mo bez restartu Gateway.

### ZastavenĂ­ TUI

Klikni na tlaÄŤĂ­tko **â–  Zastavit OpenClaw TUI** (ÄŤervenĂ©). Gateway zĹŻstane bÄ›Ĺľet.

### ZavĹ™enĂ­ aplikace s bÄ›ĹľĂ­cĂ­m Gateway

PĹ™i zavĹ™enĂ­ se zobrazĂ­ dialog se tĹ™emi moĹľnostmi:
- **Ano** â€” zastavĂ­ Gateway a zavĹ™e aplikaci
- **Ne** â€” zavĹ™e aplikaci, Gateway nechĂˇ bÄ›Ĺľet na pozadĂ­
- **ZruĹˇit** â€” vrĂˇtĂ­ se zpÄ›t do aplikace

---

## 7. ĹĂ­zenĂ­ Gateway

### SpuĹˇtÄ›nĂ­ Gateway

TlaÄŤĂ­tko **Start** nebo Ctrl+G. Gateway se spustĂ­ v novĂ©m PowerShell oknÄ›. Aplikace ÄŤekĂˇ na â€žgateway ready" â€” status bar zobrazuje â€žspouĹˇtĂ­ se..." dokud Gateway nenĂ­ pĹ™ipraven.

### ZastavenĂ­ Gateway

TlaÄŤĂ­tko **Stop** nebo Ctrl+G (pokud Gateway bÄ›ĹľĂ­). ZobrazĂ­ se potvrzovacĂ­ dialog.

**PoznĂˇmka:** ZastavenĂ­ Gateway pĹ™eruĹˇĂ­ vĹˇechny aktivnĂ­ TUI sessions. Scheduled Task pro automatickĂ© spouĹˇtÄ›nĂ­ pĹ™i pĹ™ihlĂˇĹˇenĂ­ se **nezmÄ›nĂ­** â€” Gateway se znovu spustĂ­ pĹ™i pĹ™Ă­ĹˇtĂ­m pĹ™ihlĂˇĹˇenĂ­.

### Restart Gateway

TlaÄŤĂ­tko **Restart** nebo Ctrl+R. TUI se odpojĂ­, Gateway se zastavĂ­ a znovu spustĂ­. Po ĂşspÄ›ĹˇnĂ©m restartu se TUI automaticky znovu spustĂ­.

---

## 8. MÄ›Ĺ™enĂ­ latencĂ­

Sekce **MÄ›Ĺ™enĂ­ latence** v levĂ©m panelu zobrazuje rychlost odpovÄ›dĂ­ Gateway:

| Hodnota | Popis |
|---|---|
| PoslednĂ­ | Latence poslednĂ­ho requestu |
| PrĹŻmÄ›r 10Ă— | KlouzavĂ˝ prĹŻmÄ›r poslednĂ­ch 10 requestĹŻ |
| Maximum | NejvyĹˇĹˇĂ­ namÄ›Ĺ™enĂˇ latence od startu/restartu |
| RequestĹŻ | CelkovĂ˝ poÄŤet requestĹŻ od startu/restartu |

**Jak ÄŤĂ­st hodnoty:**
- đźź˘ zelenĂˇ â€” pod 1 000 ms (rychlĂˇ odpovÄ›ÄŹ)
- ÄŤernĂˇ â€” 1 000â€“5 000 ms (normĂˇlnĂ­)
- đź”´ ÄŤervenĂˇ â€” nad 5 000 ms (pomalĂˇ odpovÄ›ÄŹ, moĹľnĂˇ pĹ™etĂ­ĹľenĂ­)

Hodnoty se zaÄŤnou zobrazovat aĹľ po prvnĂ­m requestu odeslanĂ©m pĹ™es TUI.

---

## 9. Gateway log

OtevĹ™i pĹ™es **Gateway log** tlaÄŤĂ­tko nebo pĹ™es menu **OtevĹ™Ă­t â†’ Gateway log**.

### ZobrazenĂ­ logu

Vyber poÄŤet zobrazenĂ˝ch Ĺ™ĂˇdkĹŻ (vĂ˝chozĂ­: poslednĂ­ch 20) a klikni **Aktualizovat**. NahoĹ™e se zobrazuje cesta k souboru, velikost a poÄŤet Ĺ™ĂˇdkĹŻ.

### KopĂ­rovĂˇnĂ­ do schrĂˇnky

Klikni **KopĂ­rovat** â€” celĂ˝ obsah se zkopĂ­ruje do schrĂˇnky. Po kliknutĂ­ se na 2 sekundy zobrazĂ­ â€žâś“ ZkopĂ­rovĂˇno".

### Ĺ˝ivĂ© sledovĂˇnĂ­ (Ctrl+L)

Klikni **Ĺ˝ivĂˇ data** pro otevĹ™enĂ­ ĹľivĂ©ho okna:

- OtevĹ™e se vedle hlavnĂ­ aplikace (nezamkne ovlĂˇdĂˇnĂ­)
- Automaticky detekuje novĂ© zĂˇznamy
- NejstarĹˇĂ­ zĂˇznamy se prĹŻbÄ›ĹľnÄ› odstraĹujĂ­ (vĹľdy N poslednĂ­ch Ĺ™ĂˇdkĹŻ)
- NovĂ© Ĺ™Ăˇdky jsou oznaÄŤeny `â–ş ` po dobu 2 sekund
- TlaÄŤĂ­tko **KopĂ­rovat** zkopĂ­ruje ÄŤistĂ˝ obsah (bez `â–ş ` prefixĹŻ)

---

## 10. SprĂˇvce API klĂ­ÄŤĹŻ (Token Manager)

OtevĹ™i tlaÄŤĂ­tkem **SprĂˇvce API klĂ­ÄŤĹŻ** v levĂ©m panelu. SlouĹľĂ­ k bezpeÄŤnĂ©mu uklĂˇdĂˇnĂ­ API klĂ­ÄŤĹŻ a citlivĂ˝ch hodnot, k maskovĂˇnĂ­ souborĹŻ pĹ™ed jejich sdĂ­lenĂ­m a k pĹ™enosnĂ© zĂˇloze trezoru.

### Trezor (vault) a bezpeÄŤnost

Trezor je soubor `secrets.json` v cestÄ› nastavenĂ© v NastavenĂ­ (vĂ˝chozĂ­ `%USERPROFILE%\.token-manager\`). Hodnoty tokenĹŻ jsou ĹˇifrovanĂ© pĹ™es **Windows DPAPI** pro aktuĂˇlnĂ­ho uĹľivatele. Trezor zkopĂ­rovanĂ˝ na jinĂ˝ poÄŤĂ­taÄŤ nebo pod jinĂ˝ ĂşÄŤet nelze deĹˇifrovat.

SprĂˇvce API klĂ­ÄŤĹŻ zobrazĂ­ varovĂˇnĂ­, pokud trezor leĹľĂ­ v rizikovĂ©m umĂ­stÄ›nĂ­:
- uvnitĹ™ Git repozitĂˇĹ™e
- uvnitĹ™ `.openclaw`
- v cloud-synchronizovanĂ© sloĹľce (OneDrive, Dropbox, iCloud)
- ve sdĂ­lenĂ© nebo projektovĂ© sloĹľce

**DoporuÄŤenĂ­:** pouĹľĂ­vej `%USERPROFILE%\.token-manager\` mimo projekt, Git a cloud sync.

### Inicializace trezoru

PĹ™i prvnĂ­m otevĹ™enĂ­ klikni **Inicializovat trezor**. Pokud existuje starĹˇĂ­ neĹˇifrovanĂ˝ trezor, aplikace ho automaticky zmigruje na Ĺˇifrovanou verzi pĹ™i prvnĂ­m uloĹľenĂ­.

### PĹ™idĂˇnĂ­ a sprĂˇva tokenĹŻ

Klikni **PĹ™idat** a vyplĹ:
- **ID** â€” unikĂˇtnĂ­ identifikĂˇtor bez mezer (napĹ™. `OPENAI_KEY`) â€” pouĹľĂ­vĂˇ se jako zastupnĂ˝ text `[REDACTED_OPENAI_KEY]`
- **Hodnota** â€” samotnĂ˝ tajnĂ˝ klĂ­ÄŤ
- **Popis** â€” volitelnĂˇ poznĂˇmka

**Upravit** â€” nechej Hodnotu prĂˇzdnou pokud chceĹˇ zachovat stĂˇvajĂ­cĂ­ hodnotu.
**Rotovat** â€” zadĂˇ novou hodnotu bez ztrĂˇty ID a popisu.
**Odstranit** â€” odstranĂ­ token z trezoru.

### ZĂˇloha a obnova trezoru (`.ocvault`)

Trezor je svĂˇzanĂ˝ s tvĂ˝m Windows profilem (DPAPI), takĹľe ho nejde jen tak zkopĂ­rovat na jinĂ˝ poÄŤĂ­taÄŤ. Pro pĹ™enos nebo bezpeÄŤnostnĂ­ zĂˇlohu slouĹľĂ­ pĹ™enosnĂ˝ formĂˇt `.ocvault`:

- **Backup (ZĂˇloha)** â€” exportuje trezor do souboru `.ocvault` chrĂˇnÄ›nĂ©ho **heslem**, kterĂ© zadĂˇĹˇ. Soubor je ĹˇifrovanĂ˝ pĹ™es **PBKDF2-SHA256** (200 000 iteracĂ­) a **AES-256-GCM** â€” nezĂˇvisle na DPAPI, takĹľe ho lze obnovit i na jinĂ©m poÄŤĂ­taÄŤi.
- **Restore (Obnova)** â€” naÄŤte `.ocvault` po zadĂˇnĂ­ hesla a znovu ho uloĹľĂ­ do lokĂˇlnĂ­ho DPAPI trezoru pro aktuĂˇlnĂ­ Windows profil.

Heslo zadĂˇvĂˇĹˇ v samostatnĂ©m dialogu (`PasswordPromptWindow`). PĹ™i exportu i importu se pouĹľijĂ­ Save/Open dialogy pro vĂ˝bÄ›r `.ocvault` souboru.

> âš ď¸Ź **BezpeÄŤnost zĂˇlohy:** `.ocvault` soubor a jeho heslo uchovĂˇvej oddÄ›lenÄ› a na bezpeÄŤnĂ©m mĂ­stÄ›. Kdokoliv s obÄ›ma zĂ­skĂˇ pĹ™Ă­stup k tvĂ˝m tokenĹŻm. ZĂˇlohu neuklĂˇdej do Git repozitĂˇĹ™e ani do cloud sync sloĹľky vedle hesla.

### MaskovĂˇnĂ­ a obnova souborĹŻ

**Workflow maskovĂˇnĂ­:**
1. Vyber token a klikni **NĂˇhled** â€” zobrazĂ­ jak bude soubor vypadat po maskovĂˇnĂ­
2. Klikni **[REDACT]** â€” nahradĂ­ hodnoty zastupnĂ˝m textem `[REDACTED_ID]`
3. PĹ™ed odeslĂˇnĂ­m klikni **OvÄ›Ĺ™it** â€” zkontroluje Ĺľe soubor neobsahuje ĹľĂˇdnou plaintext hodnotu

**Obnova:** klikni **Obnovit** â€” nahradĂ­ zastupnĂ© texty zpÄ›t hodnotami. Automaticky se vytvoĹ™Ă­ zĂˇloĹľnĂ­ `.bak` soubor.

### TlaÄŤĂ­tka pĹ™ehled

| TlaÄŤĂ­tko | Barva | Funkce |
|---|---|---|
| **[REDACT]** | đźź˘ zelenĂ©, tuÄŤnĂ© | Maskuje citlivĂ© hodnoty v souboru |
| **OvÄ›Ĺ™it** | đź”µ modrĂ©, tuÄŤnĂ© | OvÄ›Ĺ™Ă­ Ĺľe soubor neobsahuje plaintext hodnotu |
| **NĂˇhled** | đź”µ modrĂ© | ZobrazĂ­ nĂˇhled maskovĂˇnĂ­ bez zĂˇpisu |
| **ProchĂˇzet** | đź”µ modrĂ© | VĂ˝bÄ›r souboru |
| **Backup** | đź”µ modrĂ© | Exportuje trezor do `.ocvault` chrĂˇnÄ›nĂ©ho heslem |
| **Restore** | đź”µ modrĂ© | ObnovĂ­ trezor z `.ocvault` |
| **Obnovit** | đź”´ ÄŤervenĂ© | ObnovĂ­ zastupnĂ© texty na hodnoty |
| **ZavĹ™Ă­t** | đź”´ ÄŤervenĂ© | ZavĹ™e okno |

### PomocnĂ© funkce

- **ZastupnĂ˝ text** â€” zkopĂ­ruje `[REDACTED_ID]` do schrĂˇnky pro ruÄŤnĂ­ vloĹľenĂ­
- **SloĹľka** â€” otevĹ™e adresĂˇĹ™ trezoru v PrĹŻzkumnĂ­ku
- **.gitignore** â€” pĹ™idĂˇ cestu k trezoru do nejbliĹľĹˇĂ­ho `.gitignore`
- **Import** â€” naÄŤte tokeny ze souboru (JSON nebo `KLĂŤÄŚ=HODNOTA`)

---

## 11. Cleaning Tool â€” VyÄŤistit soubory

OtevĹ™i pĹ™es tlaÄŤĂ­tko **VyÄŤistit soubory** nebo Ctrl+Shift+C.

### Co lze vyÄŤistit

| Krok | Co maĹľe | VĂ˝chozĂ­ |
|---|---|---|
| 1 â€” Gateway logy | StarĂ© log soubory (ne dneĹˇnĂ­) | âś… zapnuto |
| 2 â€” ZĂˇlohy konfigurace | `.bak` soubory (ponechĂˇ 2 nejnovÄ›jĹˇĂ­) | âś… zapnuto |
| 3 â€” Stability logy | Logy starĹˇĂ­ neĹľ 3 dny | âťŚ vypnuto |
| 4 â€” Browser cache | Cache starĹˇĂ­ neĹľ 1 den | âś… zapnuto |
| 5 â€” Session locky | ZĂˇmkovĂ© soubory sessions | âś… zapnuto |
| 6 â€” sessions.json | StarĂˇ session data (ponechĂˇ N nejnovÄ›jĹˇĂ­ch) | âś… zapnuto |
| 7 â€” ZĂˇlohy trezoru | `*.bak` v Token Manager sloĹľce | âťŚ vypnuto (opt-in) |

PosuvnĂ­kem u kroku 6 nastav kolik sessions zachovat (vĂ˝chozĂ­: 10). Krok 7 je zĂˇmÄ›rnÄ› vypnutĂ˝ â€” zĂˇlohy trezoru jsou zĂˇchrana pĹ™i selhĂˇnĂ­ obnovy.

### DoporuÄŤenĂ˝ postup

1. Klikni **NĂˇhled** (modrĂ© tlaÄŤĂ­tko) â€” zobrazĂ­ co by se smazalo, nic neudÄ›lĂˇ
2. Zkontroluj vĂ˝pis
3. Klikni **Spustit** (zelenĂ© tlaÄŤĂ­tko) â€” skuteÄŤnĂ© smazĂˇnĂ­

> âš ď¸Ź **UpozornÄ›nĂ­:** Spustit trvale smaĹľe vybranĂ© soubory. Akci nelze vrĂˇtit.

---

## 12. NastavenĂ­

OtevĹ™i pĹ™es menu **NastavenĂ­ â†’ OtevĹ™Ă­t NastavenĂ­...** nebo Ctrl+,.

### Jazyk / Language

PĹ™epni mezi **ÄŚeĹˇtina** a **English**. ZmÄ›na se projevĂ­ po uloĹľenĂ­.

### Cesty

| Pole | Popis |
|---|---|
| OpenClaw sloĹľka | Kde OpenClaw uklĂˇdĂˇ konfiguraci (`~\.openclaw`) |
| Temp sloĹľka | Kde jsou uloĹľeny Gateway logy |
| openclaw pĹ™Ă­kaz | PĹ™Ă­kaz nebo cesta k `openclaw` spustitelnĂ©mu souboru |
| PowerShell pracovnĂ­ adresĂˇĹ™ | AdresĂˇĹ™ kde se otevĹ™e PowerShell |
| Trezor (vault) | Cesta k `secrets.json` â€” ĹˇifrovĂˇno pĹ™es Windows DPAPI |

TlaÄŤĂ­tka **ProchĂˇzet...** otevĹ™ou dialog pro vĂ˝bÄ›r sloĹľky. Pole â€žopenclaw pĹ™Ă­kaz" validuje zakĂˇzanĂ© znaky â€” hodnota se zakĂˇzanĂ˝mi shell znaky (`<`, `>`, `%`, `^`, `&`, `|`) nejde uloĹľit.

### TĂ©ma

PĹ™epni mezi sedmi tĂ©maty (Theme.Legacy, Theme.StandardLight, Theme.StandardDark, Theme.ModernDark, Theme.ModernLight, Theme.HighContrast, Theme.CrabCute). ZmÄ›na se projevĂ­ okamĹľitÄ› â€” viz kapitola [3. TĂ©mata a splash screen](#3-tĂ©mata-a-splash-screen).

### TlaÄŤĂ­tka

- **UloĹľit** â€” uloĹľĂ­ a zavĹ™e
- **Reset na vĂ˝chozĂ­** â€” obnovĂ­ vĂ˝chozĂ­ hodnoty (vyĹľaduje potvrzenĂ­)
- **ZruĹˇit** â€” zavĹ™e bez uloĹľenĂ­

NastavenĂ­ se uklĂˇdĂˇ do `%APPDATA%\OpenClawManager\settings.json`.

---

## 13. O aplikaci

OtevĹ™i pĹ™es menu **NĂˇpovÄ›da â†’ O aplikaci...** nebo F1. Zobrazuje logo, verzi, technickĂ˝ stack a pĹ™ehled klĂˇvesovĂ˝ch zkratek.

---

## 14. ÄŚastĂ© situace a Ĺ™eĹˇenĂ­

### Gateway se nespustĂ­

**PĹ™Ă­znak:** Status bar zĹŻstane na â€žspouĹˇtĂ­ se..." dĂ©le neĹľ 3 minuty.

**ĹeĹˇenĂ­:**
1. OtevĹ™i **Gateway log** a zkontroluj poslednĂ­ Ĺ™Ăˇdky
2. Zkontroluj v NastavenĂ­ zda je sprĂˇvnĂ˝ â€žopenclaw pĹ™Ă­kaz"
3. SpusĹĄ **Opravit konfiguraci**
4. Zkus restartovat Gateway

### TUI se nezobrazĂ­ (prĂˇzdnĂˇ ÄŤernĂˇ plocha vpravo)

**ĹeĹˇenĂ­:**
1. PoÄŤkej 2â€“3 sekundy â€” TUI se inicializuje
2. Klikni do oblasti terminĂˇlu vpravo
3. Pokud poĹ™Ăˇd prĂˇzdnĂ© â€” zastav TUI a spusĹĄ znovu

### Latence se nezobrazujĂ­ (pomlÄŤky)

**PĹ™Ă­ÄŤina:** Latence se mÄ›Ĺ™Ă­ aĹľ po prvnĂ­m requestu odeslanĂ©m v TUI.

**ĹeĹˇenĂ­:** NapiĹˇ zprĂˇvu do TUI a poÄŤkej na odpovÄ›ÄŹ.

### SprĂˇvce API klĂ­ÄŤĹŻ hlĂˇsĂ­ â€žtrezor nenalezen"

**ĹeĹˇenĂ­:** Klikni **Inicializovat trezor**. Pokud ses pĹ™ihlĂˇsil pod jinĂ˝m Windows ĂşÄŤtem, trezor nelze deĹˇifrovat â€” obnov ho ze zĂˇlohy `.ocvault` (Restore) nebo vytvoĹ™ novĂ˝ a pĹ™idej tokeny znovu.

### Obnova `.ocvault` hlĂˇsĂ­ ĹˇpatnĂ© heslo

**PĹ™Ă­ÄŤina:** Heslo neodpovĂ­dĂˇ tomu, kterĂ˝m byla zĂˇloha vytvoĹ™ena.

**ĹeĹˇenĂ­:** Zkontroluj heslo. `.ocvault` nelze obnovit bez sprĂˇvnĂ©ho hesla â€” ĹľĂˇdnĂˇ zadnĂ­ vrĂˇtka neexistujĂ­.

### SprĂˇvce API klĂ­ÄŤĹŻ zobrazĂ­ varovĂˇnĂ­ o rizikovĂ©m umĂ­stÄ›nĂ­

**ĹeĹˇenĂ­:** OtevĹ™i NastavenĂ­ â†’ zmÄ›Ĺ cestu k trezoru â†’ uloĹľit.

### Aplikace hlĂˇsĂ­ â€žSloĹľka neexistuje"

**ĹeĹˇenĂ­:** OtevĹ™i NastavenĂ­ (Ctrl+,) a oprav cestu tlaÄŤĂ­tkem **ProchĂˇzet...**

### Gateway log je prĂˇzdnĂ˝ nebo nenalezen

**PĹ™Ă­ÄŤina:** Gateway nebyl spuĹˇtÄ›n pĹ™es tuto aplikaci.

**ĹeĹˇenĂ­:** SpusĹĄ Gateway pĹ™es tlaÄŤĂ­tko **Start** nebo pĹ™es hlavnĂ­ TUI tlaÄŤĂ­tko.

### Po restartu PC Gateway nebÄ›ĹľĂ­

**PĹ™Ă­ÄŤina:** Scheduled Task â€žOpenClaw Gateway" je zakĂˇzanĂ˝ nebo nebyl vytvoĹ™en.

**ĹeĹˇenĂ­:** OtevĹ™i Cleaning Tool â†’ sekce â€žOpenClaw Gateway" â†’ Enable Scheduled Task.

---

**Konec dokumentu â€” verze aplikace v2.0**


