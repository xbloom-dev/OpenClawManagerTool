# OpenClaw Manager Tool by Bloom — Vývojářský manuál

Tento manuál doplňuje `Docs/README.md`, `COLOR_STYLE_GUIDE.md` a `SPLASH_WEBVIEW2_SAFETY.md`.
Soubor vznikl s modulem „Pošta a kalendář“; obecná pravidla projektu jsou shrnuta níže, architektura modulu je v kapitole 2.

---

## 1. Základní pravidla

- Texty rozhraní patří jen do `Resources/Lang/Strings.cs.xaml` a `Strings.en.xaml` (česky s diakritikou), nic natvrdo. Test `LocalizationKeysExistInBothLanguages` hlídá, že všechny klíče `Str_MailControl_*` použité v kódu existují v obou jazycích a že v XAML okna není natvrdo text ani barva.
- Barvy jen z theme zdrojů (`DynamicResource`, `ThemeService.GetBrush()`), viz `COLOR_STYLE_GUIDE.md`. Do `ThemeService.cs`, `MainWindow.Splash.cs` a `Views/TerminalControl.xaml.cs` nesahat (citlivé oblasti, jen jeden agent najednou).
- Nastavení: při každé změně `AppSettings` zvýšit `CurrentSchemaVersion`, doplnit migraci v `SettingsService.MigrateSettings` a přidat test migrace.
- Do `MainWindow.xaml(.cs)` zasahovat minimálně (sdílený soubor).
- Změny jdou přes Pull Request do `develop`; `master` se nemění přímo. Destruktivní git příkazy jen po výslovném souhlasu.
- Build bez varování a testy: `run-tests.ps1` (`dotnet build` + `dotnet run --project TokenService.Tests`).

---

## 2. Modul „Pošta a kalendář“ (ovládání backendu)

### Role

Aplikace je **jen ovládací panel** komponenty automatického zpracování pošty a kalendáře (Python, `<kořen>\control`, `mail`, `gcal`). Žádná pravidla, limity ani potvrzování zde nejsou: všechno vynucuje backend, potvrzení rizikových změn dělá nativním dialogem Windows. Aplikace nikdy nepíše do konfiguračních souborů backendu, nečte složku `credentials` a nezobrazuje ani neloguje tajné údaje.

Závazný kontrakt: `E:\OpenClaw\Dokumentace\Kalendar-a-posta\API-ovladaci-aplikace.md` a `control\schema.json` (kopie je ve fixtures).

### Vrstvy

| Vrstva | Soubor | Úloha |
|---|---|---|
| DTO a parsování | `Models/MailControlModels.cs` | tolerantní čtení obálky `{"ok":…}`, nastavení, stav, návrhy, tabulky; `PlainText.Sanitize` pro nedůvěryhodný text |
| Runner | `Services/MailControlRunner.cs` | `IMailControlRunner`; `ProcessMailControlRunner` spouští `python.exe control_run.py <příkaz> --args-json …` (`ArgumentList`, UTF-8, `PYTHONUTF8=1`); `MailControlPaths.DetectRoot` |
| Simulátor | `Services/FakeMailControlRunner.cs` | stejný kontrakt nad fixtures a stavem v paměti; řízení potvrzení (`Decline`/`Accept`/`NeedsConfirmation`), zamčené soubory, vynucená chyba |
| Klient | `Services/MailControlClient.cs` | typované metody pro každý příkaz, časové limity (čtení 30 s, zápis 150 s kvůli dialogu, `health` 100 s) |
| Formulář | `Services/MailControlForm.cs` | `SettingControlKind`, `BuildGroups`, klientské ověření, `MovesAwayFromSafeValue`, mapování chybových kódů |
| ViewModely | `ViewModels/MailControlViewModel*.cs`, `MailControlSettingsViewModels.cs` | jeden ViewModel rozdělený na oddíly (Overview, Settings, Rules, Proposals, Outbox, People, Health, History) |
| Okno | `Views/MailControlWindow.xaml(.cs)` | navigace (ListBox, aby vzhled držela všechna témata), stálý pruh stavu, oddíly |
| Fixtures | `Resources/MailControl/Fixtures/*.json` | vložené jako `EmbeddedResource` (`OpenClawManager.MailControlFixtures.<název>.json`); `schema`, `settings`, `state` jsou reálný tvar, ostatní syntetická data bez osobních údajů |

### Rozhodnutí, na která se vyplatí pamatovat

