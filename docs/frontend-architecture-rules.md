
## 1. Solution Architecture

```
hcmus-shop/         ← UI project (WinUI 3)
```

> **Note**: EF Core, `DbContext`, and local migrations have been removed. The backend (`hcmus-shop-server`) owns the database entirely. All data access goes through the GraphQL API via the custom `IGraphQLClientService`.

### Dependency direction (strict — never reverse)

```
Page → ViewModel → Service Interface → Service Implementation → IGraphQLClientService → NestJS GraphQL Backend
                ↘ Models/DTOs ↗
```

- Pages know only their ViewModel.
- ViewModels know only service interfaces and `Models/DTOs/`.
- Service implementations inject `IGraphQLClientService`, use `GraphQLClientService.SafeExecuteAsync()` for all calls, and return `Result<T>`.
- `IGraphQLClientService` is the single GraphQL entry point — it handles HTTP transport, auth token injection, and JSON deserialisation.
- `Models/DTOs/` contains full entity shapes mirroring the server — no input/filter types here.
- `Services/{Feature}/Dto/` contains per-feature request inputs and GraphQL response envelopes — these never leave the service layer.
- `GraphQL/Operations/` contains query/mutation strings as `static class` constants — one class per feature.

---

## 2. Folder Structure (canonical — do not deviate)

### hcmus-shop (UI)

```
hcmus-shop/
├── App.xaml / App.xaml.cs              ← DI container registration, navigation setup
├── MainWindow.xaml / .cs
├── appsettings.json                    ← API base URL and non-secret config
├── Assets/
├── Contracts/
│   └── Services/ (may need to create Viewmodels here but not now)
│       ├── IGraphQLClientService.cs    ← QueryAsync, MutateAsync, SetAuthToken, SetServerUrl
│       ├── IAuthService.cs
│       ├── IBrandService.cs
│       ├── ICategoryService.cs
│       ├── ISeriesService.cs
│       ├── IProductService.cs
│       └── IFeatureFlagService.cs
├── Models/
│   ├── Common/
│   │   └── Result.cs                   ← Result<T> with IsSuccess, Value, Error
│   └── DTOs/                           ← Full entity shapes mirroring server; no input/filter types
│       ├── UserDto.cs
│       ├── BrandDto.cs
│       ├── CategoryDto.cs
│       ├── SeriesDto.cs
│       └── ProductDto.cs               ← ProductDto, ProductImageDto, ProductPageDto
├── Converters/
|   ├── BooleanToVisibilityConverter.cs
│   ├── InverseBooleanToVisibilityConverter.cs
│   ├── NullToVisibilityConverter.cs
│   ├── StockStatusTextConverter.cs
│   ├── StockStatusColorConverter.cs
│   └── StatusBadgeConverter.cs
├── Resources/ keep every style globally in there except for padding, corner radius
│   ├── Styles/
│   │   ├── ThemeResources.xaml         ← ALL color/brush tokens
│   │   ├── ButtonStyles.xaml
│   │   ├── TextStyles.xaml
│   │   ├── FormStyles.xaml
│   │   ├── BadgeStyles.xaml
│   │   ├── TableStyles.xaml
│   │   └── ProductTemplates.xaml       ← DataTemplates for product table rows
│   └── Auth/
│       └── LoginStyles.xaml
├── GraphQL/
│   └── Operations/                     ← Query/mutation strings; one static class per feature
│       ├── AuthQueries.cs
│       ├── BrandQueries.cs
│       ├── CategoryQueries.cs
│       ├── ProductQueries.cs
│       └── SeriesQueries.cs
├── Services/
│   ├── GraphQL/
│   │   └── GraphQLClientService.cs     ← Implements IGraphQLClientService; owns HttpClient,
│   │                                      JsonSerializerOptions, SafeExecuteAsync, auth header
│   ├── Auth/
│   │   ├── Dto/
│   │   │   ├── AuthRequest.cs          ← LoginRequest
│   │   │   └── AuthResponse.cs         ← LoginResponse, MeResponse
│   │   └── AuthService.cs
│   ├── Brands/
│   │   └── BrandService.cs
│   ├── Categories/
│   │   └── CategoryService.cs
│   ├── Products/
│   │   ├── Dto/
│   │   │   ├── ProductRequest.cs       ← GetProductsRequest, GetProductByIdRequest,
│   │   │   │                              CreateProductInput, UpdateProductInput, DeleteProductRequest
│   │   │   └── ProductResponse.cs      ← ProductsResponse, ProductResponse,
│   │   │                                  CreateProductResponse, UpdateProductResponse, DeleteProductResponse
│   │   └── ProductService.cs
│   ├── Series/
│   │   └── SeriesService.cs
│   └── FeatureFlagService.cs
├── ViewModels/
│   ├── Admin/
│   ├── Auth/
│   ├── Dashboard/
│   └── Products/
│       ├── ProductsViewModel.cs
│       ├── ProductRowViewModel.cs      ← per-row display state; IsSelected, StatusText, etc.
│       ├── AddProductViewModel.cs
│       ├── CategoryOptionViewModel.cs
│       ├── FilterOptionViewModel.cs
│       ├── LookupOptionViewModel.cs
│       └── PageButtonItem.cs
└── Views/
|   ├── Components/                     ← Shared across all features
|   │   ├── Pagination/
|   │   │   └── PaginationFooter.xaml/.cs
|   │   ├── BulkActionBar.xaml/.cs
|   │   ├── PageHeader.xaml/.cs
|   │   ├── SearchFilterBar.xaml/.cs
|   │   └── StatusBadge.xaml/.cs
|   └── Pages/
|   ├── Admin/
|   │   ├── AdminPage.xaml/.cs
|   │   └── DashboardPage.xaml/.cs  ← admin shell
|   ├── Auth/
|   ├── Dashboard/
|   │   ├── Components/
|   │   │   ├── Charts/
|   │   │   │   ├── InvoiceStatsCard.xaml/.cs
|   │   │   │   └── SalesAnalyticsCard.xaml/.cs
|   │   │   ├── Controls/
│   |   │   │   ├── DashboardCard.xaml/.cs
|   │   │   │   ├── HeaderBar.xaml/.cs
|   │   │   │   ├── KpiCard.xaml/.cs
|   │   │   │   └── SidebarNavigation.xaml/.cs
|   │   │   └── Tables/
|   │   └── DashboardPage.xaml.cs
|   ├── Inventory/
|   ├── Messages/
|   ├── Products/
|   │   ├── AddProductPage.xaml/.cs ← treated as a feature-scoped page; navigated to from ProductsPage
|   │   └── ProductsPage.xaml/.cs
|   ├── Sale/
|   └── Store/
```

