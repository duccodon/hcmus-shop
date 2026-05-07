# 09 — How to Add a New Feature

Recipe for adding a feature end-to-end. Use this as a checklist.

## Example: "Customer management" feature

We want to:
- List customers in a table
- View one customer's details
- Add / edit / delete customers
- Each customer has loyalty points

## Step 1: Backend GraphQL feature module

Create `hcmus-shop-server/src/features/customer/`:

```
customer/
├── customer.typeDef.graphql      ← schema
├── customer.dto.ts                ← TypeScript interfaces (input shapes)
├── customer.repository.ts         ← Prisma queries
├── customer.service.ts            ← business logic
└── customer.resolver.ts           ← thin Query/Mutation router
```

Wire into `src/index.ts`:
```typescript
import { customerResolver } from "./features/customer/customer.resolver";
// ...
const typeDefs = [..., loadTypeDef("customer/customer.typeDef.graphql")].join("\n");
const resolvers = mergeResolvers(..., customerResolver);
```

Test in Apollo Sandbox at `http://localhost:4000/graphql`.

## Step 2: Client DTOs

Create `hcmus-shop/Models/DTOs/CustomerDto.cs`:

```csharp
public class CustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int LoyaltyPoints { get; set; }
}

public class CreateCustomerInput
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
```

## Step 3: GraphQL query strings

Create `hcmus-shop/GraphQL/Operations/CustomerQueries.cs`:

```csharp
public static class CustomerQueries
{
    public const string GetAll = @"
        query Customers {
            customers { customerId name phone loyaltyPoints }
        }";

    public const string Create = @"
        mutation CreateCustomer($input: CreateCustomerInput!) {
            createCustomer(input: $input) { customerId name }
        }";
}
```

## Step 4: Service interface + implementation

`Contracts/Services/ICustomerService.cs`:

```csharp
public interface ICustomerService
{
    Task<Result<List<CustomerDto>>> GetAllAsync();
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerInput input);
}
```

`Services/Customers/CustomerService.cs`:

```csharp
public class CustomerService : ICustomerService
{
    private readonly IGraphQLClientService _graphQL;

    public CustomerService(IGraphQLClientService graphQL) => _graphQL = graphQL;

    public async Task<Result<List<CustomerDto>>> GetAllAsync()
    {
        var result = await (_graphQL as GraphQLClientService)!
            .SafeExecuteAsync(() =>
                _graphQL.QueryAsync<CustomersResponse>(CustomerQueries.GetAll));

        if (!result.IsSuccess)
            return Result<List<CustomerDto>>.Failure(result.Error!);

        return Result<List<CustomerDto>>.Success(result.Value!.Customers);
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerInput input)
    {
        // similar pattern
    }

    private class CustomersResponse { public List<CustomerDto> Customers { get; set; } = new(); }
}
```

## Step 5: ViewModel

`ViewModels/Customers/CustomersViewModel.cs`:

```csharp
public partial class CustomersViewModel : ObservableObject
{
    private readonly ICustomerService _service;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _errorMessage;

    public ObservableCollection<CustomerDto> Customers { get; } = new();

    public CustomersViewModel(ICustomerService service) { _service = service; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        var result = await _service.GetAllAsync();
        if (result.IsSuccess)
        {
            Customers.Clear();
            foreach (var c in result.Value!) Customers.Add(c);
        }
        else
        {
            ErrorMessage = result.Error;
        }
        IsLoading = false;
    }
}
```

## Step 6: View

`Views/Pages/Customers/CustomersPage.xaml`:

```xml
<Page x:Class="hcmus_shop.Views.Customers.CustomersPage" ...>
    <Grid>
        <ListView ItemsSource="{x:Bind ViewModel.Customers, Mode=OneWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="dto:CustomerDto">
                    <StackPanel Orientation="Horizontal" Spacing="12">
                        <TextBlock Text="{x:Bind Name}" />
                        <TextBlock Text="{x:Bind Phone}" />
                        <TextBlock Text="{x:Bind LoyaltyPoints}" />
                    </StackPanel>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />

        <InfoBar
            IsOpen="{x:Bind ViewModel.ErrorMessage, Converter={StaticResource StringToBooleanConverter}, Mode=OneWay}"
            Severity="Error"
            Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}" />
    </Grid>
</Page>
```

`Views/Pages/Customers/CustomersPage.xaml.cs`:

```csharp
public sealed partial class CustomersPage : Page
{
    public CustomersViewModel ViewModel { get; }

    public CustomersPage()
    {
        ViewModel = Ioc.Default.GetRequiredService<CustomersViewModel>();
        InitializeComponent();
        Loaded += async (s, e) => await ViewModel.RefreshAsync();
    }
}
```

## Step 7: DI registration in App.xaml.cs

```csharp
services.AddSingleton<ICustomerService, CustomerService>();
services.AddTransient<CustomersViewModel>();
```

## Step 8: Add to MainWindow navigation

In `MainWindow.xaml`:
```xml
<NavigationViewItem Tag="Customers" Content="Customers">
    <NavigationViewItem.Icon><FontIcon Glyph="&#xE716;" /></NavigationViewItem.Icon>
</NavigationViewItem>
```

In `MainWindow.xaml.cs.NavigateTo`:
```csharp
case "Customers":
    NavigateOrForbid(typeof(CustomersPage), "Customers");
    break;
```

In `appsettings.json` feature flags:
```json
"FeatureFlags": {
    "AdminFeature": ["Dashboard", "Admin", "Customers"]
}
```

## Step 9: Update .csproj if needed

If your XAML files aren't auto-discovered, add to `hcmus-shop.csproj`:
```xml
<Page Include="Views\Pages\Customers\**\*.xaml" />
```

## Step 10: Build and test

1. Run backend: `npm run dev` in `hcmus-shop-server/`
2. Open Apollo Sandbox, test customer queries directly
3. Build WinUI in Visual Studio
4. Login as Admin, navigate to Customers
5. Verify list loads, create works, etc.

## Common gotchas

| Problem | Cause | Fix |
|---------|-------|-----|
| Page doesn't show up in nav | Not in MainWindow + feature flags | Add both |
| ViewModel not resolving from DI | Not registered | `services.AddTransient<XxxViewModel>()` |
| Data not displaying | Forgot `Mode=OneWay` on x:Bind, OR didn't `Clear` and re-add to ObservableCollection | Check both |
| GraphQL "Cannot return null" error | Field marked non-nullable but resolver returned null | Make field nullable in typeDef OR don't return null |
| 401 from server | Mutation without JWT (auth plugin requires it) | Login first, ensure SetAuthToken was called |
| JSON property name mismatch | C# `BrandId` vs JSON `brandId` | We use `JsonNamingPolicy.CamelCase` — should "just work"; verify your DTO is PascalCase |

## TL;DR template

```
Server:  features/X/X.{typeDef.graphql, repository.ts, service.ts, resolver.ts}
         + register in index.ts
Client:  Models/DTOs/XDto.cs
         GraphQL/Operations/XQueries.cs
         Contracts/Services/IXService.cs
         Services/X/XService.cs
         ViewModels/X/XViewModel.cs
         Views/Pages/X/XPage.xaml + .xaml.cs
         + register service + viewmodel in App.xaml.cs
         + add nav item in MainWindow if user-facing
```
