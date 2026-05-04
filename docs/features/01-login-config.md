# Feature 01 — Login + ConfigPage + Auto-login

**Owner**: Dev A
**Status**: Planned (Login already partially done — see existing AuthService)
**Base**: B1 = 0.25 pts
**Phase**: 2

## Summary

Three related sub-features:
1. **Login** — username + password form, JWT-based auth (already implemented)
2. **ConfigPage** — pre-login screen to configure the GraphQL server URL
3. **Auto-login** — on app startup, validate stored token and skip login if valid

## Why ConfigPage is critical

The instructor's requirement: "Cho phép cấu hình thông tin server từ màn hình Config". Our architecture has a GraphQL server in between, so the client only needs the server URL (DB credentials live on the server in `.env`).

This satisfies the client-server architecture proof: the client doesn't have hardcoded connection info, it depends on a configurable server.

## User-visible behavior

### First launch
1. App opens to LoginPage
2. User clicks "Config" (gear icon top-right) → ConfigPage opens
3. User enters server URL (default `http://localhost:4000/graphql`)
4. User clicks "Test Connection" → server is pinged → success/failure message
5. User clicks "Save" → URL persisted to LocalSettings → returns to LoginPage
6. User logs in → MainWindow opens

### Subsequent launches (auto-login enabled)
1. App reads stored JWT
2. Calls `me` query to validate
3. If valid → MainWindow opens directly (no login screen)
4. If expired/invalid → LoginPage shown

### Logout
1. AuthService clears token from LocalSettings
2. App returns to LoginPage

## Architecture

```
App.xaml.cs OnLaunched()
       │
       ▼
   ┌──────────────────────────┐
   │ AuthService.TryAutoLoginAsync()
   │   - Read saved token
   │   - Set on GraphQL client
   │   - Call `me` query
   │   - If success: set CurrentUser
   └──────────────────────────┘
       │
       ▼
   ┌──────────┐         ┌──────────┐
   │  Logged  │   No    │ LoginPage │
   │   in?    │────────►│           │
   └──────────┘         └──────────┘
       │ Yes                 │
       ▼                     │
   ┌──────────────┐          │
   │  MainWindow  │          │
   └──────────────┘          │
                             ▼
                    User clicks Config?
                             │
                             ▼
                    ┌──────────────┐
                    │  ConfigPage  │
                    └──────────────┘
```

## Files

### New
| File | Purpose |
|------|---------|
| `Views/Pages/Auth/ConfigPage.xaml` | Server URL input UI |
| `Views/Pages/Auth/ConfigPage.xaml.cs` | Code-behind |
| `ViewModels/Auth/ConfigViewModel.cs` | URL, TestConnection, Save commands |
| `Contracts/Services/IConfigService.cs` | Read/write server URL |
| `Services/Config/ConfigService.cs` | Implementation using LocalSettings |

### Modified
| File | Change |
|------|--------|
| `App.xaml.cs` | Add `TryAutoLoginAsync` call in `OnLaunched` |
| `ViewModels/Auth/LoginViewModel.cs` | Wire `OpenConfigCommand` properly |
| `Views/Pages/Auth/LoginPage.xaml.cs` | Handle navigation to ConfigPage |

## Data flow

### ConfigPage save flow
1. User types URL → bound to `ConfigViewModel.ServerUrl`
2. User clicks "Test Connection"
3. ViewModel creates a temporary `GraphQLClientService` with that URL
4. Sends `query { __typename }` (cheapest possible query)
5. On success: shows green checkmark
6. User clicks "Save"
7. ViewModel calls `IConfigService.SetServerUrl(url)` → writes to LocalSettings
8. ViewModel calls `_graphQL.SetServerUrl(url)` to update the singleton
9. Navigate back to LoginPage

### Auto-login flow
1. `OnLaunched` runs
2. App calls `auth.TryAutoLoginAsync()` (already implemented)
3. Inside: reads token from LocalSettings, sets on GraphQL client, calls `me` query
4. Returns `bool`
5. If `true`: `app.OpenMainWindow()`
6. If `false`: continue with LoginWindow as before

## Implementation outline

### ConfigViewModel
```csharp
public partial class ConfigViewModel : ObservableObject
{
    [ObservableProperty] string _serverUrl = "";
    [ObservableProperty] string? _testResult;
    [ObservableProperty] bool _isTesting;

    private readonly IConfigService _config;
    private readonly IGraphQLClientService _graphQL;

    public ConfigViewModel(IConfigService config, IGraphQLClientService graphQL)
    {
        _config = config;
        _graphQL = graphQL;
        ServerUrl = _config.GetServerUrl();
    }

    [RelayCommand]
    private async Task TestConnectionAsync() { /* ping server */ }

    [RelayCommand]
    private void Save() { _config.SetServerUrl(ServerUrl); _graphQL.SetServerUrl(ServerUrl); }
}
```

### App.xaml.cs change
```csharp
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    var auth = Ioc.Default.GetRequiredService<IAuthService>();
    var loggedIn = await auth.TryAutoLoginAsync();
    if (loggedIn) {
        OpenMainWindow();
    } else {
        _loginWindow = new LoginWindow();
        _loginWindow.Activate();
    }
}
```

## Business rules

- Default server URL if none saved: from `appsettings.json` (`GraphQL.Endpoint`)
- ConfigPage is accessible BEFORE login (per instructor: "Config khác với Settings, ví dụ Settings... là cấu hình sau khi đã đăng nhập thành công, Config là cấu hình để có thể đăng nhập được")
- Test Connection is optional but recommended before saving
- Saved URL persists across app restarts (LocalSettings)
- JWT auto-login only succeeds if token is valid AND not expired (server enforces 7-day expiry)
- "Remember me" checkbox controls whether token is saved AT ALL on login

## Edge cases

| Case | Behavior |
|------|----------|
| Server URL is malformed | Save still allowed; login attempt will fail with helpful error |
| Server unreachable on Test Connection | Show "Connection failed: <reason>" |
| Token expired during auto-login | `TryAutoLoginAsync` returns false → show LoginPage |
| User clears browser-style data | LocalSettings cleared → no auto-login, default URL used |
| User opens ConfigPage but cancels | URL not saved, returns to LoginPage with old URL |

## Verification

1. Fresh install: open app → LoginPage with default URL displayed
2. Click Config → ConfigPage opens, shows current URL
3. Change URL to `http://invalid-host:9999/graphql` → Test Connection fails
4. Change back to `http://localhost:4000/graphql` → Test Connection succeeds → Save
5. Restart app → still uses the saved URL
6. Login with admin/admin123, check "Remember me" → MainWindow opens
7. Close app, reopen → MainWindow opens directly (auto-login)
8. Logout → LoginPage shown again, token cleared

## Extension points

- Add multiple saved server profiles (for staging/prod toggling)
- Add fingerprint/Windows Hello for credential autofill
- Add "Forgot password" link (would need server-side reset flow)
