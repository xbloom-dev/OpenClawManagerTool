# OpenClaw Manager Tool by Bloom — Changelog

---

## v2.0.5 - 4 July 2026

### Modern Dark
- Completed the focused Modern Dark visual integration pass on top of the stable v2.0.4 window-chrome baseline.
- Added Modern Dark TopBar, status bar, panel, App Log, splash-panel, and secondary-window visual polish.
- Restored the Modern Dark secondary-window shell while keeping full secondary-window transparency optional and disabled by default.
- Added the `UseFullSecondaryWindowTransparency` setting for users who explicitly want full Modern Dark secondary-window transparency.
- Backported the approved 2026-06-01/02 Modern Dark visual values without reopening the locked Legacy, HighContrast, or CrabCute themes.

### Release
- User smoke test accepted the v2.0.5 Modern Dark state on 4 July 2026.
- Remaining visual tweaks are deferred to v2.0.6.
- Wider StandardLight, StandardDark, and ModernLight redesign work remains deferred to a later release.

---

## v2.0.4 - 3 June 2026

### Stability
- Added a startup-only main-window chrome selection so `Legacy` and `StandardDark` keep the native Windows frame while `StandardLight`, `ModernDark`, `ModernLight`, `HighContrast`, and `CrabCute` keep the frameless custom frame from the v2.0.3 baseline.
- Moved Windows 11 DWM rounded-corner and immersive-dark setup into a narrow helper and apply it only for frameless startup themes.
- Removed the old live `DragMove()` titlebar handler and stopped live theme switching from mutating `WindowStyle` / `WindowChrome`.
- Kept `AllowsTransparency=False` so window shadows, resizing, and frame composition stay handled by Windows/DWM.

### Release
- This is a small stable checkpoint before the v2.0.5 Standard/Modern theme redesign work.
- `Legacy`, `HighContrast`, and `CrabCute` theme dictionaries were intentionally left unchanged.

---

## v2.0.3 - 3 June 2026

### Bug fixes
- Fixed WelcomeWindow save-settings failure text to use localized strings with proper Czech diacritics.
- Removed settings DI access from static theme helpers by tracking the currently applied theme in `ThemeService`.
- Moved AboutWindow shortcut colors and secondary-window output/log colors to semantic theme resources.
- Added missing Legacy fallback brushes used by `Style.Button.StandardFlat`.
- Fixed the AboutWindow logo WebView background so the crab icon remains transparent in all themes.

### Release
- Kept risky Modern Dark runtime UI work out of this stabilization release.
- Verified manual smoke test: Settings and tested secondary windows open/close without crashes, and the app exits without zombie processes.

---

## v2.0.0 — 25 May 2026

### Architecture
- Introduced Dependency Injection via `Microsoft.Extensions.Hosting` for application services and windows.
- Migrated the main UI flows toward MVVM: About, Settings, Cleaning, Gateway Log, Live Log, Token Manager, and MainWindow status logic now use ViewModels.
- Added `MainViewModel` for Gateway start/stop/restart state, status polling, AppLog, latency display, and localized left-panel state.
- Added `IAppEnvironment` to encapsulate application paths and make filesystem-dependent code easier to test.
- Reduced theme runtime code by moving selected shared WPF styles into `Resources/Themes/CoreStyles.xaml`.

### User-visible improvements
- Smoother UI status updates by moving WMI Gateway checks and latency log reads away from the UI thread.
- More stable secondary windows through DI-managed dependencies instead of ad-hoc service construction.
- Cleaner theme behavior for shared GroupBox, Button, and MenuItem styling.

### Security and tests
- Preserved the existing DPAPI vault, `.ocvault`, PBKDF2-SHA256, and AES-256-GCM implementation without vault format changes.
- Expanded TokenService test coverage for token updates, rotation, removal, redaction overwrite protection, and GitIgnore idempotence.
- Maintained Gitleaks verification as part of the release checklist.

### Completed from the v1.x backlog
- UI thread blocking audit
- App path/environment encapsulation
- Dependency Injection foundation
- MVVM migration for all major windows
- Theme system cleanup pilot

---

## v1.1 — 23. května 2026

### Bezpečnost
- Export/import Token Vaultu do přenosného `.ocvault` souboru chráněného heslem
- `.ocvault` používá PBKDF2-SHA256 (`200 000` iterací) a AES-256-GCM
- Import zálohy znovu ukládá vault přes Windows DPAPI pro aktuální profil
- Přidán `PasswordPromptWindow` s WPF `PasswordBox` pro zadání hesla
- Rozšířená validace OpenClaw příkazu odmítá shell znaky `<`, `>`, `%`, `^`, `&`, `|`
- P0 security scan Git historie dokončen přes Gitleaks a doplňkový high-confidence scan bez nálezů
- Přidán `.gitleaks.toml` s úzkou allowlistou pro známý false positive v minifikovaném xterm.js

### Token Manager
- Tlačítka **Backup** / **Restore** pro ruční zálohu a obnovu vaultu
- Save/Open dialogy pro `.ocvault`
- Test export/import roundtripu v `TokenService.Tests`
- Test špatného hesla při importu

### Developer workflow
- About EasterEgg prompt podporuje replay splash, přepínání témat a rychlé otevření logů/tokenů/nastavení
- GitHub Actions CI ověřeno pro build i TokenService testy na aktuální větvi
- GitHub Actions aktualizováno na Node 24 kompatibilní akce (`actions/checkout@v6`, `actions/setup-dotnet@v5`) a pevný runner `windows-2025`

### Dokumentace
- Public `README.md` aktualizován pro v1.1 a vault backup/restore workflow
- Changelog a release notes doplněny o stav v1.1 před public/release přípravou

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
- Token Manager localized with consistent vault terminology
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
