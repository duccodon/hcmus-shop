# HCMUS Shop — 3 Week Feature Implementation Roadmap

**Timeline**: 2026-04-11 → 2026-05-03 (feature implementation, 3 weeks)
**Bug fix buffer**: 2026-05-03 → 2026-05-13 (10 days)
**Team**: 3 developers (Buu, Duc, Dung)
**Submission format**: Recorded demo video (no live presentation)
**Goal**: Max out the 10-point scale (5.0 base + 5.0 bonus cap) with a buffer of safety features

---

## How we work: full-stack feature ownership

Each developer **owns their features end-to-end** — from Prisma schema and GraphQL resolver on the backend all the way to the XAML and ViewModel on the client. This keeps integration simple and gives each dev clear accountability.

**Shared foundation (already done):**

- Backend: Express + Apollo + Prisma with auth, brand/category/series/product CRUD
- Client: GraphQL client wrapper, DI, auth flow, login page
- Database: PostgreSQL in Docker
- Docs: ERD, feature specs, this roadmap

## Scoring strategy: aim for the 5.0 bonus cap

The max bonus is **5.0 points**. We're already at **2.0** (GraphQL, MVVM, DI). We need **at least 3.0 more** to cap out. We'll aim to implement **~6.5 more** so we have a buffer if some features fail or don't meet the quality bar.

### Base features (5.0 total)

| ID  | Feature              | Points |
| --- | -------------------- | ------ |
| B1  | Login + ConfigScreen | 0.25   |
| B2  | Dashboard            | 0.50   |
| B3  | Products CRUD        | 1.25   |
| B4  | Orders CRUD          | 1.50   |
| B5  | Reports              | 1.00   |
| B6  | Settings             | 0.25   |
| B7  | Installer            | 0.25   |

### Bonus features (target)

| Feature                                           | Points    | Owner   | Target week |
| ------------------------------------------------- | --------- | ------- | ----------- |
| GraphQL API instead of REST                       | +1.00     | ✅ done | —           |
| MVVM architecture                                 | +0.50     | ✅ done | —           |
| Dependency Injection                              | +0.50     | ✅ done | —           |
| **Promotions**                                    | **+1.00** | Dev B   | Week 2      |
| **Role-based access (Admin/Sale data filtering)** | **+0.50** | Dev A   | Week 1      |
| **Multi-criteria sort with direction**            | **+0.50** | Dev B   | Week 2      |
| **Auto-save on create (product)**                 | **+0.25** | Dev B   | Week 2      |
| **Auto-save on create (order)**                   | **+0.25** | Dev C   | Week 2      |
| **Backup/Restore DB**                             | **+0.25** | Dev A   | Week 3      |
| **Obfuscator**                                    | **+0.25** | Dev A   | Week 3      |
| **Print order to PDF**                            | **+0.50** | Dev C   | Week 3      |
| **Loyal customer management + tier promos**       | **+0.50** | Dev C   | Week 3      |
| **Sale KPI / commission tracking**                | **+0.25** | Dev C   | Week 3      |
| **Responsive layout**                             | **+0.50** | Dev A (pattern) + all | Week 2 |
| **Onboarding (first-time tutorial)**              | **+0.50** | Dev A   | Week 3      |
| **Advanced search (multi-condition)**             | **+1.00** | Dev B   | Week 3      |
| **Trial mode (15-day lock)**                      | **+0.50** | Dev A   | Week 3      |
| **Test cases (unit tests)**                       | **+0.50** | Shared (each dev) | Week 3 |

**Bonus points targeted**: 9.0 (will cap at 5.0, so 4.0 safety buffer)
**Target total**: 10.0 / 10

---

## Feature ownership (full-stack)

### Shared responsibilities (every dev handles their own code)
- **Responsive layout** (+0.5) — Dev A sets the patterns (Grid star widths, VisualStateManager), then each dev ensures their own pages respond to window resize
- **Unit tests** (+0.5) — each dev writes tests for their own backend business logic (~3-4 tests per dev)

### Dev A (Dung) — Foundation, Dashboard & System bonuses — ~3.75 points

