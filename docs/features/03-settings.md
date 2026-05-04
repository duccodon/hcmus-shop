# Feature 03 — Settings

**Owner**: Dev A
**Status**: Planned
**Base**: B6 = 0.25 pts
**Phase**: 4

## Summary

Per-user (per-machine) preferences that take effect after login. Distinct from Config (pre-login server connection).

## User-visible behavior

Accessible from sidebar after login. Contains:
- **Page size dropdown**: 5 / 10 / 15 / 20 (affects Products list, Orders list, etc.)
- **"Open last screen on startup" toggle**: when enabled, after login, jumps directly to the screen the user last opened (instead of Dashboard)
- **Backup/Restore** (added in Phase 5 — see [06-backup-restore.md](06-backup-restore.md))

## Architecture

```
Settings stored in:
  Windows.Storage.ApplicationData.Current.LocalSettings

Keys:
  "settings_page_size" → int (default 10)
  "settings_remember_last_screen" → bool (default false)
  "settings_last_screen" → string (e.g. "Products")

Read by:
  - ProductsViewModel (for pageSize default)
  - MainWindow.xaml.cs (on login, navigate to last screen if enabled)

Written by:
  - SettingsViewModel.SaveCommand
  - MainWindow.xaml.cs (every navigation)
```

## Files

### New
| File | Purpose |
|------|---------|
| `Views/Pages/Settings/SettingsPage.xaml` | Page UI |
| `Views/Pages/Settings/SettingsPage.xaml.cs` | Code-behind |
| `ViewModels/Settings/SettingsViewModel.cs` | State + Save command |
| `Contracts/Services/ISettingsService.cs` | Interface |
| `Services/Settings/SettingsService.cs` | Wraps LocalSettings |

### Modified
| File | Change |
|------|--------|
| `MainWindow.xaml` | Add Settings nav item |
| `MainWindow.xaml.cs` | Track last screen on navigation |
| `App.xaml.cs` | Register ISettingsService + SettingsViewModel |
| `ViewModels/Products/ProductsViewModel.cs` | Read default pageSize from settings |

## Business rules

- Settings only apply AFTER successful login
- Page size change is global (affects all paginated lists)
- "Last screen" tracking starts fresh on each login (resets if disabled)
- Default page size: 10
- Default last screen: Dashboard

## Edge cases

| Case | Behavior |
|------|----------|
| Settings file corrupted | Fall back to defaults |
| Last screen no longer exists (feature flag changed) | Fall back to Dashboard |
| Page size changed mid-session | Existing pages keep current size; new visits use new size |

## Verification

1. Open Settings, change page size to 20 → save
2. Navigate to Products → 20 items per page
3. Enable "Open last screen on startup" → save
4. Navigate to Orders → logout → login → lands on Orders (not Dashboard)
5. Disable the toggle → logout → login → lands on Dashboard

## Extension points

- Theme picker (Light / Dark / System)
- Language picker (English / Vietnamese)
- Notification preferences
- Default printer for invoices
