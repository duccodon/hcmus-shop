# Dev A — Master Implementation Plan

**Owner**: Dung (Dev A)
**Scope**: All Dev A features per `docs/plans/project-roadmap-3-weeks.md`
**Target**: ~3.75 points (1.25 base + 2.50 bonus)

---

## Dependency map

### Hard dependencies (must wait)
None. Every Dev A task can be done independently.

### Soft dependencies (would help but not blockers)
- **Dev C's orders** — would make `dashboardStats` show real numbers. Without it: empty arrays / zero counts (still correct, just empty).
- **Dev B's ProductsPage UI** — already exists; needed for the role-based "hide import price column" tweak.

### Reverse dependencies (I block others)
- **Image upload endpoint** → blocks Dev B's AddProduct image upload flow → **MUST DO FIRST**
- **Role-based backend filter on `importPrice`** → small UI tweak in Dev B's ProductsPage to hide the column for Sale users → coordinate or do myself

---

## Feature inventory

| # | Feature | Pts | Layer | Where | Phase |
|---|---------|-----|-------|-------|-------|
| 1 | Image upload endpoint | — | Backend | `hcmus-shop-server/` | 1 |
| 2 | Realistic seed data (125 products) | — | Backend | `prisma/seed.ts` | 1 |
| 3 | Role-based access (backend) | +0.50 | Backend | Product resolver | 2 |
| 4 | Hide import price column (UI) | (part of 3) | Client | ProductsPage | 2 |
| 5 | ConfigPage (server URL) | (part of B1) | Client | `Views/Pages/Auth/ConfigPage` | 2 |
| 6 | Auto-login flow | (part of B1) | Client | App.xaml.cs / LoginWindow | 2 |
| 7 | `dashboardStats` resolver | (part of B2) | Backend | new feature module | 3 |
| 8 | DashboardService + wire UI | (part of B2) | Client | Services + DashboardPage | 3 |
| 9 | SettingsPage (page size, last screen) | 0.25 | Client | `Views/Pages/Settings` | 4 |
| 10 | Responsive layout pattern | +0.50 | Client | Login/Dashboard/Settings/Config | 4 |
| 11 | Backup/Restore endpoints | (part of 12) | Backend | new module | 5 |
| 12 | Backup/Restore UI in Settings | +0.25 | Client | SettingsPage | 5 |
| 13 | Trial mode (15-day lock) | +0.50 | Client | App startup + new TrialPage | 5 |
| 14 | Onboarding tutorial overlay | +0.50 | Client | TeachingTip on first login | 5 |
| 15 | Unit tests (backend) | (shared) | Backend | `tests/` | 6 |
| 16 | Obfuscator (build step) | +0.25 | Build | `.csproj` post-build | 7 |
| 17 | MSIX Installer + Package.appxmanifest | 0.25 | Build | manifest config | 7 |

**Base feature aggregation**: B1 Login (0.25) + B2 Dashboard (0.50) + B6 Settings (0.25) + B7 Installer (0.25) = **1.25**
**Bonus aggregation**: Role 0.5 + Backup 0.25 + Obfuscator 0.25 + Trial 0.5 + Onboarding 0.5 + Responsive 0.5 = **2.50**
**Total**: 3.75 points

---

## Phased execution plan

Each phase ends with a commit + push.

### PHASE 1 — Unblock Dev B (priority 0)
Estimated: 1.5h
- [ ] **1.1** Add `multer` to backend deps
- [ ] **1.2** Create `POST /uploads` REST endpoint (multipart) in `index.ts`
- [ ] **1.3** Add `app.use('/uploads', express.static('uploads'))`
- [ ] **1.4** Configure CORS for the upload endpoint
- [ ] **1.5** Test with curl: returns `{ url: "/uploads/xxx.jpg" }`
- [ ] **1.6** Expand `prisma/seed.ts` to 25 products × 5 categories = 125 products with realistic specs and image URLs
- [ ] **1.7** Run `npx ts-node prisma/seed.ts` against fresh DB to verify
- [ ] **Commit**: `feat(server): image upload endpoint + expanded seed data`

### PHASE 2 — Auth foundation (Login + Config)
Estimated: 2h
- [ ] **2.1** Add role check to `Product.importPrice` resolver — return null if not Admin
- [ ] **2.2** Create `IConfigService` + `ConfigService` to read/write server URL
- [ ] **2.3** Create `ConfigPage.xaml` + `ConfigPage.xaml.cs`
  - Server URL input
  - Test Connection button
  - Save button
