# User Manual - OpenClaw Manager Tool by Bloom

English | [Česká verze](USER_MANUAL.cs.md) | [HTML version](USER_MANUAL.html)

**App version:** 2.0.4
**Updated:** 3 June 2026

## Contents

1. [Overview](#1-overview)
2. [Installation and startup](#2-installation-and-startup)
3. [Themes](#3-themes)
4. [Main window](#4-main-window)
5. [Gateway control](#5-gateway-control)
6. [Token Manager](#6-token-manager)
7. [Cleaning Tool](#7-cleaning-tool)
8. [Settings](#8-settings)
9. [Troubleshooting](#9-troubleshooting)

## 1. Overview

OpenClaw Manager is a Windows desktop helper for managing and diagnosing a local OpenClaw environment. It helps you start the TUI, control the Gateway, inspect logs, measure latency, clean temporary files, and manage local tokens.

The tool is designed for local maintenance. It does not replace OpenClaw itself and it does not require an online help page to open this manual.

## 2. Installation and startup

### Installed build

Use the installer and launch the app from the Start menu or desktop shortcut. Installed builds store settings in the standard Windows app data location.

### Portable build

Extract the portable ZIP into a writable folder. A portable build uses the local `settings.json` placed next to the executable, so settings stay with the extracted folder.

### First run

The Welcome screen lets you choose language, theme, and whether update checks should run at startup. Closing the Welcome screen before finishing closes the app.

## 3. Themes

The app uses seven internal theme identifiers. The user interface shows localized display names.

| Internal ID | English name | Notes |
|---|---|---|
| `Legacy` | Legacy | Classic default look. Do not change this theme unless explicitly approved. |
| `StandardLight` | Standard | Light standard interface. |
| `StandardDark` | Standard Dark | Dark standard interface. |
| `ModernDark` | Modern Dark | Modern glass-styled dark interface. |
| `ModernLight` | Modern | Modern light interface. |
| `HighContrast` | High Contrast | Higher contrast for readability. |
| `CrabCute` | Crab Cute | Playful visual variant. |

Lite builds intentionally expose only the Legacy theme and omit the video splash file.

## 4. Main window

The main window is split into actions, tools, latency information, app log, and terminal output.

| Area | Purpose |
|---|---|
| Actions | Start OpenClaw TUI and control the Gateway. |
| Tools | Open PowerShell, Gateway log, Cleaning Tool, Token Manager, and Doctor Fix. |
| Latency | Shows the last, average, maximum, and count values from Gateway checks. |
| App log | Shows local application events and diagnostic messages. |
| Terminal | Embedded terminal surface for OpenClaw output. |

### Keyboard shortcuts

- `Ctrl+,` opens Settings.
- `F1` opens this local manual.
- `Alt+F4` closes the active window.

## 5. Gateway control

The Gateway buttons can start, stop, or restart the local Gateway process. The status bar shows whether the Gateway appears to be running, including process ID and uptime when available.

If the Gateway does not start, open Settings and verify the OpenClaw command, working directory, and log folder paths.

## 6. Token Manager

Token Manager stores and edits local provider tokens. Sensitive values are protected with Windows data protection where available. Portable mode may have different protection boundaries because the settings are stored next to the executable.

Keep backups secure. Anyone with access to exported or copied token files may be able to use those credentials.

## 7. Cleaning Tool

Cleaning Tool removes selected temporary files, logs, and stale local data. Review selections before running cleanup, especially when working from a portable folder.

## 8. Settings

Settings controls paths, theme, language, startup behavior, and update-check preference. If the app behaves unexpectedly after a theme or path change, restart it once and verify the values again.

## 9. Troubleshooting

| Problem | What to try |
|---|---|
| Welcome screen does not show video | Lite builds omit the video by design. Full builds fall back to a static image if the video cannot be loaded. |
| Help does not open | Check that the `Docs` folder exists next to the executable and contains `USER_MANUAL.html`. |
| Gateway status stays offline | Verify the Gateway command and inspect the Gateway log from the Tools section. |
| Settings are not saved | Installed builds need write access to app data. Portable builds need write access to the extracted folder. |
