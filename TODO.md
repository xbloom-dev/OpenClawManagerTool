# TODO — OpenClaw Manager Tool

Plánované úkoly a technický dluh mimo aktivní vývoj.

---

## Priority

- **P0** — řešit před releasem; bezpečnost nebo stabilita
- **P1** — řešit v nejbližší minor verzi; významně zlepší údržbu nebo spolehlivost
- **P2** — plánovaný refactor; dělat postupně bez velkého přepisu najednou

---

## P1 — data, výkon a testovatelnost

- **Audit UI thread blockingu** — projít Gateway/TUI/logy/resource monitor a ověřit, že IO a delší operace neběží na UI vlákně; blokující místa převést na `async/await` nebo bezpečně přesunout mimo UI vlákno.
- **Zapouzdření cest a souborového IO** — zavést malé rozhraní pro aplikační prostředí/cesty (např. `IAppEnvironment`) a postupně přes něj vést `%APPDATA%`, temp složky a testovací cesty.

## P2 — dlouhodobá údržba WPF vrstvy

- **Postupný přechod k MVVM** — nezačínat velkým přepisem hlavního okna; nejdřív menší okna a izolované funkce, teprve později hlavní okno, splash a TUI.
- **Zavedení dependency injection** — po oddělení služeb a prostředí přidat DI kontejner pro služby, viewmodely a testy.
- **Zmenšení `MainWindow.*.cs` code-behind** — postupně přesouvat aplikační logiku z partial tříd do služeb/viewmodelů; citlivé části splash/WebView2/TUI měnit jen samostatně a s ručním testem.
- **Dotažení theme systému** — odstraňovat zbytky hardcoded barev a duplicitních runtime stylů; témata mají číst hodnoty z ResourceDictionary/metadat a nemají si navzájem přepisovat vzhled.

---

## Hotovo v v1.1

- **Export/import vaultu chráněný heslem** — přenosný `.ocvault`, PBKDF2-SHA256 + AES-256-GCM, obnova zpět do lokálního DPAPI vaultu.
- **Rozšířená validace OpenClaw příkazu** — odmítá shell znaky `<`, `>`, `%`, `^`, `&`, `|`.
- **Security scan Git historie** — Gitleaks bez nálezů.
- **CI na Node 24 kompatibilní akce** — `actions/checkout@v6`, `actions/setup-dotnet@v5`, runner `windows-2025`.