| Feature                                 | Points |
| --------------------------------------- | ------ |
| B1: Login + ConfigScreen polish         | 0.25   |
| B2: Dashboard                           | 0.50   |
| B6: Settings                            | 0.25   |
| B7: MSIX Installer                      | 0.25   |
| Data seeding + image upload endpoint    | —      |
| Role-based access (backend enforcement) | +0.50  |
| Backup/Restore DB                       | +0.25  |
| Obfuscator (build step)                 | +0.25  |
| Trial mode (15-day lock)                | +0.50  |
| Onboarding flow                         | +0.50  |
| Responsive layout pattern + own pages   | +0.50  |

### Dev B (Buu) — Products & Promotions — ~4.00 points

| Feature                                       | Points |
| --------------------------------------------- | ------ |
| B3: Products CRUD                             | 1.25   |
| Promotions CRUD + apply to orders             | +1.00  |
| Multi-criteria sort (Products)                | +0.50  |
| Advanced search (multi-condition in Products) | +1.00  |
| Auto-save when creating product               | +0.25  |

### Dev C (Duc) — Orders, Reports & Customers — ~4.00 points

| Feature                                                     | Points |
| ----------------------------------------------------------- | ------ |
| B4: Orders CRUD (hardest single feature — serials, sync)    | 1.50   |
| B5: Reports (charts, date ranges)                           | 1.00   |
| Customer management (CRUD + loyalty points + tier promos)   | +0.50  |
| Print order to PDF                                          | +0.50  |
| Sale KPI / commission tracking                              | +0.25  |
| Auto-save when creating order                               | +0.25  |

**Note**: Customer management belongs with Dev C because orders depend on customers. Dev C owns the customer picker, creation flow, and loyalty points (which update when orders are Paid). Dev B still owns the "tier promotions" logic (promotions that require minimum loyalty points) since that's a promotion feature — but Dev C provides the customer.loyaltyPoints field for validation.

---

## What we DO NOT do (explicitly skipped)

- Excel import (use manual seeding instead)
- Multi-theme support
- ML/LLM integration (too much effort)
- UI automated testing (+1.0 — too much setup)
- Plugin architecture (+1.0 — architectural complexity not worth it)

---

## WEEK 1 (Apr 11–17) — Core CRUD + Role-based Access

**Goal**: Full Products CRUD end-to-end, Orders scaffolded, role-based access live, data seeded.

### Shared kickoff (Apr 11)

- [ ] All devs: pull latest, clone to Windows filesystem
- [ ] All devs: `docker compose up -d`, `npm install`, `npm run dev` → verify Apollo Sandbox at `http://localhost:4000/graphql`
- [ ] All devs: open solution in Visual Studio, build, log in as admin/admin123
- [ ] Read together: `docs/erd.md`, `docs/feature-specs.md`, this roadmap
- [ ] Agree on branching: `feat/<dev>-<feature>` (e.g. `feat/buu-products-list`)

### Dev A — Foundation + Role-based access

**Backend:**

- [ ] Create `uploads/` folder, add REST endpoint `POST /uploads` using multer
- [ ] Add `app.use('/uploads', express.static('uploads'))` for serving
- [ ] Write realistic seed script: 5 categories, 25 products per category, 3 images per product, 5 instances per product with serial numbers
  - Real laptop names (ASUS ROG Strix G16, Dell XPS 13 Plus, etc.)
  - Realistic VND prices (15M–80M)
  - JSON specs (CPU, RAM, GPU, Screen)
- [ ] **Role-based access in resolvers**:
  - `Product.importPrice` resolver: return `null` if `context.user.role !== "Admin"`
  - In the future, `orders` query: Sale sees only their own orders (filter by `userId`)
  - Document the rule in `docs/rbac.md`
- [ ] Add `query dashboardStats` returning: totalProducts, totalOrdersToday, totalRevenueToday, lowStockProducts, topSellingProducts, recentOrders, dailyRevenue

**Client:**

- [ ] `ConfigPage.xaml` + code-behind: Server URL input, Save button, Test Connection button
- [ ] Wire `OpenConfigCommand` in LoginViewModel to navigate to ConfigPage
- [ ] Implement auto-login: on app startup call `TryAutoLoginAsync`
- [ ] Stub `SettingsPage.xaml` and `DashboardPage.xaml`
- [ ] In ProductsPage, hide the import price column for Sale role (using `_authService.HasRole("Admin")`)