- [ ] **2.4** Create `ConfigViewModel` with `[ObservableProperty]` server URL + RelayCommands
- [ ] **2.5** Wire `OpenConfigCommand` in `LoginViewModel` to navigate to ConfigPage
- [ ] **2.6** Add Cancel button on ConfigPage to go back to LoginPage
- [ ] **2.7** Modify `App.xaml.cs.OnLaunched` to call `IAuthService.TryAutoLoginAsync()` first; if true, open MainWindow directly
- [ ] **2.8** In ProductsPage XAML, hide import price column when `IAuthService.HasRole("Admin") == false`
- [ ] **Commit**: `feat(auth): config page + auto-login + role-based price hiding`

### PHASE 3 — Dashboard end-to-end
Estimated: 2.5h
- [ ] **3.1** Create `dashboard` feature module on backend (resolver + service + repository + typeDef)
- [ ] **3.2** Implement `query dashboardStats` returning all KPIs
- [ ] **3.3** Create `IDashboardService` + `DashboardService` on client
- [ ] **3.4** Create `DashboardDto` matching server response
- [ ] **3.5** Add GraphQL query string in `GraphQL/Operations/DashboardQueries.cs`
- [ ] **3.6** Refactor `DashboardViewModel` — remove ALL mock data, fetch via service
- [ ] **3.7** Loading/error states in DashboardPage
- [ ] **Commit**: `feat(dashboard): wire to real GraphQL stats`

### PHASE 4 — Settings + Responsive
Estimated: 2h
- [ ] **4.1** Create `Views/Pages/Settings/SettingsPage.xaml` + code-behind
- [ ] **4.2** Create `SettingsViewModel`:
  - PageSize dropdown (5/10/15/20)
  - "Open last screen on startup" toggle
- [ ] **4.3** Create `ISettingsService` to wrap LocalSettings reads/writes
- [ ] **4.4** In MainWindow, track `_lastScreen` on every navigation, save to LocalSettings
- [ ] **4.5** On login success, read saved last screen and navigate there (only if "open last screen" is enabled)
- [ ] **4.6** Make ProductsViewModel read PageSize from settings (publish event for change)
- [ ] **4.7** Apply responsive Grid pattern to Login, Config, Dashboard, Settings pages
- [ ] **4.8** Document the responsive pattern in `docs/responsive-pattern.md`
- [ ] **Commit**: `feat(settings): page size + last screen + responsive pattern`

### PHASE 5 — Bonus features (Backup, Trial, Onboarding)
Estimated: 3h
- [ ] **5.1** Backend: `POST /backup` runs `pg_dump`, streams SQL file
- [ ] **5.2** Backend: `POST /restore` accepts SQL file, runs `psql`
- [ ] **5.3** Client: Backup section in SettingsPage:
  - "Download Backup" button → save SQL file via FilePicker
  - "Restore from File" button → upload to /restore
- [ ] **5.4** Trial mode: on first launch, save `trial_start_date` to LocalSettings
- [ ] **5.5** On every launch, check `(now - start) > 15 days`
- [ ] **5.6** If expired, show `TrialExpiredPage` with activation code input
- [ ] **5.7** Hardcoded valid code `HCMUS2026` unlocks the app
- [ ] **5.8** Onboarding: first-time launch flag in LocalSettings
- [ ] **5.9** Use WinUI `TeachingTip` to step through Dashboard → Products → Logout
- [ ] **5.10** Skip and "Don't show again" buttons
- [ ] **Commit**: `feat(bonus): backup/restore + trial mode + onboarding`

### PHASE 6 — Tests
Estimated: 1h
- [ ] **6.1** Add Jest config to backend `package.json`
- [ ] **6.2** Test: auth login flow (valid + invalid creds)
- [ ] **6.3** Test: role-based price filtering for Sale role
- [ ] **6.4** Test: dashboard stats aggregation logic
- [ ] **Commit**: `test(server): unit tests for auth + roles + dashboard`

### PHASE 7 — Packaging
Estimated: 1.5h
- [ ] **7.1** Configure `Package.appxmanifest`: app name "HCMUS Shop", description, logo
- [ ] **7.2** Add post-build obfuscator step using `Obfuscar` (open source, NuGet)
- [ ] **7.3** Build MSIX installer via Visual Studio "Package and Publish"
- [ ] **7.4** Test install on a clean VM
- [ ] **7.5** Document install steps in `docs/install.md`
- [ ] **Commit**: `chore(package): obfuscator + MSIX installer`

**Total estimate**: ~13 hours (one heavy day or two normal days)