---

## 3. GraphQL Communication

### IGraphQLClientService

`Services/GraphQL/GraphQLClientService.cs` implements `IGraphQLClientService` — the single GraphQL entry point for the entire app. It:

- Owns a single `HttpClient` and `JsonSerializerOptions` (camelCase, null-ignore).
- Injects `Authorization: Bearer <token>` via `SetAuthToken(token)` — called by `AuthService` after login and on auto-login.
- Exposes `QueryAsync<T>` and `MutateAsync<T>` — both POST `{ query, variables }` to the server and deserialise `GraphQLResponse<T>`.
- Throws `GraphQLException` if the response contains an `errors` array or `data` is null.
- Exposes `SafeExecuteAsync<T>(Func<Task<T>>)` — wraps any call in try/catch and returns `Result<T>`. **All service calls must go through `SafeExecuteAsync`.**

```csharp
// Services always use SafeExecuteAsync — never call QueryAsync/MutateAsync raw
public async Task<Result<ProductPageDto>> GetAllAsync(ProductFilterDto filter)
{
    var request = new GetProductsRequest { /* map from filter */ };

    var result = await (_graphQL as GraphQLClientService)!
        .SafeExecuteAsync(() =>
            _graphQL.QueryAsync<ProductsResponse>(ProductQueries.GetProducts, request)
        );

    if (!result.IsSuccess)
        return Result<ProductPageDto>.Failure(result.Error!);

    return Result<ProductPageDto>.Success(result.Value!.Products);
}
```

### Token Storage

- JWT stored in `Windows.Storage.ApplicationData.Current.LocalSettings`.
- `AuthService` is the only class that reads/writes the token — no other class accesses `LocalSettings` for auth.
- Never store tokens in memory-only fields that do not survive app restarts.

### GraphQL Response Contract

`GraphQLClientService` deserialises all responses into `GraphQLResponse<T>`:

```csharp
public class GraphQLResponse<T>
{
    public T? Data { get; set; }
    public GraphQLError[]? Errors { get; set; }
}
```

`T` is the feature-specific response envelope defined in `Services/{Feature}/Dto/{Feature}Response.cs`:
