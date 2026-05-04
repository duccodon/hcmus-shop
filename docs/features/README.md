# Features Documentation

Each markdown file in this folder is a **handover document** for one feature owned by Dev A. Other developers should be able to read these and understand:

- What the feature does (user-facing behavior)
- How it works (data flow, business rules)
- Where the code lives (every file path)
- How to test/verify it
- Edge cases handled / not handled
- How to extend it

## Index (Dev A features)

| # | Feature | Doc | Status |
|---|---------|-----|--------|
| 01 | Login + ConfigPage + Auto-login | [01-login-config.md](01-login-config.md) | ✅ Implemented |
| 02 | Dashboard | [02-dashboard.md](02-dashboard.md) | ✅ Implemented |
| 03 | Settings | [03-settings.md](03-settings.md) | ✅ Implemented |
| 04 | MSIX Installer | [04-installer.md](04-installer.md) | ⚠️ Manifest configured; build via VS |
| 05 | Role-based access | [05-role-based-access.md](05-role-based-access.md) | ✅ Backend filter + UI helper |
| 06 | Backup/Restore DB | [06-backup-restore.md](06-backup-restore.md) | ✅ Implemented |
| 07 | Obfuscator | [07-obfuscator.md](07-obfuscator.md) | ✅ Configured |
| 08 | Trial mode | [08-trial-mode.md](08-trial-mode.md) | ✅ Implemented |
| 09 | Onboarding | [09-onboarding.md](09-onboarding.md) | ✅ Implemented |
| 10 | Responsive layout pattern | [10-responsive-layout.md](10-responsive-layout.md) | ✅ Pattern documented + applied |
| 11 | Image upload endpoint | [11-image-upload.md](11-image-upload.md) | ✅ Implemented |
| 12 | Data seeding | [12-data-seeding.md](12-data-seeding.md) | ✅ Implemented |

## How to read a feature doc

Each feature doc has the same sections:

1. **Summary** — one paragraph: what and why
2. **User-visible behavior** — what the end user sees
3. **Architecture** — diagram + components
4. **Files** — every file involved with a one-line description
5. **Data flow** — step-by-step request/response
6. **Business rules** — what's enforced and why
7. **Edge cases** — what's handled, what's not
8. **Verification** — how to manually test
9. **Extension points** — how to add to it
