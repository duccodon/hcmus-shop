# 08 — Role-based UI Hiding

How to hide UI elements based on the logged-in user's role.

## The two-layer defense

1. **Server side**: the `Product.importPrice` resolver returns `null` for non-Admin
2. **Client side**: a converter hides UI elements that should only be visible to certain roles

The server is the source of truth. The client hides things to keep the UI clean and to avoid showing meaningless data.

## RoleVisibilityConverter

```csharp
public class RoleVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var requiredRole = parameter as string ?? "Admin";
        var auth = Ioc.Default.GetService<IAuthService>();

        var allowed = string.Equals(requiredRole, "Admin", StringComparison.OrdinalIgnoreCase)
            ? string.Equals(auth?.CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase)
            : auth?.HasRole(requiredRole) ?? false;

        return allowed ? Visibility.Visible : Visibility.Collapsed;
    }
}
```

### How it works

- WinUI calls `Convert` with whatever `value` you bind to (we don't actually use it)
- `parameter` is the role you require (e.g. "Admin")
- We ask the DI container for `IAuthService`
- For "Admin"-only elements, do an EXACT match on Admin
- For other roles, use `HasRole` which permits Admin + matching role

### Why exact match for Admin?

`HasRole("Admin")` would return true for both Admin and (potentially) other roles if the implementation grants Admin universal access. To be precise about "this is for Admins only", we use exact equality.

## Registration

In `App.xaml`:

```xml
<converters:RoleVisibilityConverter x:Key="RoleVisibility" />
```

## Usage in XAML

```xml
<DataGridTextColumn
    Header="Import Price"
    Binding="{Binding ImportPrice}"
    Visibility="{Binding Converter={StaticResource RoleVisibility}, ConverterParameter=Admin}" />
```

When Sale logs in, this column collapses (zero width). When Admin logs in, it's visible.

## Limitation: visibility only updates on bind

If the user **changes role mid-session** (logs out and back in as different role), the converter only re-evaluates if the binding gets re-evaluated. For our app this is fine because logout creates a new MainWindow with fresh bindings.

If you need live-update visibility (rare), use a property on the ViewModel:

```csharp
public bool IsAdmin => _authService.CurrentUser?.Role == "Admin";
```

```xml
<TextBlock Visibility="{Binding IsAdmin, Converter={StaticResource BooleanToVisibilityConverter}}" />
```

Then raise `OnPropertyChanged(nameof(IsAdmin))` whenever auth state changes.

## Server side: Product.importPrice resolver

```typescript
importPrice: (
    parent: { importPrice: Prisma.Decimal },
    _: unknown,
    context: Context
) => {
    if (context.user?.role !== "Admin") return null;
    return Number(parent.importPrice);
}
```

The third resolver argument is `context`, which contains the JWT-decoded user info. If not Admin, return null. Otherwise return the actual number.

In the GraphQL schema:
```graphql
type Product {
  importPrice: Float    # nullable!
  ...
}
```

Marking it nullable is critical — otherwise GraphQL would error when returning null for non-Admin requests.

## To add role-based filtering to a new field

1. **Server**: in the resolver, check `context.user?.role` and return null if not authorized
2. **Schema**: mark the field nullable (drop the `!`)
3. **Client DTO**: make the C# property nullable (`double?` instead of `double`)
4. **Client XAML**: bind `Visibility` with `RoleVisibilityConverter` so the UI element collapses if the data is null/the user lacks the role

## To add a new role

1. Server: in seed data or migrations, add the role to `User.role`
2. Server: in resolvers that check role, add the new role to the allowed list
3. Client: `IFeatureFlagService` (existing) maps roles to nav items — update its config in `appsettings.json` if the new role should see/hide nav items

Currently we have just `Admin` and `Sale` — no need for more.
