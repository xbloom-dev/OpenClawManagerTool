# TODO - OpenClaw Manager Tool

Aktualni stav po instalator + welcome integraci (29 May 2026).

## Priorities

- P0 = pred RC/Release, stabilita nebo user-facing bug
- P1 = dalsi minor verze
- P2 = dlouhodoby refactor

---

## P0 (open)

- [ ] Welcome UX redesign (soucasna verze je funkcni, ale vizualne nevyhovuje).
- [ ] Potvrdit final vzhled Legacy theme proti puvodni baseline (vsechny ovladaci prvky a spacing).

## P1 (open)

- [ ] Sekundarni okna vizualne sjednotit se zbytkem app (Settings, About, Cleaning, Token dialogs).
- [ ] Audit hardcoded barev a textu v XAML (omezit runtime prepisy, vic DynamicResource).
- [ ] Prevest User Manual z Markdown do lokalnich HTML souboru (CS/EN) pro prijemnejsi zobrazeni.

## P2 (open)

- [ ] Postupny rozpad `MainWindow.*.cs` (mensi code-behind, vice ViewModel/service vrstvy).
- [ ] Pokracovat v MVVM migraci po feature blocich, ne big-bang prepisem.
- [ ] Uklid theme runtime stylu (minimum vedlejsich efektu mezi themes).

---

## Done

- [x] Portable detection v `SettingsService` (`settings.json` vedle exe ma prednost pred AppData).
- [x] Lite profile (`LITE_BUILD`) - pouze Legacy theme + bez `splash.mp4`.
- [x] Welcome startup flow (first-run routing + save + fade transition).
- [x] Unified installer Full/Lite + Portable Lite artifact.
- [x] WebView2 runtime detekce v installeru.
- [x] CI dry-run artifacts + SHA256 files.
- [x] Welcome edge case: zavreni Welcome bez dokonceni ukonci app proces.
- [x] Installer excludes `settings.json` (instalovana app nespousti portable mode omylem).
- [x] Legacy start icon hotfix: zeleny trojuhelnik misto stylizovane play ikony.
- [x] CI dry-run version hotfix: installer app version bere verzi z `.csproj` (ne `0.0.<run>`).
- [x] README support note: jak znovu vyvolat Welcome screen na installed/portable buildu.
- [x] Sjednocené názvy témat: interní `AppTheme.*`, JSON hodnoty bez prefixu, GUI CZ/EN labely sjednocené.
- [x] Help menu: pridany odkaz na verejny GitHub repozitar projektu.
- [x] Dynamic verze v UI: status bar pouziva runtime verzi z assembly metadata (bez hardcode).
- [x] Startup log pouziva runtime verzi (bez staleho `v1.1` textu).
- [x] Startup log zobrazi stav `CheckUpdatesOnStartup` a realny vysledek kontroly pres GitHub Releases API.
- [x] Help menu: polozka `Uzivatelsky manual` odkazuje na dokumentaci projektu.
- [x] Help menu: `Uzivatelsky manual` preferuje lokalni soubor (Docs) a az pak fallback na web.