**Verification:**

- `POST /uploads` with an image returns a URL
- Database has 125+ products with images and serial numbers
- Log in as Sale → Products page does NOT show import price column
- Log in as Admin → Products page shows both prices
- Query `product { importPrice }` as Sale in Playground → returns `null`
- Same query as Admin → returns the number
- ConfigScreen works, auto-login works

### Dev B — Products feature (complete)

**Backend:**

- [ ] Verify existing brand/category/series/product resolvers work
- [ ] Ensure product mutations handle image URLs from upload endpoint

**Client:**

- [ ] Refactor `ProductsViewModel` — remove mock data, fetch real via `IProductService`
- [ ] Implement:
  - Pagination (uses Settings page size)
  - Search by name (debounced input)
  - Category filter dropdown
  - Brand filter dropdown
  - Sort by name/price/stock (asc/desc)
- [ ] `ProductDetailPage.xaml`: view + edit existing product
- [ ] `AddProductPage` full wiring:
  - File picker for images → upload to `/uploads` → get URLs
  - Category multi-select
  - Brand → series cascading dropdown
  - Form validation
- [ ] Delete confirmation dialog

**Verification:**

- Products page loads real data with thumbnails
- Search, filter, sort, pagination all work
- Add product with 3 uploaded images → appears in list
- Edit, save, delete all work
- Import price hidden for Sale role (wired via Dev A's role check)

### Dev C — Orders + Customer modules (backend + list UI)

**Backend:**

- [ ] Create `customer` feature module: CRUD for customers with `loyaltyPoints` field
- [ ] Create `order` feature module (resolver, service, repository, typeDef, dto)
- [ ] Queries:
  - `orders(status, fromDate, toDate, page, pageSize)`
  - `order(orderId)`
  - `customers()` and `customer(customerId)`
- [ ] Mutations:
  - `createOrder(input: CreateOrderInput)` — customerId + list of instanceIds with unit prices
  - `updateOrderStatus(orderId, status)` — with inventory sync
  - `deleteOrder(orderId)` — only if status = Created
  - `createCustomer(name, phone, email)` / `updateCustomer` / `deleteCustomer`
- [ ] Inventory sync on status → Paid:
  - Mark ProductInstances as "Sold"
  - Decrement Product.stockQuantity
  - Create InventoryLog entries
  - Award loyalty points to customer (1% of finalAmount)

**Client:**

- [ ] `IOrderService`, `OrderService`, OrderDtos
- [ ] `OrdersPage.xaml`: list with pagination, date range filter, status badge column
- [ ] `OrderDetailPage.xaml`: read-only view of order items with serial numbers, status change buttons (Mark as Paid / Cancel)

**Verification:**

- Create an order via Playground → appears in OrdersPage list
- Click order → detail view shows items with serials
- Mark as Paid from UI → inventory updates (verify in Products page)
- Cannot delete a Paid order

### Week 1 demo (Apr 17)

- Login + Config + auto-login
- Products CRUD complete (add, edit, delete, search, filter, sort, pagination)
- Role-based price hiding works (Admin vs Sale)
- Orders list + detail + status transitions
- 125+ products seeded

---

## WEEK 2 (Apr 18–24) — Order creation flow + Dashboard + Promotions + Small bonuses

**Goal**: Full order creation from client UI. Dashboard live. Promotions working end-to-end. Multi-sort and auto-save added.

### Dev A — Dashboard + Settings + Responsive layout

**Backend:**

- [ ] Finalize dashboardStats resolver (optimize N+1, add dailyRevenue grouped by day)

**Client:**

- [ ] `IDashboardService`, `DashboardService`, DashboardDto
- [ ] `DashboardPage.xaml` with:
  - KPI cards: total products, orders today, revenue today
  - Low stock table (top 5)
  - Recent orders table (top 3)
  - Revenue line chart (current month) via LiveCharts2
- [ ] `SettingsPage.xaml`:
  - Page size dropdown (5/10/15/20) → LocalSettings
  - "Open last screen on startup" toggle
  - Track last opened screen in MainWindow, save on nav
- [ ] **Responsive layout bonus**: Set up the pattern and make own pages responsive
  - Use Grid with star widths (`*`) instead of fixed widths
  - Add VisualStateManager for narrow/wide states where needed
  - Document the pattern in `docs/responsive-guide.md`
  - Dev A does their own pages (Login, Dashboard, Settings); Dev B and Dev C follow the same pattern for their pages
  - Test by resizing the window — no horizontal scrollbars, content reflows

**Verification:**

- Dashboard shows real KPIs
- Resize window from 1920x1080 down to 800x600 → everything still usable
- Settings page size change → Products/Orders pages use it
- "Remember last screen" works across logout/login

### Dev B — Promotions + Multi-sort + Advanced search

**Backend:**

- [ ] Promotion feature module: CRUD + `validatePromotion(code)` returning status
- [ ] Integrate promotion into `createOrder` mutation — apply discount
- [ ] **Advanced search** for products: accept multiple conditions (name + sku + priceRange + categoryIds + brandIds + inStockOnly) — extend existing filter
- [ ] **Multi-criteria sort**: accept array of `{field, direction}` instead of single

**Client:**

- [ ] `IPromotionService`, `PromotionService`
- [ ] `PromotionsPage.xaml` (under Admin nav): list, create, edit, delete, status badge
- [ ] Promotion code input in CreateOrderPage (coordinate with Dev C)
- [ ] Update ProductsPage:
  - **Advanced search panel**: expandable filter with multiple conditions
  - **Multi-sort UI**: chip-style sort criteria, drag to reorder, each with direction toggle
- [ ] **Auto-save when creating product**: every 5 seconds save draft to LocalSettings, restore on reopen

**Verification:**

- Promotion 10% off applied → finalAmount correct
- Expired promotion → rejected with clear message
- Products page: search with name + category + price range + in-stock → filters applied
- Multi-sort: "name ASC, price DESC" → list ordered correctly
- Start filling out new product form, close app, reopen → form restored

### Dev C — Full order creation + Auto-save + Sale KPI

**Backend:**

- [ ] Add `customers` query for customer picker
- [ ] **Sale KPI**: `query salesKpi(userId, fromDate, toDate)` — return totalSales, totalRevenue, commission (e.g. 2% of revenue)

**Client:**

- [ ] `CreateOrderPage.xaml` with 3 steps:
  1. Customer step — dropdown of existing customers, or inline create
  2. Items step — search products → see available serial numbers → click to add to cart
  3. Review step — cart, promo code input, total, confirm button
- [ ] Running total updates as items added
- [ ] **Auto-save order draft**: every 5 seconds save to LocalSettings, restore on reopen
- [ ] OrderDetailPage: Mark as Paid / Cancel buttons with confirmation
- [ ] Under Dashboard or Admin, add **KPI widget**: shows current user's sales count + revenue + commission for this month

**Verification:**

- Full order creation flow from UI works
- Mark as Paid → product stock decreases, serial status = "Sold"
- Start creating an order, don't finish, close app → reopen → draft restored
- KPI widget shows correct numbers

### Week 2 demo (Apr 24)

- Full order creation UI
- Dashboard live with real data
- Promotions working end-to-end
- Multi-sort and advanced search in Products
- Auto-save for product and order forms
- Responsive layout throughout

---

## WEEK 3 (Apr 25 – May 3) — Reports + Bonus sprint + Installer

**Goal**: All remaining features done. Installer built. Demo data polished. Record demo video.

### Dev A — Installer + Backup + Obfuscator + Trial + Onboarding

**Backend:**

- [ ] Backup endpoint `POST /backup` — runs `pg_dump` → streams SQL file
- [ ] Restore endpoint `POST /restore` — accepts SQL file → runs `psql`
- [ ] Set up test runner (Jest or Node's built-in `node:test`)
- [ ] Write own unit tests (~3-4 tests): auth login flow, role-based price filtering, feature flag logic

**Client:**

- [ ] **Backup UI** in SettingsPage:
  - Download Backup button → calls `/backup` → saves .sql file
  - Restore button → file picker → upload
- [ ] **Trial mode (15-day lock)**:
  - On first launch, save `trial_start_date` to LocalSettings
  - On every launch, check if > 15 days have passed
  - If expired, show a screen requiring an activation code
  - Hardcode one valid code for the demo (e.g. `HCMUS2026`)
- [ ] **Onboarding**: On first login, show a step-by-step tutorial overlay
  - Use `TeachingTip` controls from WinUI
  - Step through: Dashboard → Products → Create Order → Logout
  - Skip button + "Don't show again" checkbox

**Packaging:**

- [ ] Configure `Package.appxmanifest`: app name, logo, description
- [ ] **Obfuscator**: add a post-build step using `ConfuserEx` or `Obfuscar` (open source)
  - Document the step in `docs/build.md`
- [ ] Build MSIX installer via Visual Studio Package and Publish
- [ ] Test installing on a clean machine

**Verification:**

- Backup downloads SQL file, restore works
- First launch shows onboarding overlay
- Changing clock forward 16 days → app shows trial expired screen → enter `HCMUS2026` → works
- Built .msix installer installs on clean machine
- Unit tests run via `npm test` with all passing
- DLL/exe is obfuscated (decompile with dnSpy to verify)

### Dev B — Promotions polish + Tier promotions + bug fixes

**Backend:**

- [ ] Promotions integration with customer loyalty:
  - Add `minLoyaltyPoints` field to Promotion model
  - In `createOrder`, validate that the customer has enough points if the promotion requires it
- [ ] Write unit tests for promotion validation + discount calculation (~3-4 tests)

**Client:**

- [ ] Promotions page: allow setting `minLoyaltyPoints` when creating a promotion
- [ ] Error handling in CreateOrderPage when promotion rejected (expired, not enough points, etc.)
- [ ] Polish: loading states, empty states, error banners on Products and Promotions pages
- [ ] Fix bugs found in bug bash

**Verification:**

- Create a promotion with `minLoyaltyPoints: 100`
- Customer with 50 points → rejected with clear message
- Customer with 150 points → accepted, discount applied
- Unit tests pass

### Dev C — Reports + Customer management + Print to PDF + Sale KPI

**Backend:**

- [ ] Reports resolvers:
  - `query salesReport(fromDate, toDate, groupBy: "day"|"week"|"month")` returning period + quantity + revenue + profit
  - `query topProducts(fromDate, toDate, limit)`
  - `query salesByCategory(fromDate, toDate)`
- [ ] Extend customer feature: `query customerStats(customerId)` returning points, total spent, orders count
- [ ] Sale KPI resolver: `query salesKpi(userId, fromDate, toDate)` — total sales, revenue, commission (2% of revenue)
- [ ] Write unit tests for inventory sync + loyalty points awarding (~3-4 tests)

**Client:**

- [ ] `IReportService`, `ReportService`
- [ ] `ReportPage.xaml`:
  - Date range picker
  - GroupBy dropdown (day/week/month)
  - Line chart: revenue over time
  - Bar chart: top products
  - Pie chart: revenue by category
- [ ] `CustomersPage.xaml` (under Admin): list customers with loyalty points column
- [ ] Customer detail: show loyalty status, total spent, order history
- [ ] On CreateOrderPage, after picking customer, show their loyalty points
- [ ] **Print order to PDF**:
  - On OrderDetailPage, add "Print Invoice" button
  - Use `QuestPDF` NuGet package (free)
  - Simple invoice layout: header, customer info, items table, total
  - Save to user-selected file
- [ ] **KPI widget** on Dashboard (or new page): current user's sales count + revenue + commission this month

**Verification:**

- Reports page with April date range renders all 3 charts
- GroupBy change → charts update
- Create orders for a customer → Paid → loyalty points increase in CustomersPage
- KPI widget shows correct numbers for logged-in user
- Print Invoice generates valid PDF with shop name, order ID, customer, items with serials, total
- Unit tests pass

### All devs — Bug bash + demo recording (Apr 30 – May 3)

- [ ] **Apr 30**: feature freeze, each dev tests the other devs' features for 1h each
- [ ] **May 1**: fix critical bugs from bug bash
- [ ] **May 2**: dry run of full demo script, identify rough edges
- [ ] **May 3**: record the final demo video

### Demo video script (recorded May 3)

1. Intro: team members, project name, tech stack (15 sec)
2. Start backend (docker + npm run dev, show Playground) (20 sec)
3. Launch client app (show installed MSIX) (10 sec)
4. ConfigScreen → explain server URL concept (20 sec)
5. Login as admin (10 sec)
6. Dashboard walkthrough (KPIs, charts, low stock) (30 sec)
7. Products page (search, multi-sort, advanced filter, pagination) (45 sec)
8. Add product with 3 images (30 sec)
9. Promotions page, create a promo with loyalty requirement (30 sec)
10. Create order (select customer, pick serials, apply promo, show total) (60 sec)
11. Mark order as Paid → show inventory update + loyalty points added (30 sec)
12. Print invoice to PDF (15 sec)
13. Reports page — show all 3 charts (30 sec)
14. Settings → change page size → show applied (15 sec)
15. Download backup (10 sec)
16. Logout → log in as Sale → show role-based differences (import price hidden) (30 sec)
17. Onboarding tutorial (show first-login flow) (20 sec)
18. Trial mode explanation (skip actual expiry demo) (10 sec)
19. Closing: obfuscation, unit tests, responsive layout mention (20 sec)

**Target length**: 7-8 minutes

---

## Daily workflow

**Morning standup (10 min, Zalo/Messenger):**

- Yesterday / Today / Blockers

**End of day:**

- Commit + push to feature branch

**Branching:**

- `master` — always working
- `feat/dung-<feature>`, `feat/buu-<feature>`, `feat/duc-<feature>`
- PR + 1 reviewer before merging to master

---

## Definition of "Done" per feature

1. Backend: GraphQL query/mutation works in Playground
2. Client: Calls the backend and displays data
3. Happy path works end-to-end
4. At least one error case handled (empty list, network error)
5. Another dev has tried it and not crashed the app
6. PR merged to master

We are NOT aiming for:

- 100% error coverage
- Comprehensive test suites
- Pixel-perfect UX
- Feature completeness beyond the demo script

---

## Risk mitigation

| Risk                                             | Mitigation                                                      |
| ------------------------------------------------ | --------------------------------------------------------------- |
| WinUI unfamiliarity                              | Copy patterns from existing LoginPage/ProductsPage              |
| GraphQL learning curve                           | Follow existing brand/product modules as templates              |
| Dev A's Dashboard depends on Dev C's Orders      | Dev A mocks order data until Dev C's basic order creation works |
| Dev C's Reports depend on real orders            | Dev C seeds fake orders for testing                             |
| Integration bugs from parallel work              | Friday integration session each week                            |
| Too many bonuses, can't finish all               | Bonus cap is 5.0, we target 9.0 so we have 4.0 buffer           |
| Obfuscator breaks the build                      | Start early in Week 3, keep unobfuscated build as fallback      |
| Trial mode accidentally locks us out during demo | Set trial_start_date manually in LocalSettings for demo         |

---

## If we fall behind, cut in this order

Since we target 9.0 bonus (only 5.0 counts), we can afford to drop several:

1. Onboarding (-0.5)
2. Trial mode (-0.5)
3. Unit tests (-0.5)
4. Sale KPI (-0.25)
5. Obfuscator (-0.25)
6. Backup/Restore (-0.25)
7. Print to PDF (-0.5)
8. Advanced search → basic search only (-1.0)
9. Multi-sort → single sort (-0.5)

**Never cut** (these are the score floor):

- Login, Dashboard, Products, Orders, Reports, Settings, Installer
- GraphQL, MVVM, DI (already done)
- Role-based access (instructor values this)
- Promotions (1.0 — too valuable)

Cutting all optional bonuses still leaves us at: 5.0 base + 2.0 done + 1.0 promotions + 0.5 role = **8.5**, well above passing.

---

## Final milestones

| Date           | Milestone                                                                         |
| -------------- | --------------------------------------------------------------------------------- |
| Apr 11 (Fri)   | Kickoff — everyone running locally                                                |
| Apr 17 (Fri)   | Week 1 demo — Products CRUD, Orders scaffolded, Role-based works                  |
| Apr 24 (Fri)   | Week 2 demo — Orders UI, Dashboard, Promotions, Multi-sort, Auto-save, Responsive |
| Apr 30 (Thu)   | Feature freeze, bug bash starts                                                   |
| May 2 (Sat)    | Dry run demo recording                                                            |
| May 3 (Sun)    | Record & submit final demo video                                                  |
| May 3 → May 13 | Post-submission buffer (fix any last-minute issues if resubmission allowed)       |
