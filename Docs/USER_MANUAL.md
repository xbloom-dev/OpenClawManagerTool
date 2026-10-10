# User Manual - OpenClaw Manager Tool by Bloom

English | [Česká verze](USER_MANUAL.cs.md) | [HTML version](USER_MANUAL.html)

**App version:** 2.0.5
**Updated:** 4 July 2026

## Contents

1. [Overview](#1-overview)
2. [Installation and startup](#2-installation-and-startup)
3. [Themes](#3-themes)
4. [Main window](#4-main-window)
5. [Gateway control](#5-gateway-control)
6. [Token Manager](#6-token-manager)
7. [Cleaning Tool](#7-cleaning-tool)
8. [Settings](#8-settings)
9. [Mail and calendar](#9-mail-and-calendar)
10. [Troubleshooting](#10-troubleshooting)

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

## 9. Mail and calendar

Open it from the main menu: **Open > Mail and calendar...**. The window is a control panel for the automatic mail and calendar processing that serves the OpenClaw agents. The app itself enforces **no** rules: every limit, rule and confirmation is enforced by the backend (the `control` folder of the mail and calendar installation, by default `E:\OpenClaw`). The window only shows the state and asks the backend to change it.

### Sending-state bar

The bar at the top of the window is always visible:

| Bar | Meaning |
|---|---|
| Calm: "Nothing leaves - shadow mode" | Replies are only prepared as `.eml` files. Nothing is sent. |
| Calm: "Nothing leaves - sending is off" | Real sending is switched off. |
| **Highlighted: "SENDING ENABLED"** | The agent can really send mail. |
| Highlighted: scope "open" | The agent may reply to any verified sender (not only the allowlist). |

### Sections

| Section | What it does |
|---|---|
| Overview | Mailbox, counters, statistics and the latest processed mails. |
| Settings | A form generated from the backend schema: every setting with its range, unit, default and source. A new backend setting appears without updating the app. |
| Sender rules | Rules per sender or domain (route and autonomy). |
| Proposals | Open proposals (past events, payments, reply drafts, scheduled replies). A draft can be edited before approval. |
| Outbox | Prepared `.eml` files with a read-only preview; only `auto-*.eml` and `reply-*.eml` can be deleted. |
| Recipients and templates | Allowlist of recipients, trusted event senders and reply templates. |
| Health and jobs | Script hashes against their pin, Ollama, plugins and scheduler jobs. Slow (tens of seconds), the result is cached for 30 s. |
| History and audit | Settings change history (with revert), send attempts and audit logs. |

### Confirmations and safety

- A change that widens what the agent may do (real sending, leaving shadow mode, scope "open", free autonomy, a new recipient or trusted sender, a payment with a finding) is confirmed **by the backend in a native Windows dialog**. The app only waits ("Waiting for confirmation in the Windows dialog") and then shows the result: saved, declined, or "could not be confirmed". Nothing changes when you answer No or do not answer within 2 minutes.
- Before such a change the field shows a warning that it moves away from the **safe value**. Moving back to the safe value never needs a confirmation (button "Safe value").
- A payment with a finding is shown in red and needs a **second explicit step** (tick the acknowledgement, then "Save anyway").
- Locked files (`gcal-config.json`, `mail-owner.json`, or files locked by `pin.ps1`) are read-only. They are changed in a terminal via `pin.ps1 -Unlock`.
- Subjects, names and drafts come from e-mails or the model. They are displayed as plain text and are never executed. The app never reads the `credentials` folder and never shows the app password (only whether it is set).
- Texts of labels and descriptions in the Settings section come from the backend, so they appear in the backend language.

### Backend location and simulator

The installation root is stored in `MailRootPath` in `settings.json` (detected automatically). If the backend is not found, the window offers **Choose folder...**. For development without Python, set the environment variable `OPENCLAW_MAILCONTROL_FAKE` to `decline`, `accept` or `needs` before starting the app: the window then runs against sample data and shows a red "SIMULATOR" banner.

## 10. Troubleshooting

| Problem | What to try |
|---|---|
| Welcome screen does not show video | Lite builds omit the video by design. Full builds fall back to a static image if the video cannot be loaded. |
| Help does not open | Check that the `Docs` folder exists next to the executable and contains `USER_MANUAL.html`. |
| Gateway status stays offline | Verify the Gateway command and inspect the Gateway log from the Tools section. |
| Settings are not saved | Installed builds need write access to app data. Portable builds need write access to the extracted folder. |
