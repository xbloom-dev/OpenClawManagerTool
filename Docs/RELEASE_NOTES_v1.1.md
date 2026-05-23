# OpenClaw Manager Tool v1.1 Release Notes

Release date: 2026-05-23

## Highlights

- Added password-protected Token Vault backup and restore through portable `.ocvault` files.
- Added PBKDF2-SHA256 key derivation and AES-256-GCM encryption for vault backups.
- Improved command validation for OpenClaw Gateway startup commands.
- Finalized the About window Easter Egg admin tools prompt and workspace sync menu.
- Polished Modern Dark, Standard Dark, and secondary window theme behavior.
- Updated GitHub Actions CI to Node 24 compatible actions.

## Security

- Git history was scanned with Gitleaks using `.gitleaks.toml` and redacted output.
- The latest scan found no leaks.
- A known false positive in the bundled minified xterm.js file is narrowly allowlisted.

## Verification

- Local Release/Debug builds pass.
- TokenService tests pass.
- GitHub Actions CI passes on the active `codex/phase1-stability-security-performance` branch.
- Manual UI smoke test is marked OK by Bloom for Dark, StandardDark, and ModernLight coverage.

## Remaining Public Release Steps

- Switch the GitHub repository visibility to public when ready.
- Enable branch protection for `master` and `develop`.
- Run the Gitleaks scan again on the final merged target branch.
- Create the GitHub Release `v1.1` and upload the prepared runtime ZIP.
