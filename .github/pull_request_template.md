<!-- Thanks for contributing! Please fill in the sections below. -->

## Summary

<!-- What does this PR do? Keep it focused on a single change. -->

## Related issue

<!-- Link the issue this addresses, e.g. "Closes #123". -->

## Type of change

- [ ] Bug fix
- [ ] New feature
- [ ] Refactor / cleanup (no behavior change)
- [ ] Documentation
- [ ] Build / CI

## How was this tested?

<!-- Describe your testing. Manual GUI testing matters here. -->

- [ ] `dotnet build` passes with no errors
- [ ] `dotnet run --project TokenService.Tests\TokenService.Tests.csproj` passes
- [ ] Manual smoke test of affected windows

## Sensitive areas

<!-- Tick if your change touches any of these, and describe the manual testing you did. -->

- [ ] Splash screen / WebView2 startup sequence
- [ ] Theme system
- [ ] Token vault or `.ocvault` format
- [ ] None of the above

## Checklist

- [ ] Single, focused change
- [ ] No hardcoded user-facing strings (localized via resource files)
- [ ] No secrets, tokens, or vault contents in code or logs
- [ ] Documentation updated (README / CHANGELOG) if behavior changed
- [ ] Targets the `develop` branch
