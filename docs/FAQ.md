# FAQ — common questions about Dev A's code

---

## "Why isn't my Dashboard showing data?"

Possible causes:
1. **Server isn't running** → `cd hcmus-shop-server && npm run dev`
2. **Database is empty** → run `npx ts-node prisma/seed.ts`
3. **Server URL wrong** → click Config, set `http://localhost:4000/graphql`, Test Connection
4. **Token expired** → logout and log back in
5. **No orders exist yet** → that's fine; KPIs show zero, charts show flat. Will populate once Dev C ships orders.

To debug: open browser dev tools, check the GraphQL request payload and response. Or use Apollo Sandbox at `http://localhost:4000/graphql` and run the `dashboardStats` query directly.

---

## "Why does the import price column show on Sale's view?"

It shouldn't — but Dev B hasn't added the column yet. The infrastructure (server filter + RoleVisibilityConverter) is ready. When Dev B adds an import-price column to ProductsPage, they need to add:
```xml
Visibility="{Binding Converter={StaticResource RoleVisibility}, ConverterParameter=Admin}"
```

If the column is shown but the value is null/blank for Sale users, that's the server filter working — just hide the column in the UI.

---

## "I added a new ViewModel but it can't be resolved by DI"

You forgot to register it. Open `App.xaml.cs` and add:
```csharp
services.AddTransient<MyNewViewModel>();
```

Restart the app. If it still fails, check that the constructor takes only registered service interfaces.

---

## "ConfigPage opens but the saved URL doesn't take effect after Save"

The URL is saved to LocalSettings AND the singleton `IGraphQLClientService` is updated. So the current session uses the new URL right away. **But existing connections might be cached** — if you have weird issues, restart the app.

---

## "TeachingTip doesn't appear at all"

Two possible reasons:
1. **Already completed**: clear `onboarding_completed` from LocalSettings to retrigger
2. **TeachingTip Target was null**: check that `WelcomeTip` (which has no Target, uses center placement) opens. If subsequent tips don't appear, their Target binding may be null because `DashboardItem`/`ProductsItem`/`SettingsItem` weren't yet created when the binding was set.

To clear LocalSettings for testing:
```csharp
ApplicationData.Current.LocalSettings.Values.Remove("onboarding_completed");
```

---

## "Trial expired but I don't want it to. How do I reset?"

Open Run, type `regedit`, navigate to:
```
HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\<your-app-id>\LocalState\
```

Find `trial_start_date` and set it to today, OR delete it.

For demo purposes, just toggle the system clock or hardcode a check in `TrialService.GetStatus()` to always return `Active`.

---

## "Backup endpoint returns 500 with 'pg_dump: command not found'"

`pg_dump` must be in the server's PATH. The Docker container `postgres:16-alpine` has it; Node running on the host does NOT (unless you've installed Postgres client locally).

Workaround: install postgres client tools:
```bash
# Windows: download from https://www.postgresql.org/download/windows/
# Linux: sudo apt install postgresql-client
# WSL: sudo apt install postgresql-client
```

Or: have the backup endpoint exec into the docker container:
```typescript
spawn("docker", ["exec", "hcmus-shop-db", "pg_dump", databaseUrl])
```

---

## "Why are properties capitalized differently in C# (PascalCase) and JSON (camelCase)?"

Convention. C# uses PascalCase for public properties. JSON typically uses camelCase. Our `GraphQLClientService` configures `JsonNamingPolicy.CamelCase`, which auto-converts:
- C# `BrandId` → JSON `brandId` (when serializing/deserializing)

Don't manually rename properties; let the policy handle it.

---

## "I want to add a new role (e.g., Manager). How?"

1. **Server**:
   - Update the seed to create a Manager user (or update via SQL)
   - In any role-protected resolver (e.g. `Product.importPrice`), add Manager to the allowed list:
     ```ts
     if (!["Admin", "Manager"].includes(context.user?.role ?? "")) return null;
     ```
2. **Client**:
   - In `appsettings.json`, add Manager to the appropriate FeatureFlag groups
   - The `IFeatureFlagService` reads it automatically; nav items show/hide accordingly

---

## "How do I change the default page size from 10 to something else?"

Change the default in `Services/Settings/SettingsService.cs`:
```csharp
private const int DefaultPageSize = 10;  // change this
```

Existing users will keep their saved preference. New users get the new default.

---

## "Tests pass locally but fail in CI"

1. Make sure `tsconfig.test.json` is checked in
2. Make sure `jest.config.js` is checked in (it points at `tsconfig.test.json`)
3. CI must `npm install` first to get `jest`, `ts-jest`, `@types/jest`

If a test imports from `src/` and the import fails, check the relative path: tests are in `tests/` so they import like `import { foo } from "../src/..."`.

---

## "How do I add a new GraphQL query or mutation?"

Read [`client-guide/09-how-to-add-a-feature.md`](client-guide/09-how-to-add-a-feature.md). The recipe covers backend + client + DI registration.

---

## "The XAML preview/designer in Visual Studio shows errors but the app still builds"

The XAML designer in Visual Studio is unreliable for WinUI 3. Ignore designer errors as long as the build succeeds and the app runs. Work in code-behind instead of relying on the designer.

---

## "I get 'Cannot return null for non-nullable field' GraphQL error"

You marked a field as required (`String!`) in the typeDef but the resolver returned null. Either:
- Make the field nullable: `String` (no `!`)
- Or fix the resolver to never return null for that field

---

## "Auto-login isn't working"

1. Check that you logged in WITH "Remember me" checked at least once
2. Check the saved token isn't expired (server JWT lifetime is 7 days)
3. Open LocalSettings and look for `auth_token` — if missing, you didn't log in with remember-me
4. Try clearing LocalSettings entirely and logging in fresh
