# OpenClaw Manager Tool by Bloom - changelog v1.0

## v1.0

Finalni stabilizacni vydani po v0.99.

### Bezpecnost

- DPAPI sifrovani tokenu
- migrace token vaultu
- ochrana proti novejsi verzi trezoru
- Git/vault safety banner
- validace a escapovani prikazu

### Stabilita

- optimalizace ResourceMonitoru
- WMI mereni mimo UI thread
- korektni timeout pro `nvidia-smi`
- guard proti prekryvu status ticku
- oprava timer leak v LiveLogWindow
- oprava subscription v TokenManagerWindow

### SplashScreen a WebView2

- oprava WebView2 airspace chovani
- WebView je `Hidden`, ne `Collapsed`
- Modern video -> PNG freeze frame
- Legacy ASCII art
- fallback na ASCII pri chybe splash assetu
- bezpecnejsi uvolneni videa
- zachovani splash az do kliknuti na TUI

### Lokalizace

- UI texty pres resources
- TokenManager lokalizovan
- doplnena ceska diakritika
- odstranene garbled znaky
- tooltips pro tlacitka

### UI

- barevne zvyrazneni tlacitek:
  - zelena `#D0FFD0`
  - cervena `#FFD0D0`
  - modra `#D0E8FF`
- Modern splash pozadi `#4C247E`
- About okno doplnene o zkratky
- `Ctrl+L` pro ziva data Gateway logu
- Cleaning Tool ma cesky titulek

### TokenManager

- titulek `Spravce API klicu`
- `Trezor (vault)`
- `zastupny text`
- `[REDACT]` zachovano jako technicky vyraz
- `Obnovit`
- `Overit`
- `Redact nahled` prejmenovano na `Nahled`
- finalni kosmetika tlacitek podle barevneho systemu

