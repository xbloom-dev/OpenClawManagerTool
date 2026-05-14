# OpenClaw Manager Tool by Bloom — Changelog

---

## v1.1 (vývoj — větev develop/v1.1)

### Refactoring
- Centralizované theme resource tokeny: `Brush.ActionPositive`, `Brush.ActionDanger`, `Brush.ActionUtility`, `Brush.SplashModernBackground`
- Tlačítka v hlavních oknech přepnutá z přímých HEX hodnot na theme resources
- `ThemeService.GetBrush()` API pro code-behind přístup ke theme brushům

---

## v1.0 — 13. května 2026

Finální stabilizační vydání po v0.99.

### Bezpečnost
- DPAPI šifrování tokenů (Windows CryptProtectData / CryptUnprotectData)
- Migrace trezoru z plaintext na DPAPI při prvním uložení
- `CurrentVaultVersion` — ochrana proti otevření novější verze trezoru
- Git/vault safety banner v Token Manageru
- Validace a escapování příkazů v Nastavení

### Stabilita a výkon
- `ResourceMonitor.MeasureAsync()` — WMI měření mimo UI thread
- Správný async timeout pro `nvidia-smi` s Kill po vypršení
- Guard proti překryvu status ticků v `MainWindow`
- `LatencyTracker._lastMs` — O(1) místo `Queue.Last()`
- Oprava timer leak v `LiveLogWindow` (persistent `_highlightClearTimer`)
- Oprava `SettingsChanged` subscription v `TokenManagerWindow`

### SplashScreen a WebView2
- Oprava WebView2 airspace chování — WebView je `Hidden`, ne `Collapsed`
- `ShowWebView()` se volá až po `DisposeSplash()`
- Modern video → PNG freeze frame po doběhnutí
- Legacy ASCII art
- Fallback na ASCII při chybě splash assetů
- `MediaElement UnloadedBehavior="Stop"` pro spolehlivé uvolnění videa
- Splash zůstane viditelný až do kliknutí na TUI

### Lokalizace a UI
- Kompletní lokalizace hlavních UI stringů (EN/CS)
- Token Manager lokalizován — terminologie dle `DEVELOPER_MANUAL_v1.0.md`
- Opravena česká diakritika — odstraněny garbled znaky
- Tooltipy pro tlačítka
- Barevné zvýraznění tlačítek (zelená / červená / modrá)
- Modern splash pozadí `#4C247E`
- About okno doplněno o klávesové zkratky
- `Ctrl+L` pro živá data Gateway logu
- Cleaning Tool — český titulek
- Favicon přes `EventManager` — všechna okna

### Token Manager
- Titulek: **Správce API klíčů**
- Terminologie: Trezor (vault), zastupný text, [REDACT], Obnovit, Ověřit
- Náhled maskování (přejmenováno z "Redact náhled")
- Finální kosmetika tlačítek dle barevného systému

---

## v0.99 — 12. května 2026

- Integrační build: v0.5 GUI + Token Manager
- Lokální xterm.js — žádný CDN
- WebView2 per-process user data folder
- Vault safety varování

## v0.4 — 8. května 2026

- Lokalizace EN/CS přes ResourceDictionary
- Tooltipy na všech tlačítkách
- Klávesové zkratky kompletní
- Oprava parsování latencí (JSON + ANSI strip)
- LiveLogWindow (FileSystemWatcher, sliding window)
- Gateway Log — tlačítko Kopírovat + Živá data

## v0.2 — 7. května 2026

- Layout, TUI sekvence, sledování latencí
- Cleaning Tool (základní verze)

## v0.1 — 6. května 2026

- Správa Gateway (Start/Stop/Restart)
- Nastavení, menu, log viewer
