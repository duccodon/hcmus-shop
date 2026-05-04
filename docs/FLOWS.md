# Runtime Flows

Exact sequence of what happens at each user action. Use this when debugging or explaining the app to someone.

---

## Flow 1: Cold start (first ever launch)

```
Double-click .exe / .msix
    ↓
WinUI calls App() constructor
    │ App.xaml.cs:39
    ▼
ConfigurationBuilder loads appsettings.json
    │ App.xaml.cs:50
    ▼
DI container built and registered with Ioc.Default
    │ App.xaml.cs:92
    ▼
WinUI calls App.OnLaunched
    │ App.xaml.cs:95
    ▼
Trial check: GetStatus()
    │ → Status: Active (first launch saves trial_start_date)
    ▼
Auto-login check: TryAutoLoginAsync()
    │ → No saved token → returns false
    ▼
LoginWindow created and Activate()'d
    │ App.xaml.cs:106
    ▼
LoginWindow constructor calls ShowLoginPage()
    │ Frame.Navigate(typeof(LoginPage))
    ▼
LoginPage constructor:
    │ - Resolves LoginViewModel from Ioc.Default
    │ - Subscribes to LoginViewModel.LoginSucceeded
    │ - Subscribes to LoginViewModel.OpenConfigRequested
    │ - DataContext = ViewModel
    │ - Reads VersionText from IConfiguration
    │ - Reads ServerUrl from IGraphQLClientService
    ▼
USER sees the login form with default URL displayed
```

---

## Flow 2: User clicks Config

```
LoginPage XAML: <Button Command="{Binding OpenConfigCommand}">
    ↓
LoginViewModel.OpenConfig() runs (RelayCommand)
    ↓
Fires OpenConfigRequested event (no payload)
    ↓
LoginPage.OnOpenConfigRequested handler runs
    ↓
LoginPage fires its own ConfigRequested event
    ↓
LoginWindow.OnConfigRequested handler runs
    │ - _frame.Navigate(typeof(ConfigPage))
    │ - Subscribes to ConfigPage.Saved + .Cancelled
    ▼
ConfigPage constructor:
    │ - Resolves ConfigViewModel from DI
    │ - DataContext = ViewModel
    │ - ConfigViewModel reads current URL from IConfigService
    ▼
USER sees the form with current URL pre-filled
```

---

## Flow 3: User clicks Test Connection

```
ConfigPage XAML: <Button Command="{Binding TestConnectionCommand}">
    ↓
ConfigViewModel.TestConnectionAsync()
    │ - SetStatus("Testing...", false)
    │ - new HttpClient with 5s timeout
    │ - POST {"query":"{ __typename }"} to ServerUrl
    ↓
If HTTP 200: SetStatus("Connection successful.", false)
If HTTP error: SetStatus($"Server responded with {code}.", true)
If timeout/network error: SetStatus($"Connection failed: {ex.Message}", true)
    ↓
StatusMessage and IsStatusError update via ObservableProperty
    ↓
XAML InfoBar shows status (success green or error red)
```

---

## Flow 4: User clicks Save in Config

```
ConfigPage XAML: <Button Command="{Binding SaveCommand}">
    ↓
ConfigViewModel.Save()
    │ - _config.SetServerUrl(ServerUrl.Trim())  // writes to LocalSettings
    │ - _graphQL.SetServerUrl(ServerUrl.Trim()) // updates singleton
    │ - Fires Saved event
    ↓
ConfigPage.OnConfigDoneAndReturn (in LoginWindow)
    ↓
LoginWindow.ShowLoginPage() - swaps frame back to LoginPage
    ↓
USER sees login form again, ServerUrlText updated
```

---

## Flow 5: User logs in

