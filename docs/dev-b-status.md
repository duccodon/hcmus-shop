# Dev B Status - Products & Promotions

**Last checked:** 2026-05-11
**Owner:** Dev B / Buu
**Scope:** Products CRUD, promotions, product advanced search, multi-sort, product draft auto-save, image upload integration, Dev B backend tests.

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
- Pagination reads `ISettingsService.PageSize`, normalizes invalid values to `10`, persists changes back to Settings, and uses the shared `5/10/15/20` options.
- Admin-only product management controls:
  - Add Product
  - Edit Product
  - Delete Product
  - bulk status/delete
  - Excel import
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
- `ProductsViewModel.cs`: also guards management commands with `IAuthService.HasRole("Admin")`.
- Products page errors are formatted through `UserErrorMessageFormatter` and shown above the table instead of overlaying rows.
- `AddProductViewModel.cs`: add form, image upload, category creation, draft auto-save.
- `EditProductViewModel.cs`: edit form, image upload, delete.
- Add/Edit Product save/category errors also use the shared formatter.
- `AddProductPage.xaml.cs` and `EditProductPage.xaml.cs`: redirect non-admin users to `ForbiddenPage`.
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
- Add/edit promotion dialog validates inline and stays open for:
  - missing code
  - start date after end date
  - both discount percent and amount supplied
  - neither discount percent nor amount supplied
  - discount percent over 100
- Promotion page backend errors are formatted through `UserErrorMessageFormatter` and shown above the table instead of overlaying rows.
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

## Dev B Backend Tests

Files:

```text
hcmus-shop-server/tests/product.service.test.ts
hcmus-shop-server/tests/promotion.service.test.ts
```

Implemented:

- Product service tests:
  - rejects `minPrice > maxPrice`
  - rejects unsupported sort field
  - rejects unsupported sort direction
  - accepts valid multi-sort input
- Promotion service tests:
  - percent discount calculation
  - fixed discount calculation
  - discount capped at subtotal
  - rejects both percent and amount
  - rejects neither percent nor amount
  - validates Bronze/Silver/Gold/Diamond rank order

The tests mock repositories and do not require a live PostgreSQL database.

---

## Known Gaps / Follow-Up

### Manual UI Verification Still Needed

Code review and builds/tests are green, but Dev B still needs a manual demo pass:

- Products CRUD.
- Product search/filter/advanced search/multi-sort.
- Products pagination using Settings page size.
- Product image upload and minimum 3-image validation.
- Add Product draft restore/discard.
- Excel import/export.
- Admin versus Sale access on product management actions.
- Promotions list/search/pagination.
- Promotion add/edit/deactivate.
- Promotion inline validation dialog.
- Order promotion code apply flow.

### Local Database Must Match Prisma Schema

If the Promotions page shows this runtime error:

```text
The column `promotions.minimumCustomerRank` does not exist in the current database.
```

then code is ahead of the local database. Apply migrations or reset the local dev database according to the team workflow:

```powershell
cd hcmus-shop-server
npx prisma generate
npx prisma migrate dev
```

Do not fix this by changing code unless Prisma schema and migrations are wrong.

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
npm install
npx prisma generate
npx tsc --noEmit
```

Result:

- Passed.

```powershell
cd hcmus-shop-server
npm test -- --runInBand
```

Result:

- Passed: 5 suites, 22 tests.

---

## Quick Resume Checklist For Future Agent

1. Run `git status --short --branch`.
2. Run `cd hcmus-shop-server && npm install` if `node_modules` is missing.
3. Run `npx prisma generate`.
4. Run `npx tsc --noEmit`.
5. Run `npm test -- --runInBand`.
6. Build client with `dotnet build hcmus-shop.slnx`.
7. If Promotions fails with a missing `minimumCustomerRank` column, apply the Prisma migration to the local database.
8. Manually verify Products, Promotions, and order promotion apply flow in the WinUI app.
9. Capture screenshots/test output for the final report or presentation.
