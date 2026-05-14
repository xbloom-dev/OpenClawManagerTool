# SplashScreen a WebView2 - bezpecnostni pravidla v1.0

## Proc je tato cast kriticka

SplashScreen a WebView2 byly nejcitlivejsi casti ladeni v1.0. Problem se SplashScreenem se opravoval opakovane. Dalsi upravy musi byt konzervativni a vzdy overene manualnim GUI smoke testem.

## Aktualni architektura

- `SplashBorder` = Legacy ASCII art uvnitr `TerminalControl`
- `SplashOverlay` = Modern splash vrstva v `MainWindow`
- Modern rezim pouziva video `splash.mp4` a nasledny PNG freeze frame
- Legacy rezim pouziva ASCII art
- TUI terminal bezi ve WebView2

## Zasadni pravidla pro SplashScreen

1. `SplashBorder` a `SplashOverlay` nemichat dohromady.
2. `MediaEnded` nesmi kompletne zavrit splash.
3. Po dobehnuti videa se ma zobrazit PNG freeze frame.
4. Splash mizi az pri kliknuti na OpenClaw TUI.
5. Pri Modern -> Legacy:
   - zastavit video
   - skryt Modern overlay
   - zobrazit ASCII art
6. Pri Legacy -> Modern:
   - skryt ASCII art
   - zobrazit Modern overlay nebo PNG
7. `OnThemeChanged` musi respektovat `IsTuiRunning`.
8. Pokud chybi `splash.mp4` nebo PNG fallback selze, zobrazit ASCII art.
9. `MediaElement.Source = null` a `UnloadedBehavior="Stop"` jsou dulezite pro uvolneni videa.
10. Nemenit `MediaElement` na jiny prehravac bez silneho duvodu.

## WebView2 HWND airspace problem

WebView2 je HWND control a muze vykreslovat pres WPF vrstvy jinak, nez by clovek cekal. Proto:

- WebView2 nesmi byt zobrazeny driv, nez zmizi splash overlay.
- WebView2 ma byt na startu `Hidden`, ne `Collapsed`.
- `ShowWebView()` se smi volat az po `DisposeSplash()`.
- Nepouzivat opravy, ktere by WebView2 znovu zviditelnily pod overlayem.

## Encoding v HTML/JS

Historicky problem:

`Ceka se na ConPTY...` se zobrazovalo jako garbled text.

Pricina:

- UTF-8 string byl interpretovan jako Latin-1 nebo vlozen do raw HTML/JS nevhodnym zpusobem.

Bezpecne postupy:

- v HTML pouzit `meta charset="utf-8"`
- pri cteni resource streamu explicitne pouzit UTF-8
- non-ASCII znaky v JS stringu pripadne escapovat jako `\uXXXX`
- nepouzivat ad hoc prevody encodingu

## Smoke test po kazde uprave Splash/WebView2

Minimalni test:

1. Start v Modern theme.
2. Overit prehrani `splash.mp4`.
3. Overit PNG freeze frame po konci videa.
4. Kliknout na OpenClaw TUI.
5. Overit, ze se zobrazi terminal.
6. Start v Legacy theme.
7. Overit ASCII art.
8. Prepnout Modern -> Legacy pred spustenim TUI.
9. Prepnout Legacy -> Modern pred spustenim TUI.
10. Overit fallback pri chybejicim `splash.mp4`.
11. Overit fallback pri chybejicim/poskozenem PNG.

