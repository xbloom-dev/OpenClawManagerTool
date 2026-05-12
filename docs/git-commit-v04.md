# Git commit — OpenClaw Manager v0.4

Spusť v PowerShellu z `E:\OpenClaw\OpenClawManager\`:

```powershell
cd E:\OpenClaw\OpenClawManager

# 1. Zkontroluj co se změnilo
git status
git diff --stat

# 2. Přidej všechny soubory
git add .

# 3. Commit
git commit -m "feat: v0.4 polish - lokalizace EN/CS, tooltipy, latence, LiveLog

- Oprava parsování latencí (JSON parse + ANSI strip + file offset tracking)
- Lokalizace EN/CS přes ResourceDictionary (Strings.cs.xaml / Strings.en.xaml)
- L10n.cs service pro přepínání jazyka, ApplyLocalization() ve všech oknech
- Dialog O aplikaci se SVG logem (WebView2, světlé pozadí)
- Barvy tlačítek: zelená #D0FFD0 / červená #FFD0D0
- Token Manager aktivní (info dialog o v0.5)
- Tooltipy na všech tlačítkách ve všech oknech
- Klávesové zkratky kompletní (Ctrl+T/G/R, Ctrl+Shift+C, Ctrl+,, F1)
- Menu plně lokalizováno (EN/CS)
- AppSettings.Language property + přepínač v Settings okně
- Gateway Log: tlačítko Kopírovat s 2s fade feedback
- LiveLogWindow: FileSystemWatcher, sliding window N řádků, zvýraznění nových řádků
- Aktualizace dokumentace: spec.md v2.0, dev-manual.md v2.0, user-manual.md v1.0"

# 4. Tag verze
git tag -a v0.4 -m "v0.4 - Polish: lokalizace EN/CS, tooltipy, latence, LiveLog"

# 5. Ověř
git log --oneline -5
git tag
```

Po úspěšném commitu uvidíš výpis podobný:
```
abc1234 feat: v0.4 polish - lokalizace EN/CS, tooltipy, latence, LiveLog
...
```

A tag:
```
v0.1
v0.2
v0.3
v0.4
```
