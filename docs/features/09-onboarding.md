# Feature 09 — Onboarding Tutorial

**Owner**: Dev A
**Status**: Planned
**Bonus**: +0.50 pts
**Phase**: 5

## Summary

First-time user sees a step-by-step tutorial overlay using WinUI's `TeachingTip` controls, walking through the main features. Can be skipped or "don't show again".

## User-visible behavior

After **first** successful login:
1. TeachingTip appears on Dashboard nav item: "This is your home — overview of your shop"
2. User clicks "Next" → tip on Products: "Browse and manage products here"
3. Next → Orders: "Create new sales here"
4. Next → Settings: "Customize the app from here"
5. Final tip: "You're all set! Enjoy your shop." → Done

Subsequent logins: no overlay (unless user disabled "Don't show again" toggle in Settings).

## Architecture

```
LocalSettings:
  "onboarding_completed" → bool (default false)

MainWindow.xaml.cs after first login:
  if (!_onboarding.IsCompleted)
      ShowOnboardingTips()
```

## Files

### New
| File | Purpose |
|------|---------|
| `Contracts/Services/IOnboardingService.cs` | Interface |
| `Services/Onboarding/OnboardingService.cs` | Reads/writes flag |

### Modified
| File | Change |
|------|--------|
| `MainWindow.xaml` | Add `<TeachingTip>` controls |
| `MainWindow.xaml.cs` | Sequence them on first login |
| `ViewModels/Settings/SettingsViewModel.cs` | "Reset tutorial" button (extension) |

## Implementation outline

```xml
<!-- MainWindow.xaml -->
<TeachingTip x:Name="DashboardTip"
             Title="Welcome to HCMUS Shop"
             Subtitle="This is your dashboard — get an overview of your shop here."
             ActionButtonContent="Next"
             ActionButtonClick="OnDashboardTipNext" />

<TeachingTip x:Name="ProductsTip"
             Title="Products"
             Subtitle="Browse and manage your products here."
             ActionButtonContent="Next"
             ActionButtonClick="OnProductsTipNext" />
<!-- ... -->
```

```csharp
// MainWindow.xaml.cs
private void StartOnboarding()
{
    if (_onboarding.IsCompleted) return;
    DashboardTip.Target = DashboardItem;
    DashboardTip.IsOpen = true;
}

private void OnDashboardTipNext(TeachingTip sender, object args)
{
    DashboardTip.IsOpen = false;
    ProductsTip.Target = ProductsItem;
    ProductsTip.IsOpen = true;
}
// ... etc
```

## Business rules

- Onboarding shows ONCE per machine — flag in LocalSettings
- User can dismiss any time with the close (×) button
- "Don't show again" button on first tip immediately marks completed

## Edge cases

| Case | Behavior |
|------|----------|
| User logs out mid-onboarding | Flag NOT marked completed → restarts on next login |
| User disabled a feature flag mid-tour | Skip that step |

## Verification

1. Clear LocalSettings → fresh install state
2. Login → first tip appears
3. Click Next several times → tips advance
4. Final tip → app continues normally
5. Logout → login again → no tips shown
6. Manually clear `onboarding_completed` in LocalSettings → tips show again

## Extension points

- Make tour content data-driven (JSON)
- Add interactive elements (e.g. "Click here to add a product")
- Different tours for different roles (Admin vs Sale)
- Reset tour from Settings page
