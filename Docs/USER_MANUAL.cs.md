# Uživatelský manuál - OpenClaw Manager Tool by Bloom

Česky | [English version](USER_MANUAL.md) | [HTML verze](USER_MANUAL.cs.html)

**Verze aplikace:** 2.0.3
**Aktualizováno:** 3. června 2026

## Obsah

1. [Přehled](#1-přehled)
2. [Instalace a spuštění](#2-instalace-a-spuštění)
3. [Témata](#3-témata)
4. [Hlavní okno](#4-hlavní-okno)
5. [Ovládání Gateway](#5-ovládání-gateway)
6. [Token Manager](#6-token-manager)
7. [Cleaning Tool](#7-cleaning-tool)
8. [Nastavení](#8-nastavení)
9. [Řešení potíží](#9-řešení-potíží)

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

## 9. Řešení potíží

| Problém | Co zkusit |
|---|---|
| Uvítací obrazovka nezobrazuje video | Lite verze video záměrně neobsahuje. Full verze při chybě načtení videa přejde na statický obrázek. |
| Nápověda se neotevře | Zkontroluj, že vedle aplikace existuje složka `Docs` a obsahuje `USER_MANUAL.cs.html`. |
| Gateway zůstává offline | Ověř příkaz Gateway a otevři Gateway log v části Nástroje. |
| Nastavení se neukládá | Instalovaná verze potřebuje přístup k aplikačním datům. Portable verze potřebuje zápis do rozbalené složky. |
