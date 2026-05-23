# OpenClaw Manager — Barevný styl v1.1

## Účel

Barvy tlačítek nejsou dekorace — používají se jako rychlá vizuální orientace:

- **zelená** = hlavní pozitivní akce
- **červená** = zavřít, zrušit, obnovit nebo rizikovější akce
- **modrá** = pomocné technické akce, náhled, kopírování, ověření, procházení

Od v1.1 jsou barvy definovány jako **theme resource tokeny** v `Resources/Themes/Theme.*.xaml`.
Code-behind přistupuje přes `ThemeService.GetBrush("Brush.ActionPositive", fallback)`.

---

## Sémantické tokeny (sdílené napříč tématy)

| Token | Modern (světlé) | Modern Dark | Popis |
|---|---|---|---|
| `Brush.ActionPositive` | `#D0FFD0` | `#1C3527` | pozitivní/spouštěcí akce |
| `Brush.ActionDanger` | `#FFD0D0` | `#3D1515` | zavřít, zrušit, obnovit, opravit |
| `Brush.ActionUtility` | `#D0E8FF` | `#152535` | kopírovat, náhled, reset, procházet |
| `Brush.SplashModernBackground` | `#4C247E` | `#181818` | pozadí splash obrazovky |

---

## Paleta Modern (Standard Light)

| Token | HEX | Popis |
|---|---|---|
| `Theme.Brush.Background` | `#F4F6FA` | pozadí okna |
| `Theme.Brush.Surface` | `#FFFFFF` | panely, GroupBox |
| `Theme.Brush.Chrome` | `#F4F6FA` | TUI pozadí |
| `Theme.Brush.TitleBar` | `#F4F6FA` | horní a spodní lišta |
| `Theme.Brush.MenuBackground` | `#F4F6FA` | menu pozadí |
| `Theme.Brush.Menu.Hover` | `#E5E7EB` | hover položky menu |
| `Theme.Brush.Text.Primary` | `#1A1A2E` | primární text |
| `Theme.Brush.Text.Secondary` | `#6B7280` | sekundární text |
| `Theme.Brush.ButtonText` | `#1A1A2E` | popisky tlačítek |
| `Theme.Brush.Border` | `#E5E7EB` | ohraničení |

---

## Paleta Modern Dark

| Token | HEX | Popis |
|---|---|---|
| `Theme.Brush.Background` | `#191919` | pozadí okna |
| `Theme.Brush.Surface` | `#272727` | panely, GroupBox |
| `Theme.Brush.Chrome` | `#121212` | TUI okno pozadí |
| `Theme.Brush.TitleBar` | `#202020` | horní + spodní lišta *(nový v1.1)* |
| `Theme.Brush.MenuBackground` | `#181818` | menu + Splash pozadí *(nový v1.1)* |
| `Theme.Brush.Menu.Hover` | `#363635` | zvýraznění menu *(nový v1.1)* |
| `Theme.Brush.SecondaryButton` | `#2E2E2E` | tlačítka v sek. oknech idle *(nový v1.1)* |
| `Theme.Brush.Disabled` | `#282828` | idle nástrojů tlačítka |
| `Theme.Brush.Hover` | `#464646` | hover tlačítek |
| `Theme.Brush.Pressed` | `#535353` | stisknutý stav |
| `Theme.Brush.Text.Primary` | `#FFFFFF` | primární text |
| `Theme.Brush.Text.Secondary` | `#787878` | sekundární text |
| `Theme.Brush.ButtonText` | `#FFFFFF` | popisky tlačítek |
| `Theme.Brush.Border` | `#3A3A3A` | ohraničení |

---

## Paleta Standard Dark

| Token | HEX | Popis |
|---|---|---|
| `Theme.Brush.Chrome` | `#121212` | TUI pozadí |
| `Theme.Brush.Background` | `#282828` | pozadí okna, Nástroje idle |
| `Theme.Brush.Active` | `#383838` | Akce tlačítka idle, Latence, Log |
| `Theme.Brush.Hover` | `#464646` | hover |
| `Theme.Brush.Pressed` | `#535353` | pressed |
| `Theme.Brush.Separator` | `#1E1E1E` | GridSplitter, oddělovače |
| `Theme.Brush.Text.Primary` | `#FFFFFF` | primární text |
| `Theme.Brush.Text.Secondary` | `#787878` | sekundární text |

---

## Mapování tlačítek (Legacy + Modern Light)

### Zelená `#D0FFD0` / Dark `#1C3527`

- `OpenClaw TUI`
- `Živá data`
- `Uložit` v Nastavení
- `Spustit` v Cleaning Tool
- `[REDACT]` v Token Manageru *(tučné)*

### Červená `#FFD0D0` / Dark `#3D1515`

- `Opravit konfiguraci`
- `Zavřít` v Gateway logu, Živých datech, Cleaning Tool, Token Manageru
- `Zrušit` v Nastavení
- `Obnovit` v Token Manageru

### Modrá `#D0E8FF` / Dark `#152535`

- `Reset na výchozí` v Nastavení
- `Kopírovat` v Gateway logu a Živých datech
- `Náhled` v Cleaning Tool
- `Procházet` v Token Manageru *(modrá)*
- `Ověřit` v Token Manageru *(tučná modrá)*

---

## Zaoblené rohy (v1.1)

Všechna témata **Modern family** (Dark, ModernLight, StandardDark) používají `CornerRadius="6"` pro:
- Tlačítka v sekundárních oknech (`Theme.Style.Button`)
- GroupBox v sekundárních oknech (`Theme.Style.GroupBox`)
- Hover zvýraznění menu položek (`CornerRadius="4"`)

Legacy téma: žádné zaoblení (systémové WPF).

---

## Pravidla pro přidání nového tématu

1. Vytvořit `Resources/Themes/Theme.{Název}.xaml`
2. Definovat **všechny** tokeny z tabulky výše (paleta podle Variant: Dark/Light)
3. Přidat sémantické tokeny (`Brush.ActionPositive` atd.) s hodnotami odpovídajícími tmavé/světlé variantě
4. Registrovat v `ThemeService._themeResourcePaths`
5. **Nikdy** nevkládat HEX hodnoty přímo do code-behind — vždy přes `ThemeService.GetBrush()`
