# Implementation Map

Quick reference: every Dev A feature → exact file paths.

If a future maintainer asks "where does X live", find it here.

---

## Auth & Config

| Concern | File |
|---------|------|
| App startup, DI registration | `hcmus-shop/App.xaml.cs` |
| App-wide XAML resources (converters) | `hcmus-shop/App.xaml` |
| Login/Config pre-login window | `hcmus-shop/Views/Pages/Auth/LoginWindow.cs` |
| Login form layout | `hcmus-shop/Views/Pages/Auth/LoginPage.xaml` + `.xaml.cs` |
| Login state + LoginCommand | `hcmus-shop/ViewModels/Auth/LoginViewModel.cs` |
| Config form layout | `hcmus-shop/Views/Pages/Auth/ConfigPage.xaml` + `.xaml.cs` |
| Config state + Save command | `hcmus-shop/ViewModels/Auth/ConfigViewModel.cs` |
| Server URL persistence | `hcmus-shop/Services/Config/ConfigService.cs` |
| Auth interface (CurrentUser, LoginAsync, TryAutoLoginAsync) | `hcmus-shop/Contracts/Services/IAuthService.cs` |
| Auth implementation (calls GraphQL, stores JWT) | `hcmus-shop/Services/Auth/AuthService.cs` |
| GraphQL login + me query strings | `hcmus-shop/GraphQL/Operations/AuthQueries.cs` |

**Backend:**

| Concern | File |
|---------|------|
| login mutation + me query | `hcmus-shop-server/src/features/auth/auth.resolver.ts` |
| BCrypt + JWT logic | `hcmus-shop-server/src/features/auth/auth.service.ts` |
| Auth schema | `hcmus-shop-server/src/features/auth/auth.typeDef.graphql` |
| JWT generate/verify | `hcmus-shop-server/src/common/jwt.ts` |
| Context builder (decodes JWT, sets context.user) | `hcmus-shop-server/src/common/context.ts` |
| Plugin: auto-protect mutations | `hcmus-shop-server/src/common/authPlugin.ts` |

---

## Dashboard

| Concern | File |
|---------|------|
| Page layout | `hcmus-shop/Views/Pages/Dashboard/DashboardPage.xaml` |
| Page code-behind (DI + Refresh on Loaded) | `hcmus-shop/Views/Pages/Dashboard/DashboardPage.xaml.cs` |
| ViewModel (KpiCards, charts, refresh) | `hcmus-shop/ViewModels/Dashboard/DashboardViewModel.cs` |
| Service interface | `hcmus-shop/Contracts/Services/IDashboardService.cs` |
| Service implementation | `hcmus-shop/Services/Dashboard/DashboardService.cs` |
| Server response shape | `hcmus-shop/Models/DTOs/DashboardDto.cs` |
| GraphQL query string | `hcmus-shop/GraphQL/Operations/DashboardQueries.cs` |

**Backend:**

| Concern | File |
|---------|------|
| dashboardStats query | `hcmus-shop-server/src/features/dashboard/dashboard.resolver.ts` |
| Parallel orchestration | `hcmus-shop-server/src/features/dashboard/dashboard.service.ts` |
| 7 Prisma queries | `hcmus-shop-server/src/features/dashboard/dashboard.repository.ts` |
| Schema | `hcmus-shop-server/src/features/dashboard/dashboard.typeDef.graphql` |

---

## Settings (page size + last screen + backup UI)

| Concern | File |
|---------|------|
| Page layout | `hcmus-shop/Views/Pages/Settings/SettingsPage.xaml` |
| Page code-behind (sets WindowHandle for FilePicker) | `hcmus-shop/Views/Pages/Settings/SettingsPage.xaml.cs` |
| ViewModel | `hcmus-shop/ViewModels/Settings/SettingsViewModel.cs` |
| Settings interface | `hcmus-shop/Contracts/Services/ISettingsService.cs` |
| Settings persistence (LocalSettings) | `hcmus-shop/Services/Settings/SettingsService.cs` |
| Last-screen tracking | `hcmus-shop/MainWindow.xaml.cs` (line `_settings.LastScreen = target;` in `NavigateTo`) |
| Last-screen restore | `hcmus-shop/MainWindow.xaml.cs` (top of `NavigateToDefault`) |

---

## Trial mode (15-day lock)

| Concern | File |
|---------|------|
| Interface | `hcmus-shop/Contracts/Services/ITrialService.cs` |
| Implementation | `hcmus-shop/Services/Trial/TrialService.cs` |
| Trial expired window | `hcmus-shop/Views/Pages/Auth/TrialExpiredWindow.cs` |
| Trial expired page | `hcmus-shop/Views/Pages/Auth/TrialExpiredPage.xaml` + `.xaml.cs` |
| Activation form ViewModel | `hcmus-shop/ViewModels/Auth/TrialExpiredViewModel.cs` |
| Trial check on launch | `hcmus-shop/App.xaml.cs` (`OnLaunched` first thing) |
| Activation handler | `hcmus-shop/App.xaml.cs` (`RelaunchAfterTrialActivation`) |

