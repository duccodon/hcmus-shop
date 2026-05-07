# 01 — Anatomy of a WinUI Page

In WinUI 3, every screen is a `Page`. A page has TWO files:

- **`SomePage.xaml`** — the UI layout (XML-style markup)
- **`SomePage.xaml.cs`** — the "code-behind" (C# class that manages this specific page)

These two files are connected by the `partial class` pattern. The `.xaml` is compiled into part of the same class as the `.xaml.cs`.

## Example: ConfigPage

### ConfigPage.xaml (the UI)

```xml
<Page x:Class="hcmus_shop.Views.ConfigPage" ...>
    <Grid>
        <TextBox Text="{Binding ServerUrl, Mode=TwoWay}" />
        <Button Content="Save" Command="{Binding SaveCommand}" />
    </Grid>
</Page>
```

This says:
- This page's class is `hcmus_shop.Views.ConfigPage`
- It has a TextBox **bound** to a property called `ServerUrl`
- It has a Button **bound** to a command called `SaveCommand`

Bindings find these properties on the page's `DataContext`. Read on.

### ConfigPage.xaml.cs (the code-behind)

```csharp
public sealed partial class ConfigPage : Page
{
    public ConfigViewModel ViewModel { get; }

    public ConfigPage()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<ConfigViewModel>();
        DataContext = ViewModel;
    }
}
```

What this does:
1. **`InitializeComponent()`** — runs the XAML compiler-generated code that wires up controls from the `.xaml` file
2. **`Ioc.Default.GetRequiredService<ConfigViewModel>()`** — asks the Dependency Injection container for an instance of `ConfigViewModel` (more on this in [02](02-mvvm-and-di.md))
3. **`DataContext = ViewModel`** — makes that ViewModel the data source for all `{Binding ...}` expressions in the XAML

Now when the XAML says `{Binding ServerUrl}`, WinUI looks up `ServerUrl` on `ViewModel`.

## Bindings explained

```xml
<TextBox Text="{Binding ServerUrl, Mode=TwoWay}" />
```

| Part | Meaning |
|------|---------|
| `Text=` | The control's `Text` property |
| `{Binding ServerUrl}` | Look up `ServerUrl` on the `DataContext` |
| `Mode=TwoWay` | When user types, also push the value back into the property |

Without `Mode=TwoWay`, typing in the TextBox wouldn't update the ViewModel.

## Bindings vs `x:Bind`

You'll see two styles in our codebase:

- `{Binding Foo}` — looks up `Foo` on `DataContext`. Works at runtime, more flexible.
- `{x:Bind ViewModel.Foo}` — strongly-typed, compile-time checked, faster. The XAML compiler validates that `ViewModel` has a `Foo` property.

We use `{Binding}` for most ViewModels (set via `DataContext = ViewModel`). We use `{x:Bind}` when the property is on the page itself (like `VersionText` in `LoginPage.xaml.cs`).

## The Loaded event

```csharp
public DashboardPage()
{
    ViewModel = Ioc.Default.GetRequiredService<DashboardViewModel>();
    InitializeComponent();
    Loaded += async (s, e) => await ViewModel.RefreshAsync();
}
```

`Loaded` fires the first time the page becomes visible. Use it to **trigger initial data loads**. Don't put async work in the constructor — constructors can't `await`.

## Mental model

Think of a Page as:
- **What** (the XAML): describes the layout
- **Wiring** (the code-behind constructor): connects the page to its ViewModel

The actual logic (what happens when you click the button) lives in the **ViewModel**, not the page. That's the next doc.
