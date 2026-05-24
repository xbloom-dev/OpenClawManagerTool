# STATUS.md

## Current State

- **Version:** v2.0.0-RC1
- **Updated:** 2026-05-25
- **Base branch:** develop
- **Status:** Release Candidate preparation

## V2.0 Roadmap

| PR | Description | Status |
|----|-------------|--------|
| #12 | Remove CleanupServiceAdapter | Merged |
| #13 | Expand ITokenService and route TokenManagerWindow through DI | Merged |
| #14 | Unblock UI thread status polling | Merged |
| #15 | Add IAppEnvironment path abstraction | Merged |
| #16 | Expand TokenService tests | Merged |
| #17 | GatewayLogWindow MVVM | Merged |
| #18 | LiveLogWindow MVVM | Merged |
| #19 | TokenManagerWindow ViewModel | Merged |
| #20 | MainViewModel for MainWindow status logic | Merged |
| #21 | Theme system cleanup pilot | Merged |
| #22 | Release Candidate prep | This PR |

## Remaining Release Actions

- Manual smoke test for all 7 themes after PR #21.
- Build and attach the v2.0.0 release ZIP artifact.
- Merge `develop` into `master` for the final release.
- Create GitHub Release `v2.0.0`.
- Re-check branch protection and required status checks.

## Post-v2.0 Candidates

- ScheduledTaskService DI, if it becomes stateful or needs tests.
- Optional deeper XAML theme migration (`PR #21b`) after a full visual test plan.
- Additional ViewModel unit tests for MainViewModel and TokenManagerViewModel.
