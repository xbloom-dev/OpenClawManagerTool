# OpenClaw Manager Tool by Bloom - uzivatelsky manual v1.0

## Ucel aplikace

OpenClaw Manager Tool by Bloom slouzi ke sprave a spousteni OpenClaw prostredi, praci s Gateway logy, cisteni souboru a sprave API klicu v TokenManageru.

## Spusteni

V runtime balicku spustit:

`OpenClawManager.exe`

Pri prvnim spusteni aplikace nacte nebo vytvori nastaveni. Pokud existuje starsi schema nastaveni, provede se migrace.

## Hlavni okno

Hlavni akce:

- `OpenClaw TUI` - spusti terminalove rozhrani OpenClaw
- `Ziva data` - otevira aktualni Gateway log
- `Gateway log` - zobrazi Gateway log
- `Vyčistit soubory` - otevira nastroj pro cisteni
- `Nastaveni` - konfigurace cest a prikazu
- `Spravce API klicu` - sprava tokenu/API klicu
- `About` - informace o aplikaci a klavesove zkratky

## Klavesove zkratky

- `Ctrl+L` - otevrit ziva data Gateway logu
- `Alt+F4` - zavrit aplikaci

## TokenManager

Titulek okna:

`Spravce API klicu`

Dulezite pojmy:

- `Trezor (vault)` - sifrovane uloziste tokenu
- `zastupny text` - placeholder hodnota
- `[REDACT]` - technicka znacka pro maskovani citlive hodnoty
- `Obnovit` - obnoveni hodnoty nebo stavu
- `Overit` - kontrola platnosti

Tlacitka:

- `Prochazet` - vyber souboru, modre
- `[REDACT]` - maskovani citlivych hodnot, zelene a tucne
- `Obnovit` - cervene
- `Overit` - modre a tucne
- `Nahled` - zobrazeni nahledu maskovani

## Cleaning Tool

Okno je v cestine oznaceno jako vycisteni souboru. Tlacitka:

- `Nahled` - modre, zobrazi co se bude cistit
- `Spustit` - zelene, spusti cisteni
- `Zavrit` - cervene

## SplashScreen

Modern tema:

- zobrazi video
- po videu ponecha obrazek jako freeze frame
- terminal se zobrazi az po kliknuti na OpenClaw TUI

Legacy tema:

- zobrazi ASCII art

Pokud chybi splash assety, aplikace ma prepnout na ASCII fallback.

## Barvy tlacitek

- zelena `#D0FFD0` - pozitivni akce
- cervena `#FFD0D0` - zavrit/zrusit/obnovit
- modra `#D0E8FF` - pomocne technicke akce

