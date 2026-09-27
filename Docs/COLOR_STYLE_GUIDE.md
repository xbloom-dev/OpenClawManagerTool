# OpenClaw Manager â€” BarevnĂ˝ styl v1.1

## ĂšÄŤel

Barvy tlaÄŤĂ­tek nejsou dekorace â€” pouĹľĂ­vajĂ­ se jako rychlĂˇ vizuĂˇlnĂ­ orientace:

- **zelenĂˇ** = hlavnĂ­ pozitivnĂ­ akce
- **ÄŤervenĂˇ** = zavĹ™Ă­t, zruĹˇit, obnovit nebo rizikovÄ›jĹˇĂ­ akce
- **modrĂˇ** = pomocnĂ© technickĂ© akce, nĂˇhled, kopĂ­rovĂˇnĂ­, ovÄ›Ĺ™enĂ­, prochĂˇzenĂ­

Od v1.1 jsou barvy definovĂˇny jako **theme resource tokeny** v `Resources/Themes/Theme.*.xaml`.
Code-behind pĹ™istupuje pĹ™es `ThemeService.GetBrush("Brush.ActionPositive", fallback)`.

---

## SĂ©mantickĂ© tokeny (sdĂ­lenĂ© napĹ™Ă­ÄŤ tĂ©maty)

| Token | Theme.StandardLight | Theme.ModernDark | Popis |
|---|---|---|---|
| `Brush.ActionPositive` | `#D0FFD0` | `#1C3527` | pozitivnĂ­/spouĹˇtÄ›cĂ­ akce |
| `Brush.ActionDanger` | `#FFD0D0` | `#3D1515` | zavĹ™Ă­t, zruĹˇit, obnovit, opravit |
| `Brush.ActionUtility` | `#D0E8FF` | `#152535` | kopĂ­rovat, nĂˇhled, reset, prochĂˇzet |
| `Brush.SplashModernBackground` | `#4C247E` | `#181818` | pozadĂ­ splash obrazovky |

---

## Paleta Theme.StandardLight

| Token | HEX | Popis |
|---|---|---|
| `Theme.Brush.Background` | `#F4F6FA` | pozadĂ­ okna |
| `Theme.Brush.Surface` | `#FFFFFF` | panely, GroupBox |
| `Theme.Brush.Chrome` | `#F4F6FA` | TUI pozadĂ­ |
| `Theme.Brush.TitleBar` | `#F4F6FA` | hornĂ­ a spodnĂ­ liĹˇta |
| `Theme.Brush.MenuBackground` | `#F4F6FA` | menu pozadĂ­ |
| `Theme.Brush.Menu.Hover` | `#E5E7EB` | hover poloĹľky menu |
| `Theme.Brush.Text.Primary` | `#1A1A2E` | primĂˇrnĂ­ text |
| `Theme.Brush.Text.Secondary` | `#6B7280` | sekundĂˇrnĂ­ text |
| `Theme.Brush.ButtonText` | `#1A1A2E` | popisky tlaÄŤĂ­tek |
| `Theme.Brush.Border` | `#E5E7EB` | ohraniÄŤenĂ­ |

---

## Paleta Theme.ModernDark

| Token | HEX | Popis |
|---|---|---|
| `Theme.Brush.Background` | `#191919` | pozadĂ­ okna |
| `Theme.Brush.Surface` | `#272727` | panely, GroupBox |
| `Theme.Brush.Chrome` | `#121212` | TUI okno pozadĂ­ |
| `Theme.Brush.TitleBar` | `#202020` | hornĂ­ + spodnĂ­ liĹˇta *(novĂ˝ v1.1)* |
| `Theme.Brush.MenuBackground` | `#181818` | menu + Splash pozadĂ­ *(novĂ˝ v1.1)* |
| `Theme.Brush.Menu.Hover` | `#363635` | zvĂ˝raznÄ›nĂ­ menu *(novĂ˝ v1.1)* |
| `Theme.Brush.SecondaryButton` | `#2E2E2E` | tlaÄŤĂ­tka v sek. oknech idle *(novĂ˝ v1.1)* |
| `Theme.Brush.Disabled` | `#282828` | idle nĂˇstrojĹŻ tlaÄŤĂ­tka |
| `Theme.Brush.Hover` | `#464646` | hover tlaÄŤĂ­tek |
| `Theme.Brush.Pressed` | `#535353` | stisknutĂ˝ stav |
| `Theme.Brush.Text.Primary` | `#FFFFFF` | primĂˇrnĂ­ text |
| `Theme.Brush.Text.Secondary` | `#787878` | sekundĂˇrnĂ­ text |
| `Theme.Brush.ButtonText` | `#FFFFFF` | popisky tlaÄŤĂ­tek |
| `Theme.Brush.Border` | `#3A3A3A` | ohraniÄŤenĂ­ |

