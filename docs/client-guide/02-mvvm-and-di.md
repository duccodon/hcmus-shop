# 02 — MVVM and Dependency Injection

Two patterns power our app:

- **MVVM** (Model-View-ViewModel) — separates UI from logic
- **DI** (Dependency Injection) — services get given to you instead of you creating them

## Part A: MVVM with CommunityToolkit.Mvvm

### The traditional way (without the toolkit)

```csharp
public class LoginViewModel : INotifyPropertyChanged
{
    private string _username;
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Username)));
        }
    }
    public event PropertyChangedEventHandler PropertyChanged;
}
```

This is super verbose for every property.

### Our way (with CommunityToolkit.Mvvm)

```csharp
public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty]
    private string _username = string.Empty;
}
```

That's it. The `[ObservableProperty]` attribute (combined with `partial class`) **generates** the public `Username` property AT COMPILE TIME, complete with `PropertyChanged` notifications.

Inside this ViewModel:
- You write `_username` (the field, lowercase)
- The XAML binds to `Username` (the public property, PascalCase)

When you set `Username = "admin"`, the toolkit auto-fires `PropertyChanged`, and any UI bound to `Username` updates instantly.

### `[RelayCommand]`

For button clicks:

```csharp
[RelayCommand]
private async Task LoginAsync()
{
    // login logic
}
```

This generates a property called `LoginCommand` (an `ICommand`) that the XAML can bind to:

```xml
<Button Command="{Binding LoginCommand}" Content="Login" />
```

When the user clicks the button, `LoginAsync()` runs. Done.

### Key MVVM rule

**The ViewModel must NOT know about `Page`, `Button`, `TextBox`, or any UI control.** It only knows:
- Properties (state)
- Commands (actions)
- Services (injected via constructor)
- Events (to ask the View to do something it shouldn't itself do, like opening a window)

If a ViewModel needs to "open MainWindow", it raises an event. The page listens to that event and does the actual window-opening.

## Part B: Dependency Injection

### The problem DI solves

Without DI:
```csharp
public class LoginViewModel
{
    private readonly AuthService _authService = new AuthService();
}
```

This is bad because:
1. The ViewModel **knows how to construct** AuthService (which may itself need parameters)
2. You can't swap AuthService for a fake version in tests
3. If AuthService needs a `IGraphQLClientService`, it has to construct one too — and so on, an infinite chain

### With DI

```csharp
public class LoginViewModel
{
    private readonly IAuthService _authService;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }
}
```

The ViewModel just **declares** what it needs. The DI container **provides** it.

### Where DI is set up: App.xaml.cs

```csharp
var services = new ServiceCollection();
services.AddSingleton<IAuthService, AuthService>();
services.AddSingleton<IGraphQLClientService>(new GraphQLClientService(serverUrl));
services.AddTransient<LoginViewModel>();

Ioc.Default.ConfigureServices(services.BuildServiceProvider());
```

This says:
- "When someone asks for `IAuthService`, give them an `AuthService`. Use the same instance forever (Singleton)."
- "When someone asks for `IGraphQLClientService`, give them this specific instance I just built."
- "When someone asks for `LoginViewModel`, build a new one each time (Transient)."

The DI container automatically figures out: "`AuthService` needs `IGraphQLClientService`? Sure, here's the singleton you registered."

### Singleton vs Transient

| Lifetime | Behavior | When to use |
|----------|----------|-------------|
| **Singleton** | One instance for the whole app | Services with shared state (auth, HTTP client, config) |
| **Transient** | New instance every time | ViewModels (each page wants fresh state) |
| Scoped | Per-scope (we don't use scopes) | — |

### How a page gets its ViewModel

```csharp
ViewModel = Ioc.Default.GetRequiredService<LoginViewModel>();
```

`Ioc.Default` is the global container. `GetRequiredService<T>()` returns an instance, throwing if not registered.

### Why interfaces?

We register `IAuthService → AuthService`, not just `AuthService`. The reason: if you want to test the ViewModel without a real server, you can register a fake:

```csharp
services.AddSingleton<IAuthService, FakeAuthService>();
```

Without changing any ViewModel code.

## Putting it together

```
App.xaml.cs starts up
    ↓
Registers all services + viewmodels in DI container
    ↓
OnLaunched creates LoginWindow
    ↓
LoginWindow creates LoginPage
    ↓
LoginPage constructor: Ioc.Default.GetRequiredService<LoginViewModel>()
    ↓
DI sees LoginViewModel needs IAuthService → constructs AuthService
                AuthService needs IGraphQLClientService → uses singleton
    ↓
LoginPage.DataContext = ViewModel
    ↓
XAML bindings activate, user sees the login form
```
