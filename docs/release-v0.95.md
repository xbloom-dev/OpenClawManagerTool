# OpenClaw Manager v0.95

v0.95 is the audit candidate build. It includes all P0/P1 stabilization work and the P2 documentation, smoke-test, and packaging polish.

## Highlights

- Embedded terminal uses local `Resources/Terminal/xterm.min.js` and `xterm.min.css`; no CDN is needed at runtime.
- WebView2 now uses an isolated per-process user data folder with a writable fallback next to the app.
- Token Manager vault values are written encrypted with Windows DPAPI. Existing plaintext vaults are transparently migrated on the next save.
- Token Manager shows vault safety warnings for Git, OpenClaw, project, shared, and cloud-synced locations.
- Token Manager adds Open vault folder, Copy placeholder, Settings refresh, and cached Git tracking checks.
- Settings validates `OpenClawCommand` before saving to avoid unsafe shell characters.
- LiveLog uses a reusable highlight timer instead of creating one timer per update.
- User documentation now covers Token Manager, themes, and the release smoke checklist.
- App version updated to `0.95.0`.

## Verification

- `dotnet build -c Release`
- `TokenService.Tests`
- `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true`
- GUI smoke: MainWindow, TokenManagerWindow, CleaningWindow, SettingsWindow, and AboutWindow open and close successfully.

## Residual Smoke Note

Automated desktop input in the Codex environment cannot reliably complete the embedded WebView2 terminal check. The published app opens and shows `v0.95`, but the terminal readiness check should still be confirmed once manually on a normal interactive Windows desktop.