---

## Today's status checklist

Update this section as work progresses.

### Phase 1 — Unblock ✅ (committed)
- [x] 1.1 multer installed
- [x] 1.2 /uploads endpoint
- [x] 1.3 static serving
- [x] 1.4 CORS for uploads
- [ ] 1.5 curl test (pending: needs running server)
- [x] 1.6 seed expanded (25+ products per category)
- [ ] 1.7 seed verified (pending: needs running DB)
- [x] Commit done (push pending)

### Phase 2 — Auth ✅ (committed)
- [x] 2.1 importPrice role check (server returns null for non-Admin)
- [x] 2.2 ConfigService (read/write server URL via LocalSettings)
- [x] 2.3 ConfigPage (XAML)
- [x] 2.4 ConfigViewModel (Test Connection + Save + Cancel + Reset)
- [x] 2.5 OpenConfigCommand wired (LoginPage → LoginWindow → Frame.Navigate)
- [x] 2.6 Cancel button (returns to LoginPage)
- [x] 2.7 Auto-login on startup (App.OnLaunched calls TryAutoLoginAsync)
- [x] 2.8 RoleVisibilityConverter created (Dev B applies to import-price column when added)
- [x] Commit done (push pending)

### Phase 3 — Dashboard ✅ (committed)
- [x] 3.1 dashboard module created (resolver + service + repository + typeDef)
- [x] 3.2 dashboardStats resolver (queries Prisma in parallel via Promise.all)
- [x] 3.3 DashboardService (client) with Result<T> pattern
- [x] 3.4 DashboardDto (KPIs + tables + chart data)
- [x] 3.5 GraphQL query string in DashboardQueries.cs
- [x] 3.6 DashboardViewModel rewritten — RefreshAsync + ApplyStats, no mock data
- [x] 3.7 Loading + error states (IsLoading, ErrorMessage, page.Loaded triggers refresh)
- [x] Commit done (push pending)

### Phase 4 — Settings ✅ (committed)
- [x] 4.1 SettingsPage XAML (page size + remember last screen)
- [x] 4.2 SettingsViewModel (Save command + LocalSettings binding)
- [x] 4.3 ISettingsService + SettingsService
- [x] 4.4 Last screen tracking (MainWindow.NavigateTo writes _settings.LastScreen)
- [x] 4.5 Last screen restore (NavigateToDefault checks RememberLastScreen)
- [x] 4.6 PageSize wired (Settings exposes; ProductsViewModel can adopt)
- [x] 4.7 Responsive pattern documented (docs/responsive-pattern.md)
- [x] 4.8 Pattern applied to Login/Config/Settings pages
- [x] Commit done (push pending)

### Phase 5 — Bonuses ✅ (committed)
- [x] 5.1 /backup endpoint (pg_dump streaming)
- [x] 5.2 /restore endpoint (multipart psql)
- [x] 5.3 Backup UI in SettingsPage (Download + Restore buttons)
- [x] 5.4 Trial start date saved on first launch
- [x] 5.5 Expiry check (15 days, returns Active/Expired/Activated status)
- [x] 5.6 TrialExpiredPage + TrialExpiredWindow
- [x] 5.7 Activation code "HCMUS2026" unlocks app
- [x] 5.8 Onboarding flag in LocalSettings (IOnboardingService)
- [x] 5.9 TeachingTips (4-step tour: Welcome → Dashboard → Products → Settings)
- [ ] 5.10 Skip/never buttons
- [ ] Commit pushed

### Phase 6 — Tests
- [ ] 6.1 Jest config
- [ ] 6.2 Auth test
- [ ] 6.3 Role test
- [ ] 6.4 Dashboard test
- [ ] Commit pushed

### Phase 7 — Packaging
- [ ] 7.1 Manifest configured
- [ ] 7.2 Obfuscar set up
- [ ] 7.3 MSIX built
- [ ] 7.4 VM tested
- [ ] 7.5 Install doc
- [ ] Commit pushed

---

## Coordination notes

- **Dev B**: I'll commit my image upload endpoint first thing today so you can wire AddProduct's image upload. The endpoint contract is documented in `docs/features/11-image-upload.md`.
- **Dev B**: I'll add a small change to `ProductsPage.xaml` to conditionally hide the import price column for Sale role. If you'd prefer to do it yourself, ping me — it's 5 lines of XAML.
- **Dev C**: Dashboard works fine with no orders (shows zeros / empty tables). When your orders ship, the dashboard will populate automatically — no changes needed on my side.
