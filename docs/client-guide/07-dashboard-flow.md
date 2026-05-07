# 07 — Dashboard Flow

End-to-end trace for a Dashboard refresh.

## Files in this story

| File | Layer |
|------|-------|
| `Views/Pages/Dashboard/DashboardPage.xaml` | XAML — KPI cards, charts, tables |
| `Views/Pages/Dashboard/DashboardPage.xaml.cs` | Code-behind — resolves VM, triggers refresh on Loaded |
| `ViewModels/Dashboard/DashboardViewModel.cs` | UI state + RefreshAsync + ApplyStats |
| `Contracts/Services/IDashboardService.cs` | Interface |
| `Services/Dashboard/DashboardService.cs` | Implementation — calls GraphQL |
| `Models/DTOs/DashboardDto.cs` | Server response shape |
| `GraphQL/Operations/DashboardQueries.cs` | The query string |
| `Services/GraphQL/GraphQLClientService.cs` | HTTP transport |

Backend side (just so you know what the client is calling):

| File | Role |
|------|------|
| `hcmus-shop-server/src/features/dashboard/dashboard.typeDef.graphql` | GraphQL schema |
| `hcmus-shop-server/src/features/dashboard/dashboard.resolver.ts` | Routes the query to the service |
| `hcmus-shop-server/src/features/dashboard/dashboard.service.ts` | Calls the repository in parallel |
| `hcmus-shop-server/src/features/dashboard/dashboard.repository.ts` | 7 Prisma queries |

## The page constructor

```csharp
public DashboardPage()
{
    ViewModel = Ioc.Default.GetRequiredService<DashboardViewModel>();
    InitializeComponent();
    Loaded += async (s, e) => await ViewModel.RefreshAsync();
}
```

- DI gives us a `DashboardViewModel` (which got `IDashboardService` injected)
- `InitializeComponent` builds the XAML tree
- When the page first becomes visible, fire `RefreshAsync`

## DashboardViewModel.RefreshAsync

```csharp
public async Task RefreshAsync()
{
    if (IsLoading) return;
    IsLoading = true;
    ErrorMessage = null;

    var result = await _dashboardService.GetStatsAsync();
    if (!result.IsSuccess)
    {
        ErrorMessage = result.Error;
        IsLoading = false;
        return;
    }

    ApplyStats(result.Value!);
    IsLoading = false;
}
```

Flow:
1. Guard against double-refresh (`IsLoading`)
2. Show loading spinner (XAML can bind to `IsLoading` for a ProgressRing)
3. Call the service → returns `Result<DashboardStatsDto>`
4. On success: pour data into observable collections
5. On failure: store the error message (XAML shows it in an InfoBar)

## DashboardService.GetStatsAsync

```csharp
public async Task<Result<DashboardStatsDto>> GetStatsAsync()
{
    var result = await (_graphQL as GraphQLClientService)!
        .SafeExecuteAsync(() =>
            _graphQL.QueryAsync<DashboardStatsResponse>(DashboardQueries.GetStats));

    if (!result.IsSuccess)
        return Result<DashboardStatsDto>.Failure(result.Error!);

    return Result<DashboardStatsDto>.Success(result.Value!.DashboardStats);
}
```

This sends:
```
POST http://localhost:4000/graphql
Authorization: Bearer <token>

{
  "query": "query DashboardStats { dashboardStats { totalProducts ... } }",
  "variables": null
}
```

## Server processing

Apollo Server routes `dashboardStats` to:

```typescript
// dashboard.resolver.ts
Query: { dashboardStats: () => dashboardService.getStats() }
```

Which calls:

```typescript
// dashboard.service.ts
async getStats() {
  const [
    totalProducts,
    totalOrdersToday,
    totalRevenueToday,
    lowStockProducts,
    topSellingProducts,
    recentOrders,
    dailyRevenue,
  ] = await Promise.all([
    dashboardRepository.countActiveProducts(),
    dashboardRepository.countOrdersToday(),
    dashboardRepository.sumRevenueToday(),
    dashboardRepository.findLowStock(5),
    dashboardRepository.findTopSelling(5),
    dashboardRepository.findRecentOrders(3),
    dashboardRepository.findDailyRevenueThisMonth(),
  ]);
  return { totalProducts, totalOrdersToday, ... };
}
```

