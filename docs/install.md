# HCMUS Shop — Installation & Build Guide

## Run from source (development)

### Prerequisites
- Windows 10 19041+ (or Windows 11)
- Visual Studio 2022 with .NET 10 + Windows App SDK workloads
- Node.js 22+
- Docker Desktop

### 1. Backend

```bash
cd hcmus-shop-server
docker compose up -d        # PostgreSQL on port 5433
npm install
npx prisma migrate deploy
npx ts-node prisma/seed.ts  # creates 125+ products
npm run dev                 # Apollo Sandbox at http://localhost:4000/graphql
```

### 2. Client

Open `hcmus-shop.slnx` in Visual Studio.
Build → Run (F5). Default users:
- `admin` / `admin123` (full access, sees import prices)
- `sale` / `sale123` (limited, no import prices)

## Build a release

### Without obfuscation
```
Visual Studio → Build → Configuration: Release → Platform: x64
Right-click the project → Package and Publish → Create App Packages
Choose: Sideloading (no Microsoft Store)
Sign with: Test certificate (or your own .pfx)
Build for: x64 only (or x64 + ARM64)
```

Output: `bin\Release\net10.0-windows10.0.19041.0\AppPackages\HCMUSShop_1.0.0.0_x64.msix`

### With obfuscation (optional bonus)

1. Install Obfuscar tool:
   ```bash
   dotnet tool install -g Obfuscar.Console
   ```
2. Build the project in Release configuration first (without packaging).
3. From `hcmus-shop/`:
   ```bash
   obfuscar.console obfuscar.xml
   ```
4. The obfuscated DLL is at `bin\Release\net10.0-windows10.0.19041.0\win-x64\Obfuscated\hcmus-shop.dll`.
5. To verify obfuscation: open the DLL in dnSpy / ILSpy. Private classes should have opaque names like `a()`, `b()` while public types (App, MainWindow, ViewModels) keep their names.

## Install on a target machine

### Sideloading
1. Enable sideloading: **Settings → Privacy & security → For developers** → "Install apps from any source"
2. Double-click the `.msix` file → Install
3. Trust the certificate if prompted (the dev cert needs to be installed on the target machine first; ship `.cer` alongside)

### First launch
1. App opens to **ConfigPage** (or LoginPage with default URL)
2. If your server is on a different machine, click Config → set URL to `http://<server-ip>:4000/graphql` → Save
3. Login with seeded credentials

## Production deployment notes

For real production (not the demo):
- Sign the MSIX with a real certificate (not self-signed)
- Bind to a specific server URL via Group Policy or registry
- Use a real activation code system, not the hardcoded `HCMUS2026`
- Set up TLS on the server (HTTPS, not HTTP)
- Use a managed PostgreSQL instance, not local Docker