```
LoginPage XAML: <PasswordBox PasswordChanged="PasswordInput_PasswordChanged">
    │ Each keystroke: ViewModel.Password = PasswordInput.Password
    ▼
USER clicks Sign In: <Button Command="{Binding LoginCommand}">
    ↓
LoginViewModel.LoginAsync() (RelayCommand, CanExecute = !IsBusy)
    │ - Validate inputs (set ErrorMessage if blank)
    │ - IsBusy = true
    ▼
AuthService.LoginAsync(username, password)
    │ - Build LoginRequest DTO
    │ - SafeExecuteAsync wraps the call
    ▼
GraphQLClientService.MutateAsync<LoginResponse>(AuthQueries.Login, request)
    │ - Build {query, variables} body
    │ - JSON serialize
    │ - HttpClient.PostAsync to ServerUrl
    ▼
Apollo Server receives POST /graphql
    ↓
authPlugin lets it through (login is in PUBLIC_MUTATIONS)
    ↓
authResolver.Mutation.login(args) runs
    │ - Find user by username via Prisma
    │ - bcrypt.compare(password, user.passwordHash)
    │ - If match: generateToken(payload)
    │ - Return { token, user }
    ▼
Server responds with JSON
    ↓
GraphQLClientService deserializes into LoginResponse
    ↓
AuthService.LoginAsync:
    │ - Token = result.Login.Token
    │ - CurrentUser = result.Login.User
    │ - _graphQL.SetAuthToken(Token)  // Authorization header
    │ - SaveToken(Token)               // LocalSettings
    │ - return true
    ↓
LoginViewModel:
    │ - SaveRememberedUsername()
    │ - Fires LoginSucceeded event
    │ - IsBusy = false
    ↓
LoginPage.OnLoginSucceeded:
    ↓
App.OpenMainWindow():
    │ - new MainWindow()
    │ - Activate
    │ - _loginWindow.Close()
    ↓
MainWindow constructor:
    │ - Resolve services from DI
    │ - ConfigureNavigationByFeatureFlag — hides nav items
    │ - NavigateToDefault — picks first accessible page
    │ - StartOnboardingIfFirstTime — opens TeachingTip if not completed
    ↓
USER sees Dashboard (or last screen if RememberLastScreen)
```

---

## Flow 6: Auto-login on second launch

```
App.OnLaunched
    ↓
Trial check passes (Active or Activated)
    ↓
auth.TryAutoLoginAsync()
    │ - LoadToken() from LocalSettings → savedToken exists
    │ - _graphQL.SetAuthToken(savedToken)
    │ - Call me query
    ▼
Apollo Server: authPlugin allows queries → me resolver runs
    │ - Reads context.user (decoded from JWT)
    │ - If valid, returns user info
    ▼
AuthService:
    │ - Token = savedToken
    │ - CurrentUser = result.Me
    │ - return true
    ↓
App: jump straight to MainWindow (no LoginWindow)
```

If the token is expired:
- Server returns null for me
- AuthService clears the saved token and returns false
- App shows LoginWindow as normal

---

## Flow 7: Dashboard loads its data

```
USER navigates to Dashboard via sidebar
    ↓
MainWindow.NavigateTo("Dashboard")
    ↓
ContentFrame.Navigate(typeof(DashboardPage))
    ↓
DashboardPage constructor:
    │ - ViewModel = Ioc.Default.GetRequiredService<DashboardViewModel>()
    │ - Loaded += async (s, e) => await ViewModel.RefreshAsync()
    ▼
Loaded event fires when page becomes visible
    ↓
DashboardViewModel.RefreshAsync()
    │ - IsLoading = true
    ▼
DashboardService.GetStatsAsync()
    │ - SafeExecuteAsync wraps
    │ - QueryAsync<DashboardStatsResponse>(DashboardQueries.GetStats)
    ▼
HTTP POST /graphql with the query
    ↓
authPlugin allows queries
    ↓
dashboardResolver.Query.dashboardStats() = dashboardService.getStats()
    │ - Promise.all of 7 repository methods
    │ - Each method: a single Prisma query
    ▼
Server returns { totalProducts, totalOrdersToday, ..., dailyRevenue: [...] }
    ↓
Client deserializes into DashboardStatsDto
    ↓
DashboardViewModel.ApplyStats(dto):
    │ - KpiCards.Clear() and add 4 new ones
    │ - RecentInvoices.Clear() and add from dto.RecentOrders
    │ - TopSoldProducts.Clear() and add from dto.TopSellingProducts
    │ - LowStockProducts.Clear() and add from dto.LowStockProducts
    │ - InvoiceLegends/InvoiceSeries from grouped recent orders
    │ - SalesSeries with LineSeries values from dto.DailyRevenue
    │ - SalesXAxes with day labels
    ▼
ObservableCollection raises CollectionChanged events
    ↓
XAML bindings auto-update (ItemsControl, charts)
    ↓
USER sees real KPIs
```

---

## Flow 8: User changes settings

```
USER navigates to Settings → tweaks page size dropdown → toggle remember-last → click Save
    ↓
SettingsViewModel.PageSize property setter (auto-generated by [ObservableProperty])
    │ - Updates the field
    │ - Fires PropertyChanged
    ▼
USER clicks Save
    ↓
SettingsViewModel.Save() (RelayCommand)
    │ - _settings.PageSize = PageSize  // writes LocalSettings
    │ - _settings.RememberLastScreen = RememberLastScreen
    │ - SetStatus("Settings saved.", false)
    ▼
StatusMessage updates → InfoBar shows green
```

Then user navigates around:

```
MainWindow.NavigateTo("Products")  // or any nav target
    ↓
After navigation, sets _settings.LastScreen = "Products"
    ↓
On next login (or restart):
    NavigateToDefault checks RememberLastScreen + LastScreen
    if both set + LastScreen accessible → NavigateTo(LastScreen)
```

