# Dev A — Handover Document

**Last updated**: end of feat/dev-a-week1 work session
**Branch**: `feat/dev-a-week1`
**Owner**: Dung (Dev A)
**Status**: All 7 phases complete; client builds without errors

If you're a future maintainer (or future-me starting a fresh Claude session), **read this doc first**. It points to everything else.

---

## What's done

All Dev A features from the roadmap are implemented and committed on the `feat/dev-a-week1` branch. The client builds without errors. The backend has 12 unit tests passing.

| Feature | Doc | Status |
|---------|-----|--------|
| **B1 Login** | [features/01-login-config.md](features/01-login-config.md) | ✅ Implemented (login + auto-login) |
| **B1 ConfigPage** | [features/01-login-config.md](features/01-login-config.md) | ✅ Implemented |
| **B2 Dashboard** | [features/02-dashboard.md](features/02-dashboard.md) | ✅ Implemented (queries Prisma direct, gracefully handles empty data) |
| **B6 Settings** | [features/03-settings.md](features/03-settings.md) | ✅ Implemented (page size, last screen, backup) |
| **B7 Installer** | [features/04-installer.md](features/04-installer.md) | ⚠️ Manifest configured; actual MSIX must be built in Visual Studio |
| **Role-based access** | [features/05-role-based-access.md](features/05-role-based-access.md) | ✅ Backend filter + RoleVisibilityConverter (UI ready for Dev B's column) |
| **Backup/Restore** | [features/06-backup-restore.md](features/06-backup-restore.md) | ✅ Backend endpoints + client UI |
| **Obfuscator** | [features/07-obfuscator.md](features/07-obfuscator.md) | ✅ obfuscar.xml configured (run after Release build) |
| **Trial mode** | [features/08-trial-mode.md](features/08-trial-mode.md) | ✅ 15-day lock with HCMUS2026 code |
| **Onboarding** | [features/09-onboarding.md](features/09-onboarding.md) | ✅ 4-step TeachingTip tour on first login |
| **Responsive layout** | [features/10-responsive-layout.md](features/10-responsive-layout.md) | ✅ Pattern documented + applied to Dev A's pages |
| **Image upload** | [features/11-image-upload.md](features/11-image-upload.md) | ✅ POST /uploads + static serving (Dev B can wire AddProduct now) |
| **Data seeding** | [features/12-data-seeding.md](features/12-data-seeding.md) | ✅ Procedural generation: 25+ products per category |

### Bonus features awarded by these tasks

| Bonus | Pts |
|-------|-----|
| Role-based access | +0.50 |
| Backup/Restore | +0.25 |
| Obfuscator | +0.25 |
| Trial mode | +0.50 |
| Onboarding | +0.50 |
| Responsive layout | +0.50 |
| **Total bonus** | **+2.50** |

Plus 1.25 base points (B1 + B2 + B6 + B7 portion) = **3.75 points contributed by Dev A**.

---

## How to read the docs

Three folders, three audiences:

- **`docs/plans/`** — for the team (what to build, when)
  - `dev-a-master-plan.md` — Dev A's 7-phase plan with status checkboxes (all checked)
  - `project-roadmap-3-weeks.md` — full team plan
- **`docs/features/`** — for handover (one doc per feature with file paths, business rules, verification steps)
- **`docs/client-guide/`** — for teaching (how the WinUI client works, written for someone new to WinUI)

If you're new to the codebase: read [`docs/client-guide/README.md`](client-guide/README.md) first, then come back here.

---

## File inventory (everything Dev A added or modified)

### Backend (`hcmus-shop-server/`)

**New files:**
```
src/features/dashboard/
  ├── dashboard.typeDef.graphql      ← GraphQL schema for KPIs
  ├── dashboard.repository.ts        ← 7 Prisma queries
  ├── dashboard.service.ts           ← Promise.all wrapper
  └── dashboard.resolver.ts          ← thin Query.dashboardStats

src/features/upload/
  └── upload.routes.ts               ← POST /uploads (multer)

src/features/backup/
  └── backup.routes.ts               ← GET /backup, POST /restore

tests/
  ├── jwt.test.ts                    ← 3 tests
  ├── role-filter.test.ts            ← 4 tests
  └── auth-plugin.test.ts            ← 5 tests

jest.config.js
tsconfig.test.json
```

**Modified files:**
```
src/index.ts                          ← register dashboard, upload, backup routers
src/features/product/product.resolver.ts ← role check on importPrice
src/features/product/product.typeDef.graphql ← importPrice now nullable
prisma/seed.ts                        ← procedural generation 25+ per category
.gitignore                            ← add uploads/
package.json                          ← add multer, uuid, jest, ts-jest deps + test script
```

### Client (`hcmus-shop/`)

**New folders/files:**
```
Contracts/Services/
  ├── IConfigService.cs
  ├── IDashboardService.cs
  ├── ISettingsService.cs
  ├── ITrialService.cs
  ├── IOnboardingService.cs
  └── IBackupService.cs

Services/
  ├── Config/ConfigService.cs
  ├── Dashboard/DashboardService.cs
  ├── Settings/SettingsService.cs
  ├── Trial/TrialService.cs
  ├── Onboarding/OnboardingService.cs
  └── Backup/BackupService.cs

ViewModels/
  ├── Auth/ConfigViewModel.cs
  ├── Auth/TrialExpiredViewModel.cs
  └── Settings/SettingsViewModel.cs

Views/Pages/
  ├── Auth/ConfigPage.xaml + .xaml.cs
  ├── Auth/TrialExpiredPage.xaml + .xaml.cs
  ├── Auth/TrialExpiredWindow.cs
  └── Settings/SettingsPage.xaml + .xaml.cs

Models/DTOs/DashboardDto.cs

GraphQL/Operations/DashboardQueries.cs

Converters/
  ├── StringToBooleanConverter.cs
  ├── BooleanToInfoSeverityConverter.cs
  ├── InverseBooleanConverter.cs
  └── RoleVisibilityConverter.cs

obfuscar.xml
```

**Modified files:**
```
App.xaml                              ← register 4 new converters
App.xaml.cs                           ← register all new services + ViewModels in DI;
                                          add trial check + auto-login to OnLaunched;
                                          add RelaunchAfterTrialActivation
MainWindow.xaml                       ← Settings nav item; 4 TeachingTips
MainWindow.xaml.cs                    ← onboarding triggers, TeachingTip handlers,
                                          last-screen tracking, Settings always-allowed
ViewModels/Auth/LoginViewModel.cs    ← OpenConfigRequested event
Views/Pages/Auth/LoginWindow.cs      ← Frame-based navigation between LoginPage/ConfigPage
Views/Pages/Auth/LoginPage.xaml.cs   ← ConfigRequested event bubble
Views/Pages/Dashboard/DashboardPage.xaml.cs ← resolves VM from DI, refreshes on Loaded
Views/Pages/Admin/DashboardPage.xaml ← removed broken parameterless VM, now placeholder
ViewModels/Dashboard/DashboardViewModel.cs ← rewritten — RefreshAsync + ApplyStats, no mock data
Package.appxmanifest                  ← proper DisplayName + Description
hcmus-shop.csproj                     ← add Settings/**/*.xaml include
```

### Documentation (`docs/`)

```
HANDOVER.md (this file)
plans/dev-a-master-plan.md
features/README.md + 01-12.md
client-guide/README.md + 01-09.md
install.md
responsive-pattern.md
```

---

## Build status

- **Backend**: `npx tsc --noEmit` → OK; `npm test` → 12/12 passing
- **Client**: builds in Visual Studio without errors (verified)
- **Database**: 125+ products seeded, 5 categories ≥25 each (run `npx ts-node prisma/seed.ts`)

---

## Coordination handoffs

### To Dev B (Buu) — Products & Promotions
1. **Image upload endpoint is live**. POST a multipart `file` field to `http://localhost:4000/uploads`, get back `{ url: "/uploads/products/<uuid>.jpg" }`. Wire this into `AddProductPage` to upload images before calling `createProduct`.
2. **Role-based access is enforced server-side**. `Product.importPrice` is `null` for non-Admin. When you add the import-price column to ProductsPage, hide it for Sale users with:
   ```xml
   Visibility="{Binding Converter={StaticResource RoleVisibility}, ConverterParameter=Admin}"
   ```
3. **Page size from Settings**: when refactoring ProductsViewModel, read default page size from `ISettingsService.PageSize` instead of hardcoding 10.

### To Dev C (Duc) — Orders & Reports
1. **Dashboard works fine without orders**. When you add the order feature, the Dashboard's KPIs (orders today, revenue today, recent orders, daily revenue) will start populating automatically — no Dashboard changes needed.
2. **For dashboard's `topSellingProducts` to populate**: orders must be in `Paid` status. The dashboard repository sums `OrderItem.quantity` where parent order is Paid.
3. **For dashboard's `dailyRevenue` chart**: same — sum of `Order.finalAmount` where status is Paid, grouped by day.

---

## Known gotchas / things to be aware of

1. **`MainWindow.xaml.cs` line ~100**: `CanAccessFeature("Settings")` returns true for any logged-in user. Settings doesn't go through the FeatureFlagService (which is config-driven for Admin/Sale). If you need admin-only settings, add it to FeatureFlags in `appsettings.json`.

2. **`DashboardViewModel.ApplyStats` line ~134**: invoice legend is currently derived from recent orders (only 3 entries). When real reports come in, replace with a proper status breakdown query.

3. **Trial mode (`TrialService`)** uses `LocalSettings`. If a user clears LocalSettings, the trial resets. For demo this is fine; production would need machine binding.

4. **`SettingsService.PageSize`** is exposed via `ISettingsService` but ProductsViewModel hasn't been refactored yet to read it. Easy fix when Dev B touches ProductsViewModel: inject `ISettingsService`, read `PageSize`.

5. **Backup/Restore** requires the Postgres CLI tools (`pg_dump`, `psql`) to be on the server's `PATH`. The Docker container `postgres:16-alpine` includes them, so for the demo it works.

6. **Obfuscator** is configured but NOT run automatically. To produce an obfuscated build:
   ```
   dotnet build -c Release
   obfuscar.console hcmus-shop/obfuscar.xml
   ```
   Then point the MSIX packager at the `Obfuscated/` folder.

7. **Auto-login** uses the JWT stored in LocalSettings. JWT expires in 7 days (server-side). After 7 days, auto-login fails silently and the user lands on LoginPage.

---

## Next session: where to pick up

If you start a fresh Claude Code session and want to continue Dev A's work:

1. **Read** `docs/HANDOVER.md` (this file)
2. **Skim** `docs/plans/dev-a-master-plan.md` for what was scoped and how
3. **Pull latest**: `git pull origin master` then merge in `feat/dev-a-week1` if not already merged
4. **Verify build**: open in Visual Studio, F5 — should login and see dashboard
5. **Verify tests**: `cd hcmus-shop-server && npm test`

If you want to extend a specific feature:
- Open `docs/features/0X-feature-name.md` for the architectural reference
- Open `docs/client-guide/09-how-to-add-a-feature.md` for the recipe

If you want to keep adding bonus features (we already hit the 5.0 cap with margin):
- The roadmap mentions: Excel import, multi-theme, ML/LLM, plugin architecture (we explicitly skipped these)
- Quick wins: input validation, better error messages, loading skeletons

---

## Demo script (to record on May 3)

A 7-minute video covering Dev A's contributions:

1. **Show the install** — double-click .msix, install (5s)
2. **Open ConfigPage** — change server URL, test connection, save (20s)
3. **Login as admin** — show import price column visible (15s)
4. **First-time onboarding** — Welcome → Dashboard → Products → Settings tips (30s)
5. **Dashboard** — KPI cards, low stock table, revenue chart (20s)
6. **Logout, login as sale** — show import price hidden (15s)
7. **Settings** — change page size, toggle remember-last-screen (15s)
8. **Backup** — download SQL file, open it to show contents (15s)
9. **Restart app** — show auto-login + last screen restore (10s)
10. **(Optional) Trial expiry demo** — set system clock forward 16 days, show TrialExpiredPage, enter HCMUS2026 (30s)

Total: ~3 minutes for Dev A's slice. Dev B and Dev C add their slices for the full demo.

---

## Contact / questions

If something's unclear:
- Architectural questions → `docs/client-guide/`
- "How does X work" → `docs/features/0X-*.md`
- Build / install issues → `docs/install.md`
- Adding new features → `docs/client-guide/09-how-to-add-a-feature.md`

The code itself has inline comments where the logic is non-obvious (especially in `DashboardRepository`, `TrialService`, `BackupService`).
