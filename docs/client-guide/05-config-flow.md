# 05 — ConfigPage Flow

Pre-login screen for configuring the GraphQL server URL.

## Why we have a ConfigPage

Per the instructor's requirement, the user must be able to change which server the app talks to BEFORE logging in. Our equivalent of "configure the database" (in client-direct-DB designs) is "configure the server URL" because we have a GraphQL middle tier.

## Files in this story

| File | Role |
|------|------|
| `Contracts/Services/IConfigService.cs` | Interface for read/write the saved URL |
| `Services/Config/ConfigService.cs` | Implementation using LocalSettings |
| `ViewModels/Auth/ConfigViewModel.cs` | URL field, Test/Save/Cancel/Reset commands |
| `Views/Pages/Auth/ConfigPage.xaml` | Form layout |
| `Views/Pages/Auth/ConfigPage.xaml.cs` | Code-behind, exposes Saved/Cancelled events |
| `Views/Pages/Auth/LoginWindow.cs` | Hosts a Frame, navigates between LoginPage and ConfigPage |
| `Views/Pages/Auth/LoginPage.xaml.cs` | Raises ConfigRequested event when user clicks Config |
| `ViewModels/Auth/LoginViewModel.cs` | Has OpenConfigCommand and OpenConfigRequested event |

## Click flow

### 1. User on LoginPage clicks "Config" button

```xml
<Button Command="{Binding OpenConfigCommand}">
    <FontIcon Glyph="&#xE713;" />
</Button>
```

### 2. LoginViewModel.OpenConfig fires the event

```csharp
[RelayCommand]
private void OpenConfig()
{
    OpenConfigRequested?.Invoke(this, EventArgs.Empty);
}
```

The ViewModel doesn't open a page itself — it raises an event.

### 3. LoginPage forwards the event

```csharp
ViewModel.OpenConfigRequested += OnOpenConfigRequested;

private void OnOpenConfigRequested(object? sender, EventArgs e)
{
    ConfigRequested?.Invoke(this, EventArgs.Empty);
}
```

The page exposes its own `ConfigRequested` event so the **window** can listen.

### 4. LoginWindow swaps the Frame to ConfigPage

```csharp
private void ShowLoginPage()
{
    _frame.Navigate(typeof(LoginPage));
    if (_frame.Content is LoginPage page)
        page.ConfigRequested += OnConfigRequested;
}

private void OnConfigRequested(object? sender, EventArgs e)
{
    _frame.Navigate(typeof(ConfigPage));
    if (_frame.Content is ConfigPage page)
    {
        page.Saved += OnConfigDoneAndReturn;
        page.Cancelled += OnConfigDoneAndReturn;
    }
}

private void OnConfigDoneAndReturn(object? sender, EventArgs e)
{
    ShowLoginPage();
}
```

Notice the pattern:
- LoginWindow owns a `Frame` (a navigation container)
- LoginWindow subscribes to events from whatever page is currently in the frame
- When ConfigPage saves or cancels → swap back to LoginPage

This is a tiny custom router for two screens. We don't need a global router for an app this small.

### 5. ConfigViewModel constructor reads current URL

```csharp
public ConfigViewModel(IConfigService config, IGraphQLClientService graphQL)
{
    _config = config;
    _graphQL = graphQL;
    ServerUrl = _config.GetServerUrl();
}
```

Pre-fills the TextBox with whatever's currently saved.

### 6. User clicks "Test Connection"

```csharp
[RelayCommand]
private async Task TestConnectionAsync()
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    var content = new StringContent("{\"query\":\"{ __typename }\"}", Encoding.UTF8, "application/json");
    var response = await http.PostAsync(ServerUrl, content);
    SetStatus(response.IsSuccessStatusCode ? "Connection successful." : $"Server responded with {(int)response.StatusCode}.", !response.IsSuccessStatusCode);
}
```

Sends the cheapest possible GraphQL query (`{ __typename }`) to verify the server is reachable. Doesn't change any state.

### 7. User clicks "Save"

```csharp
[RelayCommand]
private void Save()
{
    _config.SetServerUrl(ServerUrl.Trim());        // persist to LocalSettings
    _graphQL.SetServerUrl(ServerUrl.Trim());        // update the singleton client
    Saved?.Invoke(this, EventArgs.Empty);
}
```

Two things happen:
- Save to disk (LocalSettings) so next launch uses this URL
- Update the in-memory GraphQL client so this session uses the URL right away

Then raises `Saved` event, which bubbles up to the LoginWindow, which swaps the frame back to LoginPage.

### 8. User on LoginPage logs in with the new URL

When `LoginViewModel.LoginAsync` calls `_graphQL.MutateAsync(...)`, the request goes to the new URL.

## ConfigService internals

```csharp
public class ConfigService : IConfigService
{
    private const string ServerUrlKey = "config_server_url";

    public string GetServerUrl()
    {
        var v = ApplicationData.Current.LocalSettings.Values[ServerUrlKey] as string;
        return !string.IsNullOrWhiteSpace(v) ? v : GetDefaultServerUrl();
    }

    public void SetServerUrl(string url)
    {
        ApplicationData.Current.LocalSettings.Values[ServerUrlKey] = url;
    }

    public string GetDefaultServerUrl()
        => _configuration["GraphQL:Endpoint"] ?? "http://localhost:4000/graphql";
}
```

`ApplicationData.Current.LocalSettings` is WinUI's per-user, per-machine, per-app key-value store. Persists across reinstalls (mostly).

## Why the LoginViewModel doesn't directly open ConfigPage

ViewModels shouldn't know about pages or windows. Otherwise:
- Hard to test (can't run without WinUI)
- Tight coupling: VM and page can't move independently

The event pattern keeps the ViewModel pure and lets the page/window decide what "open config" means.

## Future improvement

Right now ConfigPage uses a singleton GraphQL client. After save, we update the URL on the singleton. If we ever want multiple servers concurrently (e.g. for staging vs prod), we'd need to refactor to per-environment instances.
