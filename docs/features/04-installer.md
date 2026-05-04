# Feature 04 — MSIX Installer

**Owner**: Dev A
**Status**: Planned
**Base**: B7 = 0.25 pts
**Phase**: 7 (last — needs all features stable)

## Summary

Package the WinUI 3 app as an `.msix` installer that users can double-click to install. Modern Windows app distribution format.

## User-visible behavior

1. User downloads `HCMUSShop.msix`
2. Double-click → Windows installer prompts to install
3. App appears in Start Menu under "HCMUS Shop"
4. Launches from Start Menu like a normal Windows app
5. Connects to the GraphQL server (configurable via ConfigScreen on first launch)

## Files / config

| File | Purpose |
|------|---------|
| `hcmus-shop/Package.appxmanifest` | App identity, capabilities, logos |
| `hcmus-shop/Assets/` | Logo files in multiple resolutions |
| `hcmus-shop/Properties/PublishProfiles/` | Publish profile (added by VS) |

## Steps

1. **Configure manifest**
   - DisplayName: "HCMUS Shop"
   - PublisherDisplayName: "HCMUS Team"
   - Description: "POS system for laptop store"
   - Version: 1.0.0.0
   - Capabilities: `runFullTrust`
2. **Generate logos** (Visual Studio asset generator)
3. **Right-click project → Package and Publish → Create App Packages**
4. **Choose**: Sideloading (no Microsoft Store)
5. **Generate self-signed cert** for testing (for production, would use real cert)
6. **Build for x64** (or x64 + ARM64 + x86)
7. **Output**: `HCMUSShop_1.0.0.0_x64.msix`

## Verification

- Build succeeds with no errors
- Install on a clean Windows VM → app appears in Start Menu
- Launch app → connects to server, login works
- Uninstall via Settings → removes cleanly

## Extension points

- Sign with real certificate for production
- Auto-update via MSIX bundle delivery
- Add to Microsoft Store
- Bundle the server too (for fully local-only deployment)
