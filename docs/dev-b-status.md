# Dev B Status - Products & Promotions

**Last checked:** 2026-05-11
**Owner:** Dev B / Buu
**Scope:** Products CRUD, promotions, product advanced search, multi-sort, product draft auto-save, image upload integration.

Use this note when a future coding agent re-reads the codebase. It records what Dev B has already implemented and what still needs attention.

---

## What Dev B Has Implemented

### Products Backend

Files:

```text
hcmus-shop-server/src/features/product/
  product.typeDef.graphql
  product.dto.ts
  product.repository.ts
  product.service.ts
  product.resolver.ts
```

Implemented:

- GraphQL product list/detail/create/update/delete.
- Product list filters:
  - keyword search
  - name
  - SKU
  - categoryId
  - brandId
  - categoryIds
  - brandIds
  - minPrice / maxPrice
  - inStockOnly
  - includeInactive
- Multi-sort through `sorts: [ProductSortInput!]`.
- Legacy single sort through `sortBy` / `sortOrder`.
- Create/update product category links and image URLs.
- Soft delete via `isActive = false`.
- Stock quantity synchronization with available `ProductInstance` rows.

Important locations:

- `product.typeDef.graphql`: exposes advanced filter and sort arguments.
- `product.repository.ts`: builds Prisma `where` and `orderBy`.
- `product.service.ts`: validates sort fields/directions and syncs instances.

### Products Client

Files:

```text
hcmus-shop/Contracts/Services/IProductService.cs
hcmus-shop/Services/Products/ProductService.cs
hcmus-shop/Services/Products/Dto/ProductRequest.cs
hcmus-shop/Models/DTOs/ProductDto.cs
hcmus-shop/GraphQL/Operations/ProductQueries.cs
hcmus-shop/ViewModels/Products/
hcmus-shop/Views/Pages/Products/
hcmus-shop/Services/Uploads/FileUploadService.cs
```

Implemented:

- `ProductsPage` uses real GraphQL data through `IProductService`.
- Debounced product search.
- Brand/category filters.
- Advanced search panel:
  - SKU
  - name
  - min/max price
  - in-stock only
  - multi-brand
  - multi-category
- Multi-sort UI:
  - add/remove sort criteria
  - move criteria up/down
  - choose field and direction
- Pagination and page buttons.
- Add Product page:
  - file picker for images
  - uploads through `POST /uploads`
  - minimum 3 product images
  - brand to series cascading dropdown
  - multi-category selection
  - add category dialog
  - form validation
  - auto-save draft every 5 seconds to `ApplicationData.Current.LocalSettings`
  - restore/discard draft
- Edit Product page:
  - loads product detail
  - edit fields, categories, images, active status
  - upload additional images
  - delete confirmation
- CSV export is implemented through `ProductService.ExportCsvAsync`.
- Excel import hooks exist through `IProductImportService` and `ProductImportService`.

Important locations:

- `ProductsViewModel.cs`: list, search, filters, pagination, advanced search, multi-sort, import/export, delete.
- `AddProductViewModel.cs`: add form, image upload, category creation, draft auto-save.
- `EditProductViewModel.cs`: edit form, image upload, delete.
- `FileUploadService.cs`: builds `/uploads` endpoint from the configured GraphQL server URL.

### Promotions Backend

Files:

```text
hcmus-shop-server/src/features/promotion/
  promotion.typeDef.graphql
  promotion.dto.ts
  promotion.repository.ts
  promotion.service.ts
  promotion.resolver.ts
```

Implemented:

- GraphQL promotion list/detail/create/update/delete.
- `validatePromotion(code, customerRank)`.
- Percent discount or fixed amount discount.
- Active/inactive validation.
- Start/end date validation.
- Unique promotion code validation.
- Customer-rank eligibility via `minimumCustomerRank`.
- Soft delete by setting `isActive = false`.

Promotion integration with orders:

- `hcmus-shop-server/src/features/order/order.service.ts`
- `prepareOrderPayload` validates `promotionCode`.
- Discount is applied when creating/updating orders.
- Order stores `promotionId`, `discountAmount`, `subtotal`, and `finalAmount`.

### Promotions Client

Files:

```text
hcmus-shop/Contracts/Services/IPromotionService.cs
hcmus-shop/Services/Promotions/PromotionService.cs
hcmus-shop/Services/Promotions/Dto/
hcmus-shop/Models/DTOs/PromotionDto.cs
hcmus-shop/GraphQL/Operations/PromotionQueries.cs
hcmus-shop/ViewModels/Promotions/
hcmus-shop/Views/Pages/Promotions/
```

Implemented:

- Promotions page with list/search/pagination.
- Add/edit promotion dialog.
- Deactivate promotion confirmation.
- Admin-only create/edit/delete controls through `IAuthService.HasRole("Admin")`.
- Promotion rank options:
  - All ranks
  - Bronze
  - Silver
  - Gold
  - Diamond