---

## Onboarding tutorial

| Concern | File |
|---------|------|
| Interface | `hcmus-shop/Contracts/Services/IOnboardingService.cs` |
| Implementation | `hcmus-shop/Services/Onboarding/OnboardingService.cs` |
| TeachingTip controls | `hcmus-shop/MainWindow.xaml` (4 tips at bottom) |
| Tip event handlers | `hcmus-shop/MainWindow.xaml.cs` (`OnTipNext`, `OnTipFinish`, `OnTipSkip`) |
| Trigger | `hcmus-shop/MainWindow.xaml.cs` (`StartOnboardingIfFirstTime`) |

---

## Backup / Restore

| Concern | File |
|---------|------|
| Client interface | `hcmus-shop/Contracts/Services/IBackupService.cs` |
| Client implementation (HttpClient + multipart) | `hcmus-shop/Services/Backup/BackupService.cs` |
| Backup UI | `hcmus-shop/Views/Pages/Settings/SettingsPage.xaml` (Backup section) |
| Download/Restore commands | `hcmus-shop/ViewModels/Settings/SettingsViewModel.cs` |
| Server endpoint /backup | `hcmus-shop-server/src/features/backup/backup.routes.ts` |
| Server endpoint /restore | same file |

---

## Image upload

| Concern | File |
|---------|------|
| REST endpoint | `hcmus-shop-server/src/features/upload/upload.routes.ts` |
| Static serving | `hcmus-shop-server/src/index.ts` (`app.use("/uploads", express.static("uploads"))`) |

Client integration TBD by Dev B in `AddProductPage` flow.

---

## Role-based access

| Concern | File |
|---------|------|
| Backend filter (Product.importPrice returns null for non-Admin) | `hcmus-shop-server/src/features/product/product.resolver.ts` |
| Schema (importPrice nullable) | `hcmus-shop-server/src/features/product/product.typeDef.graphql` |
| Client-side UI hider | `hcmus-shop/Converters/RoleVisibilityConverter.cs` |
| Registration in App.xaml | `hcmus-shop/App.xaml` (`<converters:RoleVisibilityConverter x:Key="RoleVisibility" />`) |

---

## Data seeding

| Concern | File |
|---------|------|
| Seed script | `hcmus-shop-server/prisma/seed.ts` |
| Run | `npx ts-node prisma/seed.ts` (from `hcmus-shop-server/`) |

---

## Tests

| Concern | File |
|---------|------|
| Jest config | `hcmus-shop-server/jest.config.js` |
| Test-only tsconfig | `hcmus-shop-server/tsconfig.test.json` |
| JWT tests | `hcmus-shop-server/tests/jwt.test.ts` |
| Role-filter tests | `hcmus-shop-server/tests/role-filter.test.ts` |
| Auth plugin tests | `hcmus-shop-server/tests/auth-plugin.test.ts` |
| Run | `npm test` (from `hcmus-shop-server/`) |

---

## Packaging

| Concern | File |
|---------|------|
| App identity, logos, capabilities | `hcmus-shop/Package.appxmanifest` |
| Obfuscator config | `hcmus-shop/obfuscar.xml` |
| Build/install instructions | `docs/install.md` |

---

## Reusable converters (added by Dev A)

All in `hcmus-shop/Converters/`:

| File | Purpose |
|------|---------|
| `StringToBooleanConverter.cs` | true if string is non-empty (for InfoBar.IsOpen) |
| `BooleanToInfoSeverityConverter.cs` | true → Error, false → Success (for InfoBar.Severity) |
| `InverseBooleanConverter.cs` | !bool (for IsEnabled = !IsBusy) |
| `RoleVisibilityConverter.cs` | Visible if user has role specified in ConverterParameter |

Registered in `App.xaml`:
```xml
<converters:StringToBooleanConverter x:Key="StringToBooleanConverter" />
<converters:BooleanToInfoSeverityConverter x:Key="BooleanToInfoSeverityConverter" />
<converters:InverseBooleanConverter x:Key="InverseBooleanConverter" />
<converters:RoleVisibilityConverter x:Key="RoleVisibility" />
```

---

## DI registrations (App.xaml.cs)

All services and ViewModels added by Dev A:

```csharp
// Services (Singleton — one instance, shared)
services.AddSingleton<IConfigService, ConfigService>();
services.AddSingleton<IDashboardService, DashboardService>();
services.AddSingleton<ISettingsService, SettingsService>();
services.AddSingleton<ITrialService, TrialService>();
services.AddSingleton<IOnboardingService, OnboardingService>();
services.AddSingleton<IBackupService, BackupService>();

// ViewModels (Transient — new instance each request)
services.AddTransient<ConfigViewModel>();
services.AddTransient<DashboardViewModel>();
services.AddTransient<SettingsViewModel>();
services.AddTransient<TrialExpiredViewModel>();
```
