# User Manual — OpenClaw Manager Tool by Bloom

*English · [Czech version](USER_MANUAL.cs.md)*

**App version:** v2.0
**Date:** 25 May 2026

---

## Contents

1. [What OpenClaw Manager is](#1-what-openclaw-manager-is)
2. [Installation and startup](#2-installation-and-startup)
3. [Themes and splash screen](#3-themes-and-splash-screen)
4. [Main window](#4-main-window)
5. [Keyboard shortcuts](#5-keyboard-shortcuts)
6. [Starting the OpenClaw TUI](#6-starting-the-openclaw-tui)
7. [Gateway control](#7-gateway-control)
8. [Latency measurement](#8-latency-measurement)
9. [Gateway log](#9-gateway-log)
10. [Token Manager](#10-token-manager)
11. [Cleaning Tool](#11-cleaning-tool)
12. [Settings](#12-settings)
13. [About](#13-about)
14. [Troubleshooting](#14-troubleshooting)

---

## 1. What OpenClaw Manager is

OpenClaw Manager is a diagnostic and maintenance tool for an [OpenClaw](https://github.com/openclaw/openclaw) environment. It is most useful **when something is not working correctly** — it lets you quickly restart the Gateway, clean up old files, measure latency, and diagnose problems.

> **What is OpenClaw?** OpenClaw is an open-source AI-agent framework. Its **Gateway** is a local daemon that connects agents to AI providers (Anthropic, OpenAI, Ollama, …), and its **TUI** is a terminal dashboard for monitoring sessions, logs, and model usage. This tool is an independent wrapper — it manages an existing OpenClaw installation through a Windows GUI and is not affiliated with the OpenClaw project.

It is **not** a replacement for launching OpenClaw day to day — a shortcut or Scheduled Task is enough for normal use. OpenClaw Manager is handy when you need to:

- Restart the Gateway after an update or crash
- Check how fast the Gateway responds (latency measurement)
- Clean up old logs and session files
- Securely manage API keys and tokens (including a portable backup)
- Repair a broken configuration

---

## 2. Installation and startup

### Requirements

- Windows 10 or 11 (x64)
- WebView2 Runtime (included in Windows 11; for Windows 10 download from microsoft.com)
- A working OpenClaw installation

### Startup

Run `OpenClawManager.exe` from the runtime package. No installation is required — the app is portable. The terminal uses local xterm.js files, so no internet or CDN is needed.

### Configuring paths on first run

On first run, open **Settings** (Ctrl+,) and confirm the paths match your environment:

| Item | Default value |
|---|---|
| OpenClaw folder | `~\.openclaw` |
| Temp folder (Gateway logs) | `%LOCALAPPDATA%\Temp\openclaw` |
| openclaw command | `openclaw` (PATH lookup) |
| PowerShell working directory | `%APPDATA%\npm` |
| Vault | `%USERPROFILE%\.token-manager\secrets.json` |

If you do not run OpenClaw via `openclaw` on the PATH, set the "openclaw command" to the full path, for example `C:\Users\name\AppData\Roaming\npm\openclaw.cmd`.

---

## 3. Themes and splash screen

Change the theme in **Settings**. OpenClaw Manager v2.0 offers **seven themes**:

| Theme | Description |
|---|---|
| **Legacy** | Classic, simpler layout with emoji icons on buttons. Splash is ASCII art. |
| **Modern** | Bitmap icons, dark-purple splash background, themed secondary windows. |
| **Standard Dark** | Dark variant of the default look. |
| **Modern Dark** | Dark theme with the modern icon set. |
| **Modern Light** | Light modern theme. |
| **High Contrast** | High contrast for better readability. |
| **Crab Cute** | Playful icon and color set. |

Modern themes use bitmap icons and themed secondary windows; Legacy keeps the simpler classic layout.

### Splash screen

The splash panel behavior at startup depends on the theme:

**Legacy:** an ASCII-art splash appears in the right panel and disappears when you click **OpenClaw TUI**.

**Modern (and derived themes):**
1. `splash.mp4` plays (if present in `Resources/`)
2. After the video finishes, a static `splash.png` remains as a freeze frame
3. The overlay disappears when you click **OpenClaw TUI**

If `splash.mp4` is missing or fails to play, `splash.png` is shown directly. If `splash.png` is also missing, ASCII art is shown as a fallback.

### Switching at runtime

A theme change takes effect immediately after you save Settings. When switching between splash styles (Modern ↔ Legacy) before starting the TUI, the video stops / ASCII art switches according to the new theme.

---

## 4. Main window

The window is split into two parts:

**Left panel (330 px)** — controls, latency measurement, application log
**Right panel** — embedded terminal (OpenClaw TUI)

### Actions section

#### OpenClaw TUI button

The app's main button. It has **3 modes** depending on the current state:

| State | Color | Action |
|---|---|---|
| Gateway not running | 🟢 green | Starts Gateway + waits for ready + starts the TUI |
| Gateway running | 🟢 green | Starts only the TUI (leaves Gateway running) |
| TUI running | 🔴 red | Stops the TUI (leaves Gateway running) |

#### Gateway buttons

- **Start** — runs `openclaw gateway` in the background in a new PowerShell window
- **Stop** — stops the running Gateway (shows a confirmation dialog)
- **Restart** — stops and restarts the Gateway; the TUI disconnects

#### Open

- **PowerShell** — opens PowerShell in the working directory set in Settings
- **Gateway log** — opens a dialog showing the Gateway log
- **Live data** (Ctrl+L) — opens live monitoring of the Gateway log

#### Tools

- **Clean files** — opens the Cleaning Tool
- **Token Manager** — manage the token and API-key vault

#### Maintenance

- **Repair configuration** — runs `openclaw "doctor --fix"` in PowerShell

### Status bar (bottom)

`Gateway: ● state | PID: X | uptime: H:MM:SS | RAM: X/Y GB | VRAM: X/Y GB | CPU: X% | v2.0`

| Dot color | State |
|---|---|
| 🟢 green | Gateway running |
| 🟠 orange | Gateway starting |
| 🔴 red | Gateway failed |
| ⚫ gray | Gateway not running |

RAM, CPU, and VRAM refresh every 2 seconds in the background — the UI does not freeze.

---

## 5. Keyboard shortcuts

| Shortcut | Action |
|---|---|
| **Ctrl+T** | Start/Stop OpenClaw TUI |
| **Ctrl+G** | Start or Stop Gateway |
| **Ctrl+R** | Restart Gateway |
| **Ctrl+L** | Gateway log live data |
| **Ctrl+Shift+C** | Open Clean files |
| **Ctrl+,** | Open Settings |
| **F1** | About |
| **Alt+F4** | Quit |

---

## 6. Starting the OpenClaw TUI

### Standard procedure

1. Click the **▶ OpenClaw TUI** button (or Ctrl+T)
2. The app automatically:
   - Deletes the old Gateway log (for clean latency measurement)
   - Starts the Gateway
   - Waits for "gateway ready" (up to 3 minutes)
   - Starts the TUI in the embedded terminal on the right
3. Follow progress in "Application log" at the bottom left

### If the Gateway is already running

Clicking starts the TUI directly without restarting the Gateway.

### Stopping the TUI

Click the **■ Stop OpenClaw TUI** button (red). The Gateway keeps running.

### Closing the app with the Gateway running

On close, a dialog with three options appears:
- **Yes** — stops the Gateway and closes the app
- **No** — closes the app, leaves the Gateway running in the background
- **Cancel** — returns to the app

---

## 7. Gateway control

### Starting the Gateway

The **Start** button or Ctrl+G. The Gateway starts in a new PowerShell window. The app waits for "gateway ready" — the status bar shows "starting…" until the Gateway is ready.

### Stopping the Gateway

The **Stop** button or Ctrl+G (if the Gateway is running). A confirmation dialog appears.

**Note:** Stopping the Gateway interrupts all active TUI sessions. The Scheduled Task that auto-starts it at logon is **not** changed — the Gateway will start again at the next logon.

### Restarting the Gateway

The **Restart** button or Ctrl+R. The TUI disconnects, the Gateway stops and restarts. After a successful restart, the TUI restarts automatically.

---

## 8. Latency measurement

The **Latency** section in the left panel shows how fast the Gateway responds:

| Value | Description |
|---|---|
| Last | Latency of the last request |
| Avg 10× | Moving average of the last 10 requests |
| Maximum | Highest latency measured since start/restart |
| Requests | Total request count since start/restart |

**How to read the values:**
- 🟢 green — under 1,000 ms (fast response)
- black — 1,000–5,000 ms (normal)
- 🔴 red — over 5,000 ms (slow response, possible overload)

Values start appearing only after the first request sent through the TUI.

---

## 9. Gateway log

Open it via the **Gateway log** button or the **Open → Gateway log** menu.

### Viewing the log

Choose the number of lines shown (default: last 20) and click **Refresh**. The file path, size, and line count are shown at the top.

### Copy to clipboard

Click **Copy** — the entire content is copied to the clipboard. After clicking, "✓ Copied" appears for 2 seconds.

### Live monitoring (Ctrl+L)

Click **Live data** to open the live window:

- Opens next to the main app (does not lock the controls)
- Automatically detects new entries
- Oldest entries are continuously removed (always the last N lines)
- New lines are marked `► ` for 2 seconds
- The **Copy** button copies clean content (without the `► ` prefixes)

---

## 10. Token Manager

Open it with the **Token Manager** button in the left panel. It securely stores API keys and sensitive values, redacts files before you share them, and provides a portable vault backup.

### The vault and security

The vault is a `secrets.json` file at the path set in Settings (default `%USERPROFILE%\.token-manager\`). Token values are encrypted with **Windows DPAPI** for the current user. A vault copied to another computer or another account cannot be decrypted.

Token Manager warns if the vault is in a risky location:
- inside a Git repository
- inside `.openclaw`
- in a cloud-synced folder (OneDrive, Dropbox, iCloud)
- in a shared or project folder

**Recommendation:** use `%USERPROFILE%\.token-manager\` outside projects, Git, and cloud sync.

### Initializing the vault

On first open, click **Initialize vault**. If an older unencrypted vault exists, the app automatically migrates it to the encrypted version on the first save.

### Adding and managing tokens

Click **Add** and fill in:
- **ID** — a unique identifier without spaces (e.g. `OPENAI_KEY`) — used as the placeholder `[REDACTED_OPENAI_KEY]`
- **Value** — the secret itself
- **Description** — an optional note

**Edit** — leave Value empty to keep the existing value.
**Rotate** — sets a new value without losing the ID and description.
**Delete** — removes the token from the vault.

### Vault backup and restore (`.ocvault`)

The vault is bound to your Windows profile (DPAPI), so it cannot simply be copied to another machine. For transfer or a safety backup, use the portable `.ocvault` format:

- **Backup** — exports the vault to an `.ocvault` file protected by a **password** you choose. The file is encrypted with **PBKDF2-SHA256** (200,000 iterations) and **AES-256-GCM** — independent of DPAPI, so it can be restored on another computer.
- **Restore** — loads an `.ocvault` after you enter the password and re-saves it into the local DPAPI vault for the current Windows profile.

You enter the password in a separate dialog (`PasswordPromptWindow`). Both export and import use Save/Open dialogs to choose the `.ocvault` file.

> ⚠️ **Backup security:** keep the `.ocvault` file and its password separate and in a safe place. Anyone with both gets access to your tokens. Do not store the backup in a Git repository or a cloud-sync folder next to the password.

### File redaction and restore

**Redaction workflow:**
1. Select a token and click **Preview** — shows how the file will look after redaction
2. Click **[REDACT]** — replaces values with the placeholder `[REDACTED_ID]`
3. Before sharing, click **Verify** — checks that the file contains no plaintext value

**Un-redact:** click **Restore** — replaces placeholders back with values. A `.bak` backup is created automatically.

### Button overview

| Button | Color | Function |
|---|---|---|
| **[REDACT]** | 🟢 green, bold | Masks sensitive values in a file |
| **Verify** | 🔵 blue, bold | Checks that a file contains no plaintext value |
| **Preview** | 🔵 blue | Shows a redaction preview without writing |
| **Browse** | 🔵 blue | File selection |
| **Backup** | 🔵 blue | Exports the vault to a password-protected `.ocvault` |
| **Restore (vault)** | 🔵 blue | Restores the vault from an `.ocvault` |
| **Restore (file)** | 🔴 red | Replaces placeholders back with values |
| **Close** | 🔴 red | Closes the window |

### Helper functions

- **Placeholder** — copies `[REDACTED_ID]` to the clipboard for manual pasting
- **Folder** — opens the vault directory in Explorer
- **.gitignore** — adds the vault path to the nearest `.gitignore`
- **Import** — loads tokens from a file (JSON or `KEY=VALUE`)

---

## 11. Cleaning Tool

Open it via the **Clean files** button or Ctrl+Shift+C.

### What can be cleaned

| Step | What it deletes | Default |
|---|---|---|
| 1 — Gateway logs | Old log files (not today's) | ✅ on |
| 2 — Config backups | `.bak` files (keeps the 2 newest) | ✅ on |
| 3 — Stability logs | Logs older than 3 days | ❌ off |
| 4 — Browser cache | Cache older than 1 day | ✅ on |
| 5 — Session locks | Session lock files | ✅ on |
| 6 — sessions.json | Old session data (keeps the N newest) | ✅ on |
| 7 — Vault backups | `*.bak` in the Token Manager folder | ❌ off (opt-in) |

Use the slider at step 6 to set how many sessions to keep (default: 10). Step 7 is intentionally off — vault backups are your safety net if a restore fails.

### Recommended procedure

1. Click **Preview** (blue button) — shows what would be deleted, does nothing
2. Review the output
3. Click **Run** (green button) — actual deletion

> ⚠️ **Warning:** Run permanently deletes the selected files. The action cannot be undone.

---

## 12. Settings

Open via the **Settings → Open Settings…** menu or Ctrl+,.

### Language

Switch between **Čeština** and **English**. The change takes effect after saving.

### Paths

| Field | Description |
|---|---|
| OpenClaw folder | Where OpenClaw stores its configuration (`~\.openclaw`) |
| Temp folder | Where Gateway logs are stored |
| openclaw command | The command or path to the `openclaw` executable |
| PowerShell working directory | The directory where PowerShell opens |
| Vault | Path to `secrets.json` — encrypted with Windows DPAPI |

The **Browse…** buttons open a folder picker. The "openclaw command" field validates forbidden characters — a value with forbidden shell characters (`<`, `>`, `%`, `^`, `&`, `|`) cannot be saved.

### Theme

Switch between the seven themes (Legacy, Modern, Standard Dark, Modern Dark, Modern Light, High Contrast, Crab Cute). The change takes effect immediately — see [3. Themes and splash screen](#3-themes-and-splash-screen).

### Buttons

- **Save** — saves and closes
- **Reset to defaults** — restores default values (requires confirmation)
- **Cancel** — closes without saving

Settings are stored in `%APPDATA%\OpenClawManager\settings.json`.

---

## 13. About

Open via the **Help → About…** menu or F1. Shows the logo, version, technical stack, and a list of keyboard shortcuts.

---

## 14. Troubleshooting

### The Gateway will not start

**Symptom:** The status bar stays on "starting…" for more than 3 minutes.

**Solution:**
1. Open the **Gateway log** and check the last lines
2. Check in Settings whether the "openclaw command" is correct
3. Run **Repair configuration**
4. Try restarting the Gateway

### The TUI does not appear (blank black area on the right)

**Solution:**
1. Wait 2–3 seconds — the TUI is initializing
2. Click into the terminal area on the right
3. If still blank — stop the TUI and start it again

### Latency values do not appear (dashes)

**Cause:** Latency is measured only after the first request sent in the TUI.

**Solution:** Send a message in the TUI and wait for the response.

### Token Manager reports "vault not found"

**Solution:** Click **Initialize vault**. If you signed in under a different Windows account, the vault cannot be decrypted — restore it from an `.ocvault` backup (Restore) or create a new one and add the tokens again.

### `.ocvault` restore reports a wrong password

**Cause:** The password does not match the one the backup was created with.

**Solution:** Check the password. An `.ocvault` cannot be restored without the correct password — there is no back door.

### Token Manager shows a risky-location warning

**Solution:** Open Settings → change the vault path → save.

### The app reports "Folder does not exist"

**Solution:** Open Settings (Ctrl+,) and fix the path with the **Browse…** button.

### The Gateway log is empty or not found

**Cause:** The Gateway was not started through this app.

**Solution:** Start the Gateway via the **Start** button or the main TUI button.

### The Gateway is not running after a PC restart

**Cause:** The "OpenClaw Gateway" Scheduled Task is disabled or was never created.

**Solution:** Open the Cleaning Tool → "OpenClaw Gateway" section → Enable Scheduled Task.

---

**End of document — app version v2.0**
