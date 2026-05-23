# Security Policy

OpenClaw Manager Tool handles API tokens and other secrets, so security reports are taken seriously.

## Supported versions

| Version | Supported |
|---------|-----------|
| 1.1.x   | ✅ |
| < 1.1   | ❌ |

Only the latest released version receives security fixes. Please upgrade before reporting an issue against an older build.

## Reporting a vulnerability

**Please do not open a public GitHub issue for security problems.** Public disclosure before a fix puts users at risk.

Instead, use one of these private channels:

1. **GitHub private vulnerability reporting (preferred).** On this repository, go to the **Security** tab → **Report a vulnerability**. This opens a private advisory visible only to the maintainer.
2. **Email.** `flora-nejsladsi.3y@icloud.com`

Please include:

- A description of the issue and its impact.
- Steps to reproduce, or a proof of concept.
- Affected version(s) and your environment (Windows version, app version).
- Any suggested mitigation, if you have one.

## What to expect

- **Acknowledgement** within a few days of your report.
- An assessment of severity and a fix timeline once the issue is confirmed.
- Credit in the release notes if you would like it (let us know).

Please give a reasonable amount of time for a fix to be prepared and released before any public disclosure.

## Scope

Issues that are especially relevant to this project:

- Anything that could expose, leak, or weaken the encryption of the token vault (DPAPI vault or `.ocvault` backups).
- Command injection via the OpenClaw command path or settings.
- Unsafe handling of files during cleanup or redaction.
- Exposure of secrets in logs.

Out of scope:

- Vulnerabilities in OpenClaw itself — report those to the [OpenClaw project](https://github.com/openclaw/openclaw).
- Vulnerabilities in third-party runtimes (.NET, WebView2) — report those upstream.

## Good practice for users

- Keep the token vault outside project folders, Git repositories, and cloud-synced folders.
- Protect `.ocvault` backups and their passwords; store them separately from the app.
- Keep Windows, the .NET runtime, and the WebView2 runtime up to date.