---

## Paleta Theme.StandardDark

| Token | HEX | Popis |
|---|---|---|
| `Theme.Brush.Chrome` | `#121212` | TUI pozadĂ­ |
| `Theme.Brush.Background` | `#282828` | pozadĂ­ okna, NĂˇstroje idle |
| `Theme.Brush.Active` | `#383838` | Akce tlaÄŤĂ­tka idle, Latence, Log |
| `Theme.Brush.Hover` | `#464646` | hover |
| `Theme.Brush.Pressed` | `#535353` | pressed |
| `Theme.Brush.Separator` | `#1E1E1E` | GridSplitter, oddÄ›lovaÄŤe |
| `Theme.Brush.Text.Primary` | `#FFFFFF` | primĂˇrnĂ­ text |
| `Theme.Brush.Text.Secondary` | `#787878` | sekundĂˇrnĂ­ text |

---

## MapovĂˇnĂ­ tlaÄŤĂ­tek (Theme.Legacy + Theme.ModernLight)

### ZelenĂˇ `#D0FFD0` / Dark `#1C3527`

- `OpenClaw TUI`
- `Ĺ˝ivĂˇ data`
- `UloĹľit` v NastavenĂ­
- `Spustit` v Cleaning Tool
- `[REDACT]` v Token Manageru *(tuÄŤnĂ©)*

### ÄŚervenĂˇ `#FFD0D0` / Dark `#3D1515`

- `Opravit konfiguraci`
- `ZavĹ™Ă­t` v Gateway logu, Ĺ˝ivĂ˝ch datech, Cleaning Tool, Token Manageru
- `ZruĹˇit` v NastavenĂ­
- `Obnovit` v Token Manageru

### ModrĂˇ `#D0E8FF` / Dark `#152535`

- `Reset na vĂ˝chozĂ­` v NastavenĂ­
- `KopĂ­rovat` v Gateway logu a Ĺ˝ivĂ˝ch datech
- `NĂˇhled` v Cleaning Tool
- `ProchĂˇzet` v Token Manageru *(modrĂˇ)*
- `OvÄ›Ĺ™it` v Token Manageru *(tuÄŤnĂˇ modrĂˇ)*

---

## ZaoblenĂ© rohy (v1.1)

VĹˇechna tĂ©mata **Theme family** (Theme.ModernDark, Theme.ModernLight, Theme.StandardDark) pouĹľĂ­vajĂ­ `CornerRadius="6"` pro:
- TlaÄŤĂ­tka v sekundĂˇrnĂ­ch oknech (`Theme.Style.Button`)
- GroupBox v sekundĂˇrnĂ­ch oknech (`Theme.Style.GroupBox`)
- Hover zvĂ˝raznÄ›nĂ­ menu poloĹľek (`CornerRadius="4"`)

Legacy tĂ©ma: ĹľĂˇdnĂ© zaoblenĂ­ (systĂ©movĂ© WPF).

---

## Pravidla pro pĹ™idĂˇnĂ­ novĂ©ho tĂ©matu

1. VytvoĹ™it `Resources/Themes/Theme.{NĂˇzev}.xaml`
2. Definovat **vĹˇechny** tokeny z tabulky vĂ˝Ĺˇe (paleta podle Variant: Dark/Light)
3. PĹ™idat sĂ©mantickĂ© tokeny (`Brush.ActionPositive` atd.) s hodnotami odpovĂ­dajĂ­cĂ­mi tmavĂ©/svÄ›tlĂ© variantÄ›
4. Registrovat v `ThemeService._themeResourcePaths`
5. **Nikdy** nevklĂˇdat HEX hodnoty pĹ™Ă­mo do code-behind â€” vĹľdy pĹ™es `ThemeService.GetBrush()`


