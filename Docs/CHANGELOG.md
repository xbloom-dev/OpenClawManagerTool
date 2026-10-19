# OpenClaw Manager Tool by Bloom — Changelog

---

## Unreleased (v3.0.0)

### Mail and calendar control module
- Added the **Mail and calendar** window (menu *Open > Mail and calendar...*): a control panel for the automatic mail and calendar processing used by OpenClaw agents (overview, settings, sender rules, proposals, outbox, recipients and templates, health and jobs, history and audit).
- The settings form is generated from the backend schema (`control_run.py schema`), so a new backend setting appears without an app update.
- A persistent bar shows the sending state (`Nothing leaves` versus `SENDING ENABLED`, shadow mode and `auto_reply_scope`).
- The app enforces no rules: risky changes are confirmed by the backend in a native Windows dialog, the window only waits and shows the result (saved / declined / could not be confirmed). Payments with a finding need a second explicit step (`override`).
- The backend is called as a process (`ArgumentList`, UTF-8, `PYTHONUTF8=1`, 30 s / 150 s timeouts) behind `IMailControlRunner`; `FakeMailControlRunner` simulates it for development and tests (`OPENCLAW_MAILCONTROL_FAKE`).
- Subjects, names and drafts from e-mails are shown as plain text; the app never reads the `credentials` folder and never shows the app password.
- New `MailRootPath` setting (settings schema 3) with migration and test; CS/EN strings `Str_MailControl_*`.
- Overview shows "mail runs skipped in the heavy-task window" when the backend returns `paused_runs` (settings of the window appear automatically from the schema).
- 30 new console tests (contract parsing, error mapping, form generation, safety behaviour, migration, localization consistency).

### Standard Dark
- Brighter Active/Hover/Pressed tokens (`#4A4A4A` / `#5C5C5C` / `#6E6E6E`) and a new `Theme.Brush.TuiBackground` (`#191919`) for the TUI panel, splash and terminal.
- `SystemColors` overrides fix the light border of the system menu popup.

### Packaging
- The Lite build ships only the Legacy theme preview (the other previews were never shown).
- Removed the unused `OpenClawManager-Full.iss` and `OpenClawManager-Lite.iss`; CI builds the single Full/Lite setup from `OpenClawManager.iss`.

---

## Unreleased (v2.0.6)

Patch release on top of v2.0.5 (without the Mail and Calendar module, which ships in v3.0.0).

### Fixed
- The EXE file properties (FileVersion, AssemblyVersion) now match the version shown in the status bar. Both are derived from `Version`, including `-p:Version` in CI, instead of being hardcoded in the project file.

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
