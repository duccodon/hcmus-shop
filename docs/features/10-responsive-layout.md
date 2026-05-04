# Feature 10 — Responsive Layout Pattern

**Owner**: Dev A (sets the pattern, owns Login/Config/Dashboard/Settings)
**Cross-cutting**: Dev B and Dev C apply same pattern to their pages
**Status**: ✅ Documented + applied to Dev A's pages (commit `e615cf8`)
**Bonus**: +0.50 pts
**Phase**: 4
**Pattern doc**: [../responsive-pattern.md](../responsive-pattern.md)
**Applied to**:
- `hcmus-shop/Views/Pages/Auth/LoginPage.xaml` (1.2*/1* split, collapses to single column)
- `hcmus-shop/Views/Pages/Auth/ConfigPage.xaml` (centered card with MaxWidth)
- `hcmus-shop/Views/Pages/Settings/SettingsPage.xaml` (ScrollViewer + MaxWidth=800)

## Summary

All main pages must adapt to window resize without horizontal scrollbars. Use star-width grids and `VisualStateManager` for narrow/wide breakpoints.

## User-visible behavior

- Resize window from 1920×1080 down to 800×600 → content reflows, no scrolls
- On narrow screens, sidebar collapses to icons-only or hamburger
- On narrow screens, two-column forms (label / input) stack vertically

## The pattern

### 1. Use `*` widths, not fixed pixels
```xml
<!-- BAD -->
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="200" />
    <ColumnDefinition Width="600" />
  </Grid.ColumnDefinitions>
</Grid>

<!-- GOOD -->
<Grid>
  <Grid.ColumnDefinitions>
    <ColumnDefinition Width="*" />
    <ColumnDefinition Width="3*" />
  </Grid.ColumnDefinitions>
</Grid>
```

### 2. Use `VisualStateManager` for breakpoints
```xml
<VisualStateManager.VisualStateGroups>
  <VisualStateGroup>
    <VisualState x:Name="Wide">
      <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="900" />
      </VisualState.StateTriggers>
    </VisualState>
    <VisualState x:Name="Narrow">
      <VisualState.StateTriggers>
        <AdaptiveTrigger MinWindowWidth="0" />
      </VisualState.StateTriggers>
      <VisualState.Setters>
        <Setter Target="LeftPanel.(Grid.Column)" Value="0" />
        <Setter Target="RightPanel.(Grid.Column)" Value="0" />
        <Setter Target="LeftPanel.(Grid.Row)" Value="0" />
        <Setter Target="RightPanel.(Grid.Row)" Value="1" />
      </VisualState.Setters>
    </VisualState>
  </VisualStateGroup>
</VisualStateManager.VisualStateGroups>
```

### 3. Use ScrollViewer at the page level for overflow
```xml
<ScrollViewer VerticalScrollMode="Auto" HorizontalScrollMode="Disabled">
  <Grid>...</Grid>
</ScrollViewer>
```

### 4. Min sizes prevent over-shrinking
```xml
<Grid MinWidth="320" MinHeight="480">
```

## Standard breakpoints

| Width | Layout |
|-------|--------|
| ≥ 1200 | 3-column dashboard, full sidebar |
| 900-1199 | 2-column dashboard, full sidebar |
| 600-899 | Single column, collapsed sidebar (icons) |
| < 600 | Single column, hamburger sidebar |

## Files (Dev A's pages to apply pattern)

- `Views/Pages/Auth/LoginPage.xaml`
- `Views/Pages/Auth/ConfigPage.xaml`
- `Views/Pages/Admin/DashboardPage.xaml`
- `Views/Pages/Settings/SettingsPage.xaml`
- `MainWindow.xaml` (NavigationView display mode)

## Verification

1. Run app at 1920×1080 → all elements visible, no scrollbars
2. Resize to 800×600 → layout adapts, content stacks vertically
3. Resize to 400×300 (very narrow) → still usable, no horizontal scroll
4. NavigationView changes display mode appropriately

## For Dev B and Dev C

When you build Products, Orders, Customers, Reports pages — follow this pattern:
- Use `*` widths for all column definitions
- Add `VisualStateManager` with at least 2 states (Wide ≥ 900, Narrow < 900)
- Wrap content in `ScrollViewer` if it could overflow vertically
- Test by resizing the app window

If unsure, copy the pattern from `Views/Pages/Auth/LoginPage.xaml` (post-Phase 4).
