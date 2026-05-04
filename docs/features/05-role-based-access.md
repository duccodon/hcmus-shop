# Feature 05 — Role-based Access Control

**Owner**: Dev A
**Status**: Planned
**Bonus**: +0.50 pts
**Phase**: 2

## Summary

The server enforces what each user role can see. Currently the only role-sensitive field is `Product.importPrice` — Admin sees the cost price, Sale only sees the selling price. The client also hides UI elements when the user doesn't have access.

## Why this matters

The instructor's bonus requirement says "Admin and Sale must see different data, not just different UI". Without this, hiding fields in XAML doesn't prevent a curious user from running a GraphQL query and getting the import price anyway. Defense in depth: enforce on server, hide on client.

## User-visible behavior

| Role | Can see |
|------|---------|
| Admin | Selling price + Import price + Profit margin |
| Sale | Selling price only |

On the Products page:
- Admin sees both price columns
- Sale only sees the "Price" column (selling price)

## Architecture

```
                        GraphQL Mutation/Query
                                │
                                ▼
               ┌────────────────────────────────┐
               │  context.user (from JWT)        │
               │  { userId, username, role }     │
               └────────────────────────────────┘
                                │
                                ▼
               ┌────────────────────────────────┐
               │  Product.importPrice resolver   │
               │  if (user.role !== "Admin")     │
               │     return null                 │
               │  else                           │
               │     return Number(parent.price) │
               └────────────────────────────────┘
                                │
                                ▼
                        Client receives null or number
                                │
                                ▼
               ┌────────────────────────────────┐
               │  ProductDto.ImportPrice = null │
               │  XAML uses converter to hide UI │
               └────────────────────────────────┘
```

## Files

### Backend
| File | Change |
|------|--------|
| `hcmus-shop-server/src/features/product/product.resolver.ts` | Modify `importPrice` field resolver |

### Client
| File | Change |
|------|--------|
| `hcmus-shop/Views/Pages/Products/ProductsPage.xaml` | Bind import price column visibility to a converter |
| `hcmus-shop/Converters/RoleVisibilityConverter.cs` | New converter checking IAuthService |

## Data flow

1. User logs in → JWT includes `role` field (already set in `auth.service.ts`)
2. Each GraphQL request carries the JWT (set by `GraphQLClientService.SetAuthToken`)
3. Apollo middleware parses JWT into `context.user`
4. When client requests `product { importPrice }`:
   - Resolver runs: checks `context.user?.role === "Admin"`
   - If Admin: return `Number(parent.importPrice)`
   - If not: return `null`
5. Client receives the field as `null` for Sale users
6. XAML column visibility binds to the user's role via DI service

## Implementation

### Backend resolver
```typescript
// product.resolver.ts
Product: {
  importPrice: (
    parent: { importPrice: Prisma.Decimal },
    _: unknown,
    context: Context
  ) => {
    if (context.user?.role !== "Admin") return null;
    return Number(parent.importPrice);
  },
  // ... other field resolvers
}
```

### Client converter
```csharp
public class RoleVisibilityConverter : IValueConverter
{
    public string RequiredRole { get; set; } = "Admin";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var auth = Ioc.Default.GetRequiredService<IAuthService>();
        return auth.HasRole(RequiredRole) ? Visibility.Visible : Visibility.Collapsed;
    }
    // ...
}
```

### XAML usage
```xml
<DataGridTextColumn
    Header="Import Price"
    Binding="{Binding ImportPrice}"
    Visibility="{Binding Converter={StaticResource AdminOnlyVisibility}}"
/>
```

## Business rules

- Admin role string is `"Admin"` (case-sensitive in the DB, case-insensitive in `HasRole` check)
- Sale role string is `"Sale"`
- Any unknown role is treated as "no access"
- The `importPrice` field is the ONLY currently-protected field. Future fields (cost analysis, margin) follow the same pattern.

## Edge cases

| Case | Behavior |
|------|----------|
| User logs in as Sale, then changes role in DB while logged in | Old JWT still has old role until re-login. Acceptable. |
| Token expired | All fields return null (because `context.user` is null) — but the request also fails on auth. |
| Admin's session sees null importPrice somehow | Should never happen — investigate JWT payload |

## Verification

### Backend
```graphql
# As Admin (with admin token)
query { product(productId: 1) { importPrice sellingPrice } }
# Expect: { "importPrice": 28000000, "sellingPrice": 32990000 }

# As Sale (with sale token)
query { product(productId: 1) { importPrice sellingPrice } }
# Expect: { "importPrice": null, "sellingPrice": 32990000 }
```

### Client
1. Log in as `admin/admin123` → Products page shows two price columns
2. Logout → log in as `sale/sale123` → only one price column visible
3. Inspect network tab in dev tools (or use Apollo Playground) — confirm Sale never receives importPrice value

## Extension points

To add a new role-protected field:
1. Add a field resolver in the relevant feature resolver
2. Check `context.user?.role` and return null/sanitized value
3. On client, use `RoleVisibilityConverter` to hide UI
4. Document the new restriction here