- Promotion validation service for order flow.

Order UI integration:

- `hcmus-shop/ViewModels/Orders/OrdersViewModel.cs`
- `hcmus-shop/Views/Pages/Orders/OrdersPage.xaml`
- Order editor has a promotion code input and `Apply Promotion` command.

### App Wiring

Files:

```text
hcmus-shop/App.xaml.cs
hcmus-shop/MainWindow.xaml
hcmus-shop/MainWindow.xaml.cs
hcmus-shop/appsettings.json
hcmus-shop/hcmus-shop.csproj
```

Implemented:

- DI registrations:
  - `IProductService -> ProductService`
  - `IPromotionService -> PromotionService`
  - `IFileUploadService -> FileUploadService`
  - `ProductsViewModel`
  - `AddProductViewModel`
  - `EditProductViewModel`
  - `PromotionsViewModel`
- Navigation items:
  - Products
  - Promotions
- Feature flags include Products and Promotions.
- XAML pages are included in the project file.

---

## Known Gaps / Follow-Up

### Backend TypeScript Currently Fails

Command:

```powershell
cd hcmus-shop-server
npx tsc --noEmit
```

Observed failure:

```text
src/features/promotion/promotion.service.ts:
Property 'minimumCustomerRank' does not exist on type Promotion
```

The Prisma schema does contain:

```prisma
minimumCustomerRank String? @db.VarChar(20)
```

Likely fix:

```powershell
cd hcmus-shop-server
npm install
npx prisma generate
npx tsc --noEmit
```

Also make sure migrations are applied.

### Backend Tests Are Missing For Dev B

Existing tests only cover auth/JWT/role-filter. There are no product sort/filter tests and no promotion validation/discount tests yet.

Suggested test coverage:

- Promotion validates active date range.
- Promotion rejects expired/inactive codes.
- Percent discount calculation.
- Fixed discount calculation.
- Rank-restricted promotion rejects lower-rank customer.
- Product multi-sort rejects unsupported fields.
- Product price range rejects `minPrice > maxPrice`.

### `npm test` Did Not Run In The Checked Workspace

Observed failure:

```text
Cannot find module ... node_modules/jest/bin/jest.js
```

This means dependencies were not installed in `hcmus-shop-server/node_modules` at the time of verification.

Likely fix:

```powershell
cd hcmus-shop-server
npm install
npm test
```

### Products Pagination Does Not Use Settings Page Size

Docs say Dev B should read `ISettingsService.PageSize`.

Current state:

- `ProductsViewModel` hardcodes `_selectedPageSize = 10`.
- `ProductsViewModel.PageSizeOptions` is `[10, 20, 50]`.
- Expected project settings options are `5/10/15/20`.

Future fix:

- Inject `ISettingsService` into `ProductsViewModel`.
- Initialize `_selectedPageSize` from `_settings.PageSize`.
- Change `PageSizeOptions` to `5, 10, 15, 20`, or bind to the same options as Settings.

Promotions pagination is also local/defaulted and does not read Settings.

### Import Price Role-Based Visibility Needs UI Review

Backend already returns `Product.importPrice = null` for non-admin users. Dev A also added `RoleVisibilityConverter`.

Current Products list shows selling price only. Add/Edit product pages expose import price fields. If Sale users can access product add/edit, these fields should be hidden or access should be admin-only.

### Product Image Upload Limitation

`AddProductViewModel` only saves uploaded URLs from local files. Restored draft image paths are local files and are uploaded on save. This is fine for create flow.

`EditProductViewModel` supports existing URLs plus pending file uploads.

### Promotions Rank Model Differs From Original Roadmap Wording

Roadmap mentioned `minLoyaltyPoints`. Current implementation uses `minimumCustomerRank` instead. This matches the customer rank flow currently in code, but mention it in the report/demo as **tier/rank promotion** rather than raw points promotion.

---

## Verification Snapshot

Last verification on 2026-05-11:

```powershell
dotnet build hcmus-shop.slnx
```

Result:

- Client build succeeded.
- Warnings only, mostly MVVM Toolkit AOT warnings and a missing publish profile warning.

```powershell
cd hcmus-shop-server
npx tsc --noEmit
```

Result:

- Failed due to Prisma generated type missing `minimumCustomerRank`.

```powershell
cd hcmus-shop-server
npm test -- --runInBand
```

Result:

- Failed because Jest was missing from `node_modules`.

---

## Quick Resume Checklist For Future Agent

1. Run `git status --short --branch`.
2. Run `cd hcmus-shop-server && npm install`.
3. Run `npx prisma generate`.
4. Run `npx tsc --noEmit`.
5. Run `npm test`.
6. Build client with `dotnet build hcmus-shop.slnx`.
7. Fix Products pagination to use `ISettingsService.PageSize`.
8. Add Dev B backend tests for promotions and product filtering/sorting.
9. Re-check Sale role access on Products/Add/Edit pages.
