# OpenClaw Manager v0.99 — Release Notes

**Datum:** 13. května 2026
**Git tag:** v0.99
**Typ:** Pre-release (developer build)

---

## Co je v0.99

v0.99 je integrační build spojující v0.5 GUI větev (splash, témata, ikony) s Token Managerem. Obsahuje všechny P0/P1 opravy ze stabilizační fáze a výkonnostní optimalizace status baru.

---

## Novinky oproti v0.65

### Funkce
- Token Manager: vault CRUD, DPAPI šifrování, redact/restore/verify souborů, import, audit log
- Splash screen: MP4 video + PNG freeze frame v Modern theme, ASCII art v Legacy theme
- Theme systém: Legacy / Modern přepínání za běhu včetně splash přepnutí
- Bitmap ikony v tlačítkách (Modern theme)
- Favicon přes `EventManager` — všechna okna mají stejnou ikonu
- Cleaning Tool krok 7: Token Manager *.bak soubory (opt-in)

### Bezpečnost
- DPAPI šifrování vault hodnot (Windows CryptProtectData / CryptUnprotectData)
- Entropy = SHA256 z `"OpenClawManager.TokenVault.v1"` — cross-app ochrana
- Automatická migrace plaintext vaultu na DPAPI při prvním uložení
- Vault safety varování pro Git, .openclaw, cloud sync, sdílené složky
- Validace `OpenClawCommand` — zakázané shell znaky + PS escape

### Výkon
- `ResourceMonitor.MeasureAsync()` — WMI dotazy na thread pool, UI thread neblokován
- RAM/CPU cache 4s — eliminuje zbytečné WMI dotazy každé 2s
- nvidia-smi Kill po 2s timeoutu — zabrání zombie procesu
- `LatencyTracker._lastMs` field — O(1) místo `Queue.Last()` O(n)

### Stabilita
- WebView2 `Visibility="Hidden"` při inicializaci (ne Collapsed) — HWND pro WebView2 nutný
- `ShowWebView()` voláno až po skrytí SplashOverlay — eliminuje airspace konflikt
- `LiveLogWindow._highlightClearTimer` persistent — eliminuje timer leak při rychlém logování
- `TokenManagerWindow` subscribuje `SettingsChanged` — vault path se obnoví bez restartu

### Dokumentace
- `user-manual.md` v2.0: kapitoly Token Manager (vault, redact workflow, bezpečnost) a Témata
- `dev-manual.md` v3.0: sekce 16–19 (TerminalControl/WebView2, Splash architektura, Theme systém, Token Manager)
- `README.md`: quickstart, vault safety, offline terminal, smoke test

---

## Ověření

```powershell
dotnet build                                        # 0 warnings, 0 errors
dotnet run --project TokenService.Tests             # všechny testy prošly
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

GUI smoke: MainWindow, TerminalControl (ASCII art Legacy / video Modern), TokenManagerWindow, CleaningWindow, SettingsWindow, AboutWindow — vše otevřeno a zavřeno úspěšně.

---

## Zbývající před v1.0

| Priorita | Položka |
|---|---|
| 🟡 P1 | Token Manager UI lokalizace (EN/CS) — hardcoded česky ve ~36 místech |
| 🟡 P1 | Témata — Dark, HighContrast, Compact (ThemeService architektura je připravená) |
| 🟢 P2 | Smoke test na čistém Windows (bez .NET SDK, ověřit WebView2 terminál ručně) |
| 🟢 P2 | `AppSettings` migrace — `SettingsService.MigrateSettings()` připravena, zatím prázdná |

---

## Residual poznámka

Automatizovaný desktop smoke v Codex prostředí nemůže spolehlivě dokončit WebView2 terminál test (embedded browser potřebuje interaktivní desktop session). Všechna WPF okna prošla smoke testem. WebView2 terminál ověřit jednou ručně na reálném Windows desktopu.
