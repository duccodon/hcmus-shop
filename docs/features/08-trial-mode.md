# Feature 08 — Trial Mode (15-day Lock)

**Owner**: Dev A
**Status**: Planned
**Bonus**: +0.50 pts
**Phase**: 5

## Summary

On first launch, the app records the current date as the trial start. After 15 days, the user must enter an activation code to continue using the app. Hardcoded code for the demo: `HCMUS2026`.

## User-visible behavior

### Days 1-15
Normal usage.

### Day 16+
1. User launches app
2. `TrialExpiredPage` shown instead of LoginPage
3. Message: "Your 15-day trial has expired. Enter activation code to continue."
4. User enters `HCMUS2026` → "Activate" button
5. App unlocks permanently → returns to LoginPage

## Architecture

```
App.xaml.cs OnLaunched()
       │
       ▼
   Read LocalSettings:
     - "trial_start_date" (DateTime)
     - "trial_activated" (bool)
       │
       ▼
   Activated? ─── yes ───► Continue normally
       │ no
       ▼
   Has trial_start_date? ─── no ───► Save now, continue
       │ yes
       ▼
   (today - start) > 15 days? ─── no ───► Continue normally
       │ yes
       ▼
   Show TrialExpiredPage
```

## Files

### New
| File | Purpose |
|------|---------|
| `Views/Pages/Auth/TrialExpiredPage.xaml` | Activation UI |
| `Views/Pages/Auth/TrialExpiredPage.xaml.cs` | Code-behind |
| `ViewModels/Auth/TrialExpiredViewModel.cs` | Code input + Activate command |
| `Contracts/Services/ITrialService.cs` | Interface |
| `Services/Trial/TrialService.cs` | Trial logic |

### Modified
| File | Change |
|------|--------|
| `App.xaml.cs` | Check trial state before showing LoginWindow |

## Implementation

### TrialService
```csharp
public class TrialService : ITrialService
{
    private const string StartDateKey = "trial_start_date";
    private const string ActivatedKey = "trial_activated";
    private const int TrialDays = 15;
    private const string ValidCode = "HCMUS2026";

    public TrialStatus GetStatus()
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        if (settings[ActivatedKey] is true) return TrialStatus.Activated;

        if (settings[StartDateKey] is not string dateStr)
        {
            settings[StartDateKey] = DateTime.UtcNow.ToString("o");
            return TrialStatus.Active;
        }

        var start = DateTime.Parse(dateStr);
        var daysElapsed = (DateTime.UtcNow - start).TotalDays;
        return daysElapsed > TrialDays ? TrialStatus.Expired : TrialStatus.Active;
    }

    public bool Activate(string code)
    {
        if (code != ValidCode) return false;
        ApplicationData.Current.LocalSettings.Values[ActivatedKey] = true;
        return true;
    }
}

public enum TrialStatus { Active, Expired, Activated }
```

### App.xaml.cs flow
```csharp
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    var trial = Ioc.Default.GetRequiredService<ITrialService>();
    if (trial.GetStatus() == TrialStatus.Expired)
    {
        ShowTrialExpiredPage();
        return;
    }
    // ... existing flow
}
```

## Business rules

- Trial start date is recorded on first ever launch (cannot be reset)
- 15 days = 15 × 24 hours from trial start (not calendar days)
- Once activated, never expires again
- Hardcoded valid code `HCMUS2026` for demo purposes
- Production would validate against a server or signed license file

## Edge cases

| Case | Behavior |
|------|----------|
| User changes system clock backward | Doesn't help — start date already saved |
| User changes system clock forward | Triggers expiry early (not exploitable, just annoying for honest users) |
| User clears LocalSettings | Trial resets to day 0 (acceptable for demo) |
| Activation code typed wrong | Show error: "Invalid code" |

## Verification

1. Fresh install → app works normally
2. Manually edit LocalSettings: set `trial_start_date` to 16 days ago
3. Restart app → TrialExpiredPage shown
4. Enter wrong code `WRONG` → error
5. Enter `HCMUS2026` → app unlocks → LoginPage shown
6. Restart app → LoginPage shown directly (activation persisted)

## Extension points

- Server-side license validation
- Different tiers (trial / pro / enterprise)
- Bound to hardware (machine ID hash)
- Online verification with grace period
