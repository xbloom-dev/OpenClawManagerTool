# OpenClaw Manager Tool

[![CI](https://github.com/xbloom-dev/OpenClawManagerTool/actions/workflows/ci.yml/badge.svg)](https://github.com/xbloom-dev/OpenClawManagerTool/actions/workflows/ci.yml)

A Windows desktop app for managing a local [OpenClaw](https://github.com/openclaw/openclaw) environment — start and stop the Gateway, open the TUI, watch logs, run cleanup, manage settings and themes, and store provider tokens in a DPAPI-protected vault. All from a single window, without memorizing CLI flags.

> **What is OpenClaw?** OpenClaw is an open-source AI-agent framework. Its **Gateway** is a local daemon that connects your agents to AI providers (Anthropic, OpenAI, Ollama, …), and its **TUI** is a terminal dashboard for monitoring sessions, logs, and model usage. See the [OpenClaw project](https://github.com/openclaw/openclaw) and [docs.openclaw.ai](https://docs.openclaw.ai) to install and configure OpenClaw itself.
>
> **This is an independent, community-built tool.** It is not affiliated with or endorsed by the OpenClaw project. It simply wraps an existing OpenClaw installation in a Windows GUI.

---

## Features

- **Gateway control** — start, stop, and restart the OpenClaw Gateway from buttons instead of the command line.
- **Embedded TUI** — open the OpenClaw terminal interface in an in-app terminal (xterm.js + ConPTY + WebView2).
- **Live logs** — Gateway log viewer with copy and live-tail (`Ctrl+L`).
- **Cleaning tool** — preview and remove stale files with a dry-run mode.
- **Token Manager** — store API keys and tokens encrypted with Windows DPAPI, with masked previews and file redaction.
- **Portable vault backup** — export/import the vault as a password-protected `.ocvault` file (PBKDF2-SHA256 + AES-256-GCM).
- **Themes** — Legacy, Modern, Standard Dark, Modern Dark, Modern Light, High Contrast, and Crab Cute.
- **Offline by default** — the embedded terminal ships local xterm.js assets, so it needs no CDN or internet at runtime.
- **Bilingual UI** — English and Czech.

---

## Requirements

- Windows 10 or 11 (x64)
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (only if you run the framework-dependent build; the self-contained release bundles it)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on current Windows; the app falls back gracefully if missing)
- A working [OpenClaw](https://github.com/openclaw/openclaw) installation that the tool can manage

---

## Installation

### Option 1 — Download a release (recommended)

1. Go to the [Releases](../../releases) page.
2. Download `OpenClawManagerTool-v2.0.0-win-x64.zip`.
3. (Optional) Verify the download against the published `.sha256` file.
4. Extract anywhere and run `OpenClawManager.exe`.

### Option 2 — Build from source

See [Building from source](#building-from-source) below.

---

## First run

1. Start `OpenClawManager.exe`.
2. Open **Settings** and confirm your OpenClaw folders and command path.
3. Open **Token Manager** and initialize the vault.
4. Keep the vault **outside** project folders, `.openclaw`, Git repositories, and cloud-synced folders.

---

## Token vault safety

Token values are encrypted with **Windows DPAPI** for the current Windows user. A copied vault cannot be decrypted on another Windows profile or machine. Token Manager warns if the vault is placed in a risky location.

Recommended vault location:

```
%USERPROFILE%\.token-manager\secrets.json
```

Avoid project folders, `.openclaw`, Git repositories, cloud-sync folders, and shared folders.

**Portable backups.** You can export the vault to a portable `.ocvault` file protected by a password you choose (PBKDF2-SHA256, AES-256-GCM). Importing a backup re-encrypts it into the local DPAPI vault for the current Windows profile. Keep backup files and their passwords somewhere safe and separate.

---

## Themes

Switch themes in **Settings**: Legacy, Modern, Standard Dark, Modern Dark, Modern Light, High Contrast, and Crab Cute. Modern themes use bitmap icons and themed secondary windows; Legacy keeps the simpler classic layout.

Language, theme, paths, and the Token Manager vault path are saved in:

```
%APPDATA%\OpenClawManager\settings.json
```

---

## Offline terminal

The embedded terminal uses local xterm.js files under `Resources/Terminal`, so it works without CDN or internet access at runtime.

---

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
# Build
dotnet build

# Run tests
dotnet run --project TokenService.Tests\TokenService.Tests.csproj

# Publish a self-contained single-file build
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## Architecture (v2.0)

OpenClaw Manager v2.0 uses a clean MVVM + DI architecture:

- **Dependency Injection** via Microsoft.Extensions.Hosting
- **MVVM** for all windows (MainWindow, TokenManager, GatewayLog, LiveLog, Settings, Cleaning)
- **Async UI** with status polling, WMI checks, and log reads kept off the UI thread
- **ITokenService** interface coverage for Token Manager workflows while preserving the audited DPAPI/AES-GCM implementation

---

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request, and report security issues privately as described in [SECURITY.md](SECURITY.md).

---

## Security

This app handles API tokens and secrets. If you discover a vulnerability, **do not** open a public issue — follow the disclosure process in [SECURITY.md](SECURITY.md).

---

## Development notes

This project is built by Bloom with assistance from AI coding agents. Code, tests, and documentation are reviewed before merging.

---

## License

This project is released under the terms of the [MIT License](LICENSE).
