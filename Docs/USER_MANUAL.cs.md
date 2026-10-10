# Uživatelský manuál - OpenClaw Manager Tool by Bloom

Česky | [English version](USER_MANUAL.md) | [HTML verze](USER_MANUAL.cs.html)

**Verze aplikace:** 2.0.5
**Aktualizováno:** 4. července 2026

## Obsah

1. [Přehled](#1-přehled)
2. [Instalace a spuštění](#2-instalace-a-spuštění)
3. [Témata](#3-témata)
4. [Hlavní okno](#4-hlavní-okno)
5. [Ovládání Gateway](#5-ovládání-gateway)
6. [Token Manager](#6-token-manager)
7. [Cleaning Tool](#7-cleaning-tool)
8. [Nastavení](#8-nastavení)
9. [Pošta a kalendář](#9-pošta-a-kalendář)
10. [Řešení potíží](#10-řešení-potíží)

## 1. Přehled

OpenClaw Manager je desktopový nástroj pro Windows, který pomáhá spravovat a diagnostikovat lokální prostředí OpenClaw. Umí spustit TUI, ovládat Gateway, zobrazit logy, měřit latenci, čistit dočasné soubory a spravovat lokální tokeny.

Nástroj slouží pro lokální údržbu. Nenahrazuje samotný OpenClaw a pro otevření této nápovědy nepotřebuje internet.

## 2. Instalace a spuštění

### Instalovaná verze

Použij instalátor a spusť aplikaci ze Start menu nebo ze zástupce na ploše. Instalovaná verze ukládá nastavení do standardního umístění aplikací ve Windows.

### Přenosná verze

Rozbal portable ZIP do zapisovatelné složky. Přenosná verze používá lokální `settings.json` vedle spustitelného souboru, takže nastavení zůstává v rozbalené složce.

### První spuštění

Uvítací obrazovka umožní zvolit jazyk, téma a kontrolu aktualizací při startu. Zavření uvítací obrazovky před dokončením ukončí celou aplikaci.

## 3. Témata

Aplikace používá sedm interních identifikátorů témat. V uživatelském rozhraní se zobrazují lokalizované názvy.

| Interní ID | Český název | Poznámka |
|---|---|---|
| `Legacy` | Legacy (výchozí) | Klasický výchozí vzhled. Toto téma neměnit bez výslovného schválení. |
| `StandardLight` | Standardní | Světlé standardní rozhraní. |
| `StandardDark` | Standardní tmavé | Tmavé standardní rozhraní. |
| `ModernDark` | Moderní tmavé | Moderní tmavé rozhraní se skleněným stylem. |
| `ModernLight` | Moderní | Moderní světlé rozhraní. |
| `HighContrast` | Vysoký kontrast | Vyšší kontrast pro lepší čitelnost. |
| `CrabCute` | Crab Cute | Hravá vizuální varianta. |

Lite verze záměrně nabízí pouze téma Legacy a neobsahuje video splash soubor.

## 4. Hlavní okno

Hlavní okno je rozdělené na akce, nástroje, informace o latenci, aplikační log a terminálový výstup.

| Oblast | Účel |
|---|---|
| Akce | Spuštění OpenClaw TUI a ovládání Gateway. |
| Nástroje | Otevření PowerShellu, Gateway logu, Cleaning Tool, Token Manageru a Doctor Fix. |
| Latence | Zobrazuje poslední, průměrnou a maximální hodnotu a počet měření. |
| Log aplikace | Zobrazuje lokální události a diagnostické zprávy aplikace. |
| Terminál | Vestavěná plocha pro výstup OpenClaw. |

### Klávesové zkratky

- `Ctrl+,` otevře Nastavení.
- `F1` otevře tento lokální manuál.
- `Alt+F4` zavře aktivní okno.

## 5. Ovládání Gateway

Tlačítka Gateway umí spustit, zastavit nebo restartovat lokální Gateway proces. Stavový řádek ukazuje, zda Gateway běží, včetně ID procesu a dostupné doby běhu.

Pokud se Gateway nespustí, otevři Nastavení a ověř příkaz OpenClaw, pracovní složku a cestu ke složce logů.

## 6. Token Manager

Token Manager ukládá a upravuje lokální tokeny poskytovatelů. Citlivé hodnoty jsou chráněné pomocí ochrany dat Windows, pokud je dostupná. V přenosném režimu se hranice ochrany mohou lišit, protože nastavení je uložené vedle spustitelného souboru.

Zálohy uchovávej bezpečně. Kdo získá přístup k exportovaným nebo zkopírovaným tokenům, může je zneužít.

## 7. Cleaning Tool

Cleaning Tool odstraňuje vybrané dočasné soubory, logy a zastaralá lokální data. Před spuštěním čištění zkontroluj výběr položek, zejména při práci z portable složky.

## 8. Nastavení

Nastavení spravuje cesty, téma, jazyk, chování při startu a volbu kontroly aktualizací. Pokud se aplikace po změně tématu nebo cest chová nečekaně, jednou ji restartuj a hodnoty znovu ověř.

## 9. Pošta a kalendář

Otevře se z hlavního menu: **Otevřít > Pošta a kalendář...**. Okno je ovládací panel automatického zpracování pošty a kalendáře pro agenty OpenClaw. Aplikace sama **žádná** pravidla nevynucuje: všechny limity, pravidla a potvrzování vynucuje backend (složka `control` v instalaci pošty a kalendáře, výchozí `E:\OpenClaw`). Okno jen ukazuje stav a žádá backend o změnu.

### Pruh se stavem odesílání

Pruh na horním okraji okna je vidět vždy:

| Pruh | Význam |
|---|---|
| Klidný: „Nic neodchází – stínový režim“ | Odpovědi se jen připravují jako soubory `.eml`. Nic se neposílá. |
| Klidný: „Nic neodchází – odesílání je vypnuté“ | Skutečné odesílání je vypnuté. |
| **Výrazný: „ODESÍLÁNÍ ZAPNUTO“** | Agent může skutečně odesílat poštu. |
| Výrazný: scope „open“ | Agent smí odpovídat každému ověřenému odesílateli (nejen z allowlistu). |

### Oddíly

| Oddíl | K čemu je |
|---|---|
| Přehled | Schránka, počty, statistika a poslední zpracované zprávy. |
| Nastavení | Formulář generovaný ze schématu backendu: každé nastavení s rozsahem, jednotkou, výchozí hodnotou a zdrojem. Nové nastavení backendu se objeví bez úpravy aplikace. |
| Pravidla odesílatelů | Pravidla podle odesílatele nebo domény (cesta a autonomie). |
| Návrhy | Otevřené návrhy (události z minulosti, platby, koncepty odpovědí, naplánované odpovědi). Koncept lze před schválením upravit. |
| Odchozí | Připravené soubory `.eml` s náhledem jen pro čtení; smazat jde jen `auto-*.eml` a `reply-*.eml`. |
| Příjemci a šablony | Allowlist příjemců, důvěryhodní odesílatelé událostí a šablony odpovědí. |
| Zdraví a úlohy | Hashe skriptů proti připnutí, Ollama, pluginy a úlohy plánovače. Pomalé (desítky sekund), výsledek se 30 s drží v paměti. |
| Historie a audit | Historie změn nastavení (s návratem), pokusy o odeslání a audit. |

### Potvrzování a bezpečnost

- Změnu, která rozšiřuje, co agent smí (skutečné odesílání, vypnutí stínového režimu, scope „open“, autonomie „free“, nový příjemce nebo důvěryhodný odesílatel, platba s nálezem), potvrzuje **backend nativním dialogem Windows**. Aplikace jen čeká („Čeká se na potvrzení v dialogu Windows“) a ukáže výsledek: uloženo, zamítnuto, nebo „nešlo potvrdit“. Když zvolíš Ne nebo do 2 minut neodpovíš, nic se nezmění.
- Před takovou změnou pole ukáže varování, že se přesouvá od **bezpečné hodnoty**. Návrat na bezpečnou hodnotu potvrzení nikdy nevyžaduje (tlačítko „Bezpečná hodnota“).
- Platba s nálezem je červeně a vyžaduje **druhý výslovný krok** (zaškrtnout potvrzení, potom „Přesto uložit“).
- Zamčené soubory (`gcal-config.json`, `mail-owner.json` nebo soubory zamčené `pin.ps1`) jsou jen pro čtení. Mění se v terminálu přes `pin.ps1 -Unlock`.
- Předměty, jména a koncepty pocházejí z e-mailů nebo od modelu. Zobrazují se jako prostý text a nikdy se nespouštějí. Aplikace nikdy nečte složku `credentials` a nezobrazuje heslo aplikace (jen to, zda je nastavené).
- Popisky a popisy nastavení pocházejí z backendu, proto se zobrazují v jazyce backendu.

### Umístění backendu a simulátor

Kořen instalace je v `settings.json` jako `MailRootPath` (zjistí se automaticky). Když se backend nenajde, okno nabídne **Vybrat složku...**. Pro vývoj bez Pythonu nastav před spuštěním aplikace proměnnou prostředí `OPENCLAW_MAILCONTROL_FAKE` na `decline`, `accept` nebo `needs`: okno pak běží nad ukázkovými daty a ukazuje červený pruh „SIMULÁTOR“.

## 10. Řešení potíží

| Problém | Co zkusit |
|---|---|
| Uvítací obrazovka nezobrazuje video | Lite verze video záměrně neobsahuje. Full verze při chybě načtení videa přejde na statický obrázek. |
| Nápověda se neotevře | Zkontroluj, že vedle aplikace existuje složka `Docs` a obsahuje `USER_MANUAL.cs.html`. |
| Gateway zůstává offline | Ověř příkaz Gateway a otevři Gateway log v části Nástroje. |
| Nastavení se neukládá | Instalovaná verze potřebuje přístup k aplikačním datům. Portable verze potřebuje zápis do rozbalené složky. |
