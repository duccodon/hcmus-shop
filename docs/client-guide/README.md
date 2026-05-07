# Client Guide — How the WinUI App Works

This is a teaching guide written for someone new to WinUI. It walks through how every part of the client app connects: views ↔ viewmodels ↔ services ↔ GraphQL ↔ server.

If you're reading this and you're new to:
- **WinUI 3 / XAML** — start with [01 — Anatomy of a WinUI page](01-anatomy-of-a-page.md)
- **MVVM / DI** — start with [02 — MVVM and Dependency Injection](02-mvvm-and-di.md)
- **The actual app flows** — jump to a specific feature

## Index

| # | Doc | What you learn |
|---|-----|----------------|
| 01 | [Anatomy of a WinUI page](01-anatomy-of-a-page.md) | XAML, code-behind, DataContext, bindings |
| 02 | [MVVM and Dependency Injection](02-mvvm-and-di.md) | ObservableObject, RelayCommand, DI container, why we use them |
| 03 | [App startup flow](03-app-startup-flow.md) | App.xaml.cs → DI setup → OnLaunched → auto-login → window pick |
| 04 | [Login + Auto-login](04-login-flow.md) | Every file/event/method involved in logging in |
| 05 | [ConfigPage](05-config-flow.md) | Pre-login server URL configuration |
| 06 | [GraphQL client layer](06-graphql-client.md) | How a service call becomes an HTTP request |
| 07 | [Dashboard](07-dashboard-flow.md) | Page → VM → service → server → back to UI |
| 08 | [Role-based UI hiding](08-role-based-ui.md) | RoleVisibilityConverter pattern |
| 09 | [How to add a new feature](09-how-to-add-a-feature.md) | Step-by-step recipe |

## File mental model

```
hcmus-shop/                              (the WinUI 3 client project)
├── App.xaml + App.xaml.cs               ← App startup, DI registration
├── MainWindow.xaml + .xaml.cs           ← Sidebar + content frame after login
│
├── Views/                               ← UI (XAML pages)
│   └── Pages/
│       ├── Auth/                        Login, ConfigPage, ForbiddenPage
│       ├── Dashboard/                   DashboardPage + chart components
│       ├── Products/                    Products list, AddProduct
│       └── ...
│
├── ViewModels/                          ← UI logic (no XAML, no UI controls)
│   ├── Auth/                            LoginViewModel, ConfigViewModel
│   ├── Dashboard/                       DashboardViewModel
│   └── Products/                        ProductsViewModel, AddProductViewModel
│
├── Services/                            ← Business logic + I/O
│   ├── Auth/                            AuthService (login/logout)
│   ├── Config/                          ConfigService (server URL)
│   ├── Dashboard/                       DashboardService (GraphQL call)
│   ├── GraphQL/                         GraphQLClientService (HTTP wrapper)
│   ├── Products/                        ProductService
│   ├── Brands/                          BrandService
│   ├── Categories/                      CategoryService
│   └── Series/                          SeriesService
│
├── Contracts/Services/                  ← Interfaces (one per service)
│   ├── IAuthService.cs
│   ├── IConfigService.cs
│   ├── IDashboardService.cs
│   ├── IGraphQLClientService.cs
│   ├── IProductService.cs
│   └── ...
│
├── Models/                              ← Plain data classes
│   ├── DTOs/                            ProductDto, BrandDto, DashboardDto, ...
│   └── Common/                          Result<T> (success-or-failure wrapper)
│
├── GraphQL/Operations/                  ← Constant strings: GraphQL queries
│   ├── AuthQueries.cs                   The `login` mutation, `me` query
│   ├── ProductQueries.cs                products list, create, update, delete
│   └── DashboardQueries.cs              dashboardStats query
│
├── Converters/                          ← Tiny value transformers used in XAML
│   ├── BooleanToVisibilityConverter.cs
│   ├── RoleVisibilityConverter.cs
│   └── ...
│
└── Resources/                           ← XAML styles/themes
    ├── Auth/LoginStyles.xaml
    └── Styles/ButtonStyles.xaml, ...
```

## The 4 layers and how they connect

```
┌──────────────────────────────────┐
│  VIEW (XAML)                      │   ← What the user sees
│  Buttons, TextBoxes, charts       │
└─────────────┬────────────────────┘
              │ binds to (DataContext)
              ▼
┌──────────────────────────────────┐
│  VIEWMODEL                        │   ← UI logic, observable state
│  [ObservableProperty]             │
│  [RelayCommand]                   │
└─────────────┬────────────────────┘
              │ calls
              ▼
┌──────────────────────────────────┐
│  SERVICE (interface + impl)       │   ← Business / I/O logic
│  IAuthService, IDashboardService  │
└─────────────┬────────────────────┘
              │ uses
              ▼
┌──────────────────────────────────┐
│  GRAPHQL CLIENT                   │   ← HTTP transport
│  IGraphQLClientService            │
└─────────────┬────────────────────┘
              │ HTTP POST
              ▼
                  EXPRESS + APOLLO SERVER
```

## Key idea: every layer talks to the layer below it through an INTERFACE

The View doesn't know about HTTP. The ViewModel doesn't know about HTTP either. Only the GraphQLClientService knows how to make HTTP calls. This is why we can swap the data source (e.g. fake it for tests) without changing the UI.
