# Contributing

Thanks for your interest in improving OpenClaw Manager Tool.

This project is a Windows WPF utility for managing and diagnosing an existing
OpenClaw installation. Contributions are welcome, especially fixes that improve
stability, security, accessibility, localization, and documentation.

## Before You Start

- Check existing issues before opening a new one.
- Keep changes focused and easy to review.
- Do not include API keys, access tokens, logs with secrets, or private paths.
- Report security issues privately. See [SECURITY.md](SECURITY.md).

## Development Setup

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK
- WebView2 Runtime

Clone and build:

```powershell
git clone https://github.com/xbloom-dev/OpenClawManagerTool.git
cd OpenClawManagerTool
dotnet build
dotnet run --project TokenService.Tests\TokenService.Tests.csproj
```

## Pull Requests

Before opening a pull request:

- Run `dotnet build`.
- Run `dotnet run --project TokenService.Tests\TokenService.Tests.csproj`.
- Keep UI text in resource files instead of hardcoding strings.
- Keep public documentation in English unless the file is explicitly localized.
- Update documentation when behavior changes.

## Code Style

- Prefer small, direct changes that match the existing codebase.
- Avoid broad refactors unless they are necessary for the fix.
- Treat token handling, process launching, cleanup paths, and command validation
  as security-sensitive code.

## Project Scope

OpenClaw Manager Tool manages the local Windows experience around OpenClaw. Bugs
or feature requests for OpenClaw itself should be reported to the
[OpenClaw project](https://github.com/openclaw/openclaw).
