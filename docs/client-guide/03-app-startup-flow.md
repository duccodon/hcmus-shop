# 03 — App Startup Flow

What happens between double-clicking the .exe and seeing the login screen?

## Files involved

| File | Role |
|------|------|
| `App.xaml.cs` | Entry point, DI setup, decides which window to open |
| `App.xaml` | Defines app-wide resources (converters, styles) |
| `LoginWindow.cs` | The pre-login window |
| `MainWindow.xaml + .xaml.cs` | The post-login window (sidebar + content) |
| `appsettings.json` | Default config (server URL, app version) |

## Step-by-step

### 1. WinUI runtime calls `App()` constructor

```csharp
public App()
{
    InitializeComponent();
    // ... configuration setup ...
    // ... DI registration ...
}
```

Inside the constructor:

#### 1a. Read configuration files

```csharp
var builder = new ConfigurationBuilder()
    .SetBasePath(appDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .AddEnvironmentVariables();
Configuration = builder.Build();
```

Loads `appsettings.json`. This is where the **default GraphQL endpoint** comes from.

#### 1b. Determine server URL

```csharp
var serverUrl = GetConfiguredServerUrl();
```

This checks `LocalSettings` for a saved override (set via ConfigPage). If none, falls back to `appsettings.json`.

**Why this matters**: the user might have set a different server URL last time they used the app. We respect that.

#### 1c. Build the DI container

```csharp
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(Configuration);
services.AddSingleton<IGraphQLClientService>(new GraphQLClientService(serverUrl));
services.AddSingleton<IConfigService, ConfigService>();
services.AddSingleton<IAuthService, AuthService>();
// ... more services ...
services.AddTransient<LoginViewModel>();
// ... more viewmodels ...

Ioc.Default.ConfigureServices(services.BuildServiceProvider());
```

Now `Ioc.Default` is ready. Anyone can ask for any registered service.

### 2. WinUI runtime calls `OnLaunched()`

```csharp
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    var auth = Ioc.Default.GetRequiredService<IAuthService>();
    var loggedIn = await auth.TryAutoLoginAsync();

    if (loggedIn)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
        return;
    }

    _loginWindow = new LoginWindow();
    _loginWindow.Activate();
}
```

#### 2a. Try silent auto-login

`TryAutoLoginAsync()`:
1. Reads the saved JWT token from `LocalSettings`
2. Sets it on the GraphQL client
3. Calls the `me` query against the server
4. If the server says "yep, that token is valid, here's your user", returns `true`
5. Otherwise returns `false`

#### 2b. If logged in: open MainWindow

Skips the login screen entirely. User sees the dashboard immediately.

#### 2c. If not logged in: open LoginWindow

Shows the login form.

## Why `OnLaunched` is `async void`

Async void is generally bad, but `OnLaunched` is an event handler — WinUI calls it and doesn't `await` it. So `async void` is necessary here.

We could also do `await Task.Run(...)` but the existing pattern is fine.

## State at this point

After `OnLaunched` finishes:
- `Configuration` is loaded
- DI container is ready
- One window is showing (LoginWindow OR MainWindow)
- If MainWindow: `IAuthService.CurrentUser` is set, `Token` is set

## Switching between windows

```csharp
public void OpenMainWindow()       // Called by LoginPage after successful login
public void OpenLoginWindow()      // Called by MainWindow on logout
```

Both methods:
1. Create the new window
2. Activate it
3. Close the old window
4. Null out the old reference

This pattern is needed because each window in WinUI is a top-level OS window, and a closed window can't be re-activated.

## Cheat sheet: where to add a new global service

1. Create the interface in `Contracts/Services/IFoo.cs`
2. Create the implementation in `Services/Foo/FooService.cs`
3. Register in `App.xaml.cs`:
   ```csharp
   services.AddSingleton<IFooService, FooService>();
   ```
4. Inject into any ViewModel constructor that needs it.

That's it.
