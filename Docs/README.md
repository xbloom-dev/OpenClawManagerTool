# OpenClaw Manager Tool by Bloom v2.0

Windows WPF utility for managing an OpenClaw environment: Gateway/TUI control, embedded terminal, logs, cleanup, settings, themes, and Token Manager.

## Shared Encoding Rule (Agents)

All shared Markdown documentation must be saved as **UTF-8** and keep valid Czech diacritics.
Do not commit broken encoding artifacts (for example: `Ä›`, `Ăˇ`, `Å™`).

## First Run

1. Start `OpenClawManager.exe`.
2. Open Settings (Ctrl+,) and confirm the OpenClaw folders and command path.
3. Open Token Manager and initialize the vault.
4. Keep the vault outside project, `.openclaw`, Git, and cloud-synced folders.

## Token Vault Safety

Token values are stored with Windows DPAPI for the current Windows user. A copied vault cannot be decrypted on another Windows user profile or machine. Token Manager still warns if the vault is placed in risky locations.

Use `%USERPROFILE%\.token-manager\secrets.json` or another private, non-synced folder for the vault. Avoid project folders, `.openclaw`, Git repositories, cloud sync folders, and shared folders.

## Themes

Seven themes are available: Theme.Legacy, Theme.StandardLight, Theme.StandardDark, Theme.ModernDark, Theme.ModernLight, Theme.HighContrast, and Theme.CrabCute. Language, theme, paths, and Token Manager vault path are saved in `%APPDATA%\OpenClawManager\settings.json`.

## Offline Terminal

The embedded terminal uses local xterm.js files included under `Resources/Terminal`, so it does not need CDN or internet access at runtime.

## Smoke Test

- Start the app and confirm the status bar shows `v2.0`.
- Start Gateway.
- Open OpenClaw TUI.
- Open Gateway Log and Live Log.
- Run Cleaning Tool in preview mode.
- In Token Manager, add a token, redact a sample file, verify the redacted file, then restore it.
- Open Settings and About.
- Switch between at least 2 themes and confirm correct appearance.

## Build

```powershell
dotnet build
dotnet run --project TokenService.Tests\TokenService.Tests.csproj
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## Release Notes

See `Docs/CHANGELOG.md` for the current release summary.


