# TODO — OpenClaw Manager Tool

Odložené úkoly mimo aktivní práci na v1.1.

---

## Zbývající body z architektonické analýzy

Priority:
- **P0** — řešit před zveřejněním / releasem, bezpečnost nebo stabilita
- **P1** — řešit v nejbližší minor verzi, významně zlepší údržbu nebo spolehlivost
- **P2** — plánovaný refactor, dělat postupně bez velkého přepisu najednou

### P0 — bezpečnost a release jistota

- **Security scan Git historie** — před přepnutím repozitáře na public prohledat historii na tokeny, API klíče, hesla a další tajné hodnoty; při nálezu řešit rotaci klíčů a případné čištění historie.

### P1 — data, výkon a testovatelnost

- **Export/import DPAPI vaultu chráněný heslem** — DPAPI je bezpečné, ale je navázané na Windows profil; přidat ruční zálohu vaultu chráněnou heslem pro obnovu při ztrátě profilu.
- **Audit UI thread blockingu** — projít Gateway/TUI/logy/resource monitor a ověřit, že IO a delší operace neběží na UI vlákně; blokující místa převést na `async/await` nebo bezpečně přesunout mimo UI vlákno.
- **Zapouzdření cest a souborového IO** — zavést malé rozhraní pro aplikační prostředí/cesty (např. `IAppEnvironment`) a postupně přes něj vést `%APPDATA%`, temp složky a testovací cesty.

### P2 — dlouhodobá údržba WPF vrstvy

- **Postupný přechod k MVVM** — nezačínat velkým přepisem hlavního okna; nejdřív menší okna a izolované funkce, teprve později hlavní okno, splash a TUI.
- **Zavedení dependency injection** — po oddělení služeb a prostředí přidat DI kontejner pro služby, viewmodely a testy.
- **Zmenšení `MainWindow.*.cs` code-behind** — postupně přesouvat aplikační logiku z partial tříd do služeb/viewmodelů; citlivé části splash/WebView2/TUI měnit jen samostatně a s ručním testem.
- **Dotažení theme systému** — odstraňovat zbytky hardcoded barev a duplicitních runtime stylů; témata mají číst hodnoty z ResourceDictionary/metadat a nemají si navzájem přepisovat vzhled.

---

## Existující agentí větve

Před release v1.1 vyřešit staré větve, které ještě mají `v1.1` prefix:

- `claude/v1.1-dark-theme` — nechat agenta dokončit (merge do develop) nebo smazat
- `claude/v1.1-docs-release-polish` — nechat agenta dokončit (merge do develop) nebo smazat
- `codex/v1.1-themes-diagnostics` — nechat agenta dokončit (merge do develop) nebo smazat

Po dokončení / smazání by všechny aktivní větve měly používat nový pojmenovací standard (`codex/<popis>`, `claude/<popis>`, bez `v1.1-` prefixu).

---

## Před release v1.1 — přechod na public + monetizace

1. **Projít historii** — ověřit že v Git historii nejsou citlivé údaje (tokeny, API klíče, hesla)
2. **Změnit visibility** — Settings → Danger Zone → Change visibility → Public
3. **Aktivovat GitHub branch protection** (po public funguje zdarma):
   - `master`: require PR, require 1 approval, block force push, restrict deletions
   - `develop`: require PR, require 1 approval, block force push, restrict deletions
4. **Vytvořit `.github/FUNDING.yml`** s odkazy na podporu:
   ```yaml
   github: [Bloom]
   ko_fi: bloom
   custom: ["https://paypal.me/..."]
   ```
5. **GitHub Release** — vytvořit release `v1.1` s ZIP balíčky (build artefakty)
6. **README pro veřejnost** — screenshots, jak nainstalovat, jak používat
7. **`CONTRIBUTING.md`** — jak přispět
8. **`CODE_OF_CONDUCT.md`** — pravidla komunity
9. **Issue/PR templates** v `.github/ISSUE_TEMPLATE/` a `.github/pull_request_template.md`

---

## Nice-to-have (kdykoliv)

- **GitHub Sponsors** — schválení (zdarma, ale trvá pár dnů); 100% podpory jde tobě
- **Tagy verzí** — sjednotit pojmenování (v1.0 už existuje; pro mezi-iterace v1.1.1, v1.1.2...)
- **Automatický release build** — GitHub Actions workflow co staví release ZIP při push tagu

---

## Setup poznámky (pro budoucí referenci)

- Repo migrace: sjr4qxh785-rgb → xbloom-dev (16.5.2026)
- Branch model: master (stable), develop (integrace), agent/* a bloom/* (work)
- Ochrana: lokální Git pre-push hook v `.git/hooks/pre-push` (instalováno přes `install-git-hooks.ps1`)
- Workspace: OpenClawManager (Bloom), ClaudeWorkspace (Claude Code), CodexWorkspace (Codex)