7 parallel Prisma queries → combined into one response. Total time ≈ slowest query.

## Server response

```json
{
  "data": {
    "dashboardStats": {
      "totalProducts": 125,
      "totalOrdersToday": 3,
      "totalRevenueToday": 89970000,
      "lowStockProducts": [...],
      "topSellingProducts": [...],
      "recentOrders": [...],
      "dailyRevenue": [{"date":"2026-04-01","revenue":0}, ...]
    }
  }
}
```

## Client deserialization

The JSON is auto-deserialized into:

```csharp
private class DashboardStatsResponse
{
    public DashboardStatsDto DashboardStats { get; set; } = new();
}
```

We extract `DashboardStats` and return it.

## DashboardViewModel.ApplyStats

This is the UI mapping. Server data → ObservableCollection<UIType>.

### KPI cards

```csharp
KpiCards.Clear();
KpiCards.Add(new KpiCardItem { Title = "Total Products", Value = stats.TotalProducts.ToString("N0") });
// ... 3 more cards ...
```

### Recent orders table

```csharp
RecentInvoices.Clear();
foreach (var order in stats.RecentOrders)
{
    RecentInvoices.Add(new RecentInvoiceItem
    {
        No = $"#{ShortId(order.OrderId)}",
        Customer = order.CustomerName ?? "(walk-in)",
        Date = FormatDate(order.CreatedAt),
        Status = order.Status,
        Price = FormatCurrency(order.FinalAmount),
    });
}
```

### Sales chart

```csharp
SalesSeries.Clear();
SalesSeries.Add(new LineSeries<double>
{
    Name = "Revenue",
    Values = stats.DailyRevenue.Select(d => d.Revenue).ToArray(),
});
```

`SalesSeries` is bound to LiveCharts2 in XAML. Updating it triggers chart redraw.

## XAML bindings

The page XAML binds directly to these collections:

```xml
<ItemsControl ItemsSource="{x:Bind ViewModel.KpiCards, Mode=OneWay}" />
<ItemsControl ItemsSource="{x:Bind ViewModel.LowStockProducts, Mode=OneWay}" />
<lvc:CartesianChart Series="{x:Bind ViewModel.SalesSeries, Mode=OneWay}" />
```

`Mode=OneWay` because the View only reads (doesn't write back to ViewModel).

## Diagram

```
USER navigates to Dashboard
       │
       ▼
DashboardPage.Loaded fires
       │
       ▼
ViewModel.RefreshAsync()
       │
       │ IsLoading = true
       ▼
DashboardService.GetStatsAsync()
       │
       ▼
GraphQL POST /graphql with DashboardQueries.GetStats
       │
       ▼
Apollo Server → dashboardResolver.Query.dashboardStats
       │
       ▼
dashboardService.getStats() runs 7 Prisma queries in parallel
       │
       ▼
PostgreSQL returns rows
       │
       ▼ (back up the stack)
       │
DashboardStatsDto deserialized
       │
       ▼
ViewModel.ApplyStats(dto)
       │
       │ KpiCards.Clear(); add new ones
       │ RecentInvoices.Clear(); add
       │ TopSoldProducts.Clear(); add
       │ LowStockProducts.Clear(); add
       │ SalesSeries.Clear(); add LineSeries
       │
       │ IsLoading = false
       ▼
XAML automatically rerenders (because ObservableCollection raised CollectionChanged)
       │
       ▼
USER sees real KPIs
```

## Why empty data still works

Before Dev C ships the order feature, the database has zero orders. The dashboard:
- `totalOrdersToday` = 0 (counting zero rows)
- `totalRevenueToday` = 0 (sum of nothing)
- `recentOrders` = `[]`
- `topSellingProducts` = `[]`
- `dailyRevenue` = 30 zero entries (one per day this month)
- `lowStockProducts` = whatever has < 5 stock (probably 0 or a few)

The page renders gracefully — empty tables, flat zero-line chart. No crashes.

Once Dev C's orders ship and real orders exist, the same code populates non-zero data automatically.