---

## Flow 9: Backup download

```
USER on SettingsPage clicks "Download Backup"
    ↓
SettingsViewModel.DownloadBackupAsync (RelayCommand)
    │ - IsBusy = true (disables both backup buttons)
    │ - SetStatus("Preparing backup...", false)
    ▼
Create FileSavePicker, attach to WindowHandle
    │ Suggested filename: hcmus-shop-backup-YYYYMMDD-HHmm.sql
    ▼
USER picks save location
    ↓
BackupService.DownloadBackupAsync(targetPath)
    │ - HttpClient with 2-min timeout
    │ - GET {ServerBaseUrl}/backup
    ▼
Server: backupRouter handles GET /backup
    │ - spawn pg_dump with DATABASE_URL
    │ - pipe stdout into res
    ▼
Client receives streaming response
    │ - File.Create(targetPath)
    │ - resp.Content.CopyToAsync(fileStream)
    ▼
SetStatus($"Backup saved: {filename}", false)
IsBusy = false
```

---

## Flow 10: Trial expiry

```
App.OnLaunched
    ↓
trial.GetStatus()
    │ - Read trial_activated from LocalSettings → false
    │ - Read trial_start_date → some old date (more than 15 days ago)
    │ - Calculate elapsed days → 16
    │ - return TrialStatus.Expired
    ↓
ShowTrialExpiredWindow() instead of LoginWindow
    │ - new TrialExpiredWindow()
    │ - Activate
    ▼
TrialExpiredWindow → TrialExpiredPage
    ↓
USER types "HCMUS2026" → clicks Activate
    ↓
TrialExpiredViewModel.Activate (RelayCommand)
    │ - _trial.Activate(code) returns true
    │ - Fires Activated event
    ↓
TrialExpiredPage.OnActivated:
    ↓
App.RelaunchAfterTrialActivation():
    │ - Close trial window
    │ - Try auto-login
    │ - Open MainWindow if successful, else LoginWindow
```

---

## Flow 11: Onboarding tutorial

```
After login → MainWindow constructor → StartOnboardingIfFirstTime
    ↓
_onboarding.IsCompleted → false (first time)
    ↓
WelcomeTip.IsOpen = true
    │ TeachingTip "Welcome to HCMUS Shop!" shown center-screen
    ▼
USER clicks Next on welcome tip
    ↓
MainWindow.OnTipNext(WelcomeTip, args):
    │ - sender.IsOpen = false
    │ - DashboardTip.Target = DashboardItem
    │ - DashboardTip.IsOpen = true
    ▼
USER clicks Next → ProductsTip → Next → SettingsTip → Done
    ↓
MainWindow.OnTipFinish:
    │ - sender.IsOpen = false
    │ - _onboarding.MarkCompleted()  // sets onboarding_completed = true
    ↓
On next login, _onboarding.IsCompleted → true → no tour shown
```

User can also click Skip on any tip → OnTipSkip → MarkCompleted.

---

## Flow 12: Logout

```
USER clicks "Log Out" in sidebar footer
    ↓
MainWindow.NavigateTo("Logout"):
    │ - _authService.Logout()
    │     │ - CurrentUser = null
    │     │ - Token = null
    │     │ - _graphQL.SetAuthToken(null)
    │     │ - ClearToken() (LocalSettings)
    │ - app.OpenLoginWindow()
    │     │ - new LoginWindow + Activate
    │     │ - _mainWindow = null
    │ - Close()  // close MainWindow
    ↓
USER sees LoginWindow
```

---

## Quick reference: who fires what event

| Event | Defined in | Listened by |
|-------|------------|-------------|
| `LoginViewModel.LoginSucceeded` | LoginViewModel | LoginPage (calls App.OpenMainWindow) |
| `LoginViewModel.OpenConfigRequested` | LoginViewModel | LoginPage (raises ConfigRequested) |
| `LoginPage.ConfigRequested` | LoginPage | LoginWindow (swaps frame to ConfigPage) |
| `ConfigViewModel.Saved` | ConfigViewModel | ConfigPage (raises Saved) |
| `ConfigViewModel.Cancelled` | ConfigViewModel | ConfigPage (raises Cancelled) |
| `ConfigPage.Saved` | ConfigPage | LoginWindow (swaps back to LoginPage) |
| `ConfigPage.Cancelled` | ConfigPage | LoginWindow (swaps back to LoginPage) |
| `TrialExpiredViewModel.Activated` | TrialExpiredViewModel | TrialExpiredPage (calls App.RelaunchAfterTrialActivation) |
| `AddProductViewModel.ProductSaved` | AddProductViewModel | (Dev B's responsibility — likely AddProductPage) |
