# OpenClaw Manager v0.95 - Audit Package

Date: 2026-05-12

Contents:
- source/: clean source tree without bin/obj
- release/: published win-x64 self-contained single-file build

Verification completed:
- dotnet build -c Release: OK, 0 warnings, 0 errors
- TokenService.Tests: OK
- dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true: OK
- GUI smoke harness: MainWindow, TokenManagerWindow, CleaningWindow, SettingsWindow, AboutWindow: OK
- Published app opens and shows v0.95: OK

Residual note:
- Codex automated desktop input could not reliably complete the embedded WebView2 terminal readiness check. Please confirm the terminal once manually on a normal interactive Windows desktop.
