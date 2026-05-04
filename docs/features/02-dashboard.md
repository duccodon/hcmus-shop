# Feature 02 — Dashboard

**Owner**: Dev A
**Status**: ✅ Implemented (commit `4cd976d`)
**Base**: B2 = 0.50 pts
**Phase**: 3
**Files**:
- Backend: `hcmus-shop-server/src/features/dashboard/{typeDef.graphql, repository.ts, service.ts, resolver.ts}`
- Client: `hcmus-shop/Services/Dashboard/DashboardService.cs`, `Models/DTOs/DashboardDto.cs`,
  `GraphQL/Operations/DashboardQueries.cs`, `ViewModels/Dashboard/DashboardViewModel.cs`,
  `Views/Pages/Dashboard/DashboardPage.xaml.cs`
- Flow trace: see [FLOWS.md](../FLOWS.md) Flow 7

## Summary

Single GraphQL query that returns all KPIs the shop owner cares about, rendered as cards, tables, and charts on the Dashboard page.

## User-visible behavior

When the user logs in (or navigates to Dashboard), they see:
- **3 KPI cards**: total products, orders today, revenue today
- **Low stock table**: top 5 products with `stockQuantity < 5`
- **Top selling table**: top 5 products by units sold
- **Recent orders table**: 3 most recent orders with status badge
- **Revenue line chart**: daily revenue for current month (LiveCharts2)

## Architecture

```
DashboardPage.xaml (UI)
       │ binds to
       ▼
DashboardViewModel
       │ calls
       ▼
IDashboardService.GetStatsAsync()
       │ uses
       ▼
GraphQLClientService.QueryAsync<DashboardStatsResponse>(GraphQL/Operations/DashboardQueries.GetStats)
       │
       ▼ HTTP POST /graphql
       ▼
Apollo Server
       │
       ▼
dashboardResolver.Query.dashboardStats
       │ uses
       ▼
dashboardService.getStats()
       │ uses
       ▼
dashboardRepository.getXxx() — multiple Prisma queries in parallel
       │
       ▼
PostgreSQL
```

## Files

### Backend (new)
| File | Purpose |
|------|---------|
| `src/features/dashboard/dashboard.typeDef.graphql` | DashboardStats, KpiSnapshot, DailyRevenuePoint types + query |
| `src/features/dashboard/dashboard.resolver.ts` | Thin resolver |
| `src/features/dashboard/dashboard.service.ts` | Orchestrates parallel queries |
| `src/features/dashboard/dashboard.repository.ts` | Prisma queries |

### Backend (modified)
| File | Change |
|------|--------|
| `src/index.ts` | Load dashboard typeDef + resolver |

### Client (new)
| File | Purpose |
|------|---------|
| `Contracts/Services/IDashboardService.cs` | Interface |
| `Services/Dashboard/DashboardService.cs` | Implementation |
| `Models/DTOs/DashboardDto.cs` | Top-level DTO + nested types |
| `GraphQL/Operations/DashboardQueries.cs` | Query string |

### Client (modified)
| File | Change |
|------|--------|
| `ViewModels/Dashboard/DashboardViewModel.cs` | Replace mock data with service call |
| `Views/Pages/Admin/DashboardPage.xaml` | Bind to real properties |
| `App.xaml.cs` | Register IDashboardService |

## GraphQL schema

```graphql
type DashboardStats {
  totalProducts: Int!
  totalOrdersToday: Int!
  totalRevenueToday: Float!
  lowStockProducts: [LowStockProduct!]!
  topSellingProducts: [TopSellingProduct!]!
  recentOrders: [RecentOrder!]!
  dailyRevenue: [DailyRevenuePoint!]!
}

type LowStockProduct {
  productId: Int!
  name: String!
  stockQuantity: Int!
}

type TopSellingProduct {
  productId: Int!
  name: String!
  totalSold: Int!
}

type RecentOrder {
  orderId: ID!
  customerName: String
  finalAmount: Float!
  status: String!
  createdAt: String!
}

type DailyRevenuePoint {
  date: String!
  revenue: Float!
}

extend type Query {
  dashboardStats: DashboardStats!
}
```

## Business rules

- **Today** = local server timezone, midnight to 23:59:59
- **Low stock threshold**: `stockQuantity < 5` (configurable later if needed)
- **Top selling**: by sum of `OrderItem.quantity` where parent Order.status = "Paid"
- **Recent orders**: latest 3 by `createdAt`, regardless of status
- **Daily revenue**: sum of `Order.finalAmount` where status = "Paid", grouped by day, for current month
- If no orders exist: returns empty arrays + zero counts (Dashboard renders gracefully)

## Edge cases

| Case | Behavior |
|------|----------|
| No orders in DB | Zero KPIs, empty tables, empty chart |
| No products | totalProducts = 0, empty low-stock + top-sold tables |
| Server unreachable | DashboardViewModel shows error banner |
| Query timeout | Same — error banner |

## Verification

```graphql
query {
  dashboardStats {
    totalProducts
    totalOrdersToday
    totalRevenueToday
    lowStockProducts { productId name stockQuantity }
    topSellingProducts { productId name totalSold }
    recentOrders { orderId customerName finalAmount status createdAt }
    dailyRevenue { date revenue }
  }
}
```

Expected on a freshly seeded DB: ~125 products, 0 orders today, empty arrays except `lowStockProducts` (whatever has < 5 stock).

## Extension points

- Add date range picker (currently fixed to "today" + "current month")
- Add per-category breakdown
- Cache stats for 30 seconds to reduce DB load on rapid Dashboard visits
- Add WebSocket subscription for real-time stat updates
