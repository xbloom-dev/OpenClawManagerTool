# OpenClaw Manager - barevny styl v1.0

## Ucel

Barvy tlacitek ve v1.0 nejsou dekorace. Pouzivaji se jako rychla vizualni orientace:

- zelena = hlavni pozitivni akce
- cervena = zavrit, zrusit, obnovit nebo rizikovejsi akce
- modra = pomocne technicke akce, nahled, kopirovani, overeni, prochazeni

Pri dalsich upravach je vhodne barvy sjednotit do theme resource tokenu, aby slo ve v1.1 pridat Dark, High Contrast a Compact tema bez rucniho prebarvovani jednotlivych oken.

## Pouzite barvy

| Role | HEX | Popis |
|---|---:|---|
| Akcni zelena | `#D0FFD0` | pozitivni/spousteci akce |
| Varovna cervena | `#FFD0D0` | zavrit, zrusit, obnovit, opravit |
| Technicka modra | `#D0E8FF` | kopirovat, nahled, reset, prochazet, overit |
| Modern splash pozadi | `#4C247E` | tmave fialove pozadi vybrane podle nejtmavsiho odstinu ze `splash.png` |

## Aktualni mapovani tlacitek

### Zelena `#D0FFD0`

- `OpenClaw TUI`
- `Ziva data`
- `Ulozit` v nastaveni
- `Spustit` v Cleaning Tool
- `[REDACT]` v TokenManageru

Poznamka pro TokenManager:

- `[REDACT]` ma byt zelene a tucne.
- Text zustava technicky jako `[REDACT]`.
- V tooltipu se funkce popisuje cesky slovem `maskovat`.

### Cervena `#FFD0D0`

- `Opravit konfiguraci`
- `Zavrit` v Gateway logu
- `Zavrit` v zivych datech
- `Zrusit` v nastaveni
- `Zavrit` v Cleaning Tool
- `Zavrit` v TokenManageru
- `Obnovit` v TokenManageru

### Modra `#D0E8FF`

- `Reset na vychozi` v nastaveni
- `Kopirovat` v Gateway logu
- `Kopirovat` v zivych datech
- `Nahled` v Cleaning Tool
- `Prochazet` v TokenManageru
- `Overit` v TokenManageru

Poznamka pro TokenManager:

- `Overit` ma byt modre a tucne.
- `Prochazet` ma byt modre.
- `Redact nahled` bylo prejmenovano na `Nahled`.

## Doporuceni pro v1.1

Pri pridani temat nevkladat dalsi HEX hodnoty primo do jednotlivych oken, pokud to nebude nezbytne. Doporuceny dalsi krok:

1. Vytvorit centralni theme resource klice, napriklad:
   - `Brush.ActionPositive`
   - `Brush.ActionDanger`
   - `Brush.ActionUtility`
   - `Brush.SplashModernBackground`
2. Zachovat soucasne HEX hodnoty jako vychozi pro aktualni Modern tema.
3. Pro Dark, High Contrast a Compact vytvorit vlastni hodnoty se stejnou semantikou.