1. **Formulář nastavení se generuje ze schématu.** Do kódu se nezadávají klíče. Typ `bool` → přepínač, `int` → číselné pole s rozsahem, `enum` → výběr, `text` → textové pole (u textu je `max` nejvyšší délka). Neznámý typ spadne do textu. Skupiny a jejich pořadí jsou ze schématu. Test `FormIsGeneratedFromSchema` ověřuje každý klíč.
2. **Stálý pruh stavu** se počítá z `state.sending`. Když pole `mode` chybí nebo je neznámé, režim se dopočítá z přepínačů, a to tak, aby skutečné odesílání nikdy nezůstalo skryté. Neznámý stav (backend neodpověděl) se nikdy nezobrazí jako „nic neodchází“.
3. **Potvrzování.** Aplikace před voláním jen upozorní, že se pole přesouvá od `safe_value`; návrat na bezpečnou hodnotu je bez potvrzení. Zápisy, které mohou čekat na dialog, mají limit 150 s a v UI stav „Čeká se na potvrzení v dialogu Windows“. Po `declined` / `needs_confirmation` / `locked` se pole vrátí na hodnotu z backendu.
4. **Platba s nálezem** (`needs_explicit_override`) se ukazuje červeně; „ano“ se pošle jen s `override=true` a jen po zaškrtnutí potvrzení (druhý krok).
5. **Nedůvěryhodný text** (předměty, odesílatelé, koncepty, tabulky auditu) jde přes `PlainText.Sanitize` / `ToPlainCell` a zobrazuje se jen v `TextBlock.Text` / `TextBox.Text`; žádný HTML ani Run s markupem. Do logu aplikace se nic z toho nezapisuje.
6. **Obecné tabulky** (historie, odeslané, audit, úlohy) nemají pevné sloupce: sloupce se odvodí z klíčů řádků (`BindTableColumns`), takže nová pole backendu se zobrazí sama. Záhlaví se překládá z `Str_MailControl_Col_<klíč>`, jinak se použije klíč.
7. **Popisky nastavení** (label, description, skupiny, kategorie) dodává backend česky; v EN rozhraní zůstávají v jazyce backendu (viz „Požadavky na backend“ v handoffu).
8. **Jazyk a téma** se nepřepínají za běhu okna (nastavení je modální), proto stačí lokalizace při vytvoření okna. Záhlaví sloupců zapsaná v XAML jako `Str_…` se nahrazují v `LocalizeColumnHeaders`.
9. **Přepínání témat za běhu:** při automatizovaném vykreslování okna (pomocný nástroj, ne součást produktu) jednou skončilo přepnutí na ModernDark po jiných tématech chybou `Theme.Style.DarkScrollThumb` nenalezen. Pozdější kontrola s holým oknem (ScrollViewer) po přepnutí z každého z ostatních témat na ModernDark chybu **nezopakovala**, takže šlo nejspíš o artefakt pomocného nástroje (zároveň běžel start aplikace s vlastním MainWindow). Při vykreslování přesto pouštěj každé téma ve vlastním procesu.

### Ověření bez Pythonu a naživo

- `FakeMailControlRunner` + fixtures: všechny testy modulu běží bez Pythonu (`TokenService.Tests/MailControlTests.cs`).
- Okno nad simulátorem: nastav `OPENCLAW_MAILCONTROL_FAKE=decline|accept|needs` před spuštěním aplikace (červený pruh „SIMULÁTOR“).
- Živé čtení: `OPENCLAW_MAILCONTROL_LIVE=1` zapne v testech jen čtecí kontrolu (`schema`, `state`, `settings`, `proposals`) proti skutečnému backendu. **Nikdy** při ověřování naživo nezapínej `send_enabled`, stínový režim `off` ani `auto_reply_scope=open`; to je výhradně rozhodnutí Blooma. Měň jen nastavení typu `free` (např. `sender_name`) a vrať původní hodnotu.

### Okno pro těžké úlohy (`heavy_window_*`)

Klíče `heavy_window_enabled/day/start/end` se v Nastavení objeví automaticky, jakmile je backend přidá do schématu (nic v aplikaci se nemění; formát `HH:MM` kontroluje `pattern` ze schématu před voláním). Aplikace jen v Přehledu ukáže `stats.paused_runs` (běhy pošty přeskočené v okně za 7 dní), a jen když ho backend vrací.

### Jak přidat příkaz nebo nastavení

- **Nové nastavení v backendu**: v aplikaci nic. Objeví se samo (jen texty z backendu).
- **Nový příkaz**: metoda v `MailControlClient`, DTO v `MailControlModels.cs`, test nad fixture (přidej `Resources/MailControl/Fixtures/<příkaz>.json` a větev v `FakeMailControlRunner.Dispatch`), oddíl nebo akce ve ViewModelu, klíče `Str_MailControl_*` v obou jazycích.
- **Nová chyba**: kód přidej do `MailControlErrors.Known` a texty `Str_MailControl_Err_<kód>` do obou jazyků (test `ErrorCodesAreMapped` čte seznam kódů ze schématu).
