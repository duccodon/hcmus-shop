# Responsive Layout Pattern

For consistent behavior on different window sizes.

## The 4 rules

1. **No fixed widths on root containers**. Use `*` (star) widths and `MaxWidth` instead.
2. **Wrap pages in `ScrollViewer`** with `VerticalScrollMode="Auto"`, `HorizontalScrollMode="Disabled"`.
3. **Use VisualStateManager + AdaptiveTrigger** for layout switches at breakpoints.
4. **Set MinWidth on critical UI** so they don't shrink below usability.

## Standard breakpoints

| Width | State | Layout |
|-------|-------|--------|
| ≥ 900 | Wide | Multi-column, full sidebar text |
| < 900 | Narrow | Single column, collapsed sidebar |

## Template

```xml
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" ...>
    <ScrollViewer VerticalScrollMode="Auto" HorizontalScrollMode="Disabled">
        <Grid x:Name="RootGrid" Margin="32" RowSpacing="20" MaxWidth="1200">
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
                            <Setter Target="RootGrid.Margin" Value="16" />
                        </VisualState.Setters>
                    </VisualState>
                </VisualStateGroup>
            </VisualStateManager.VisualStateGroups>

            <!-- content -->
        </Grid>
    </ScrollViewer>
</Page>
```

## Already applied to

- ✅ `LoginPage.xaml` — two-column with collapse to single column
- ✅ `ConfigPage.xaml` — single card with MaxWidth
- ✅ `SettingsPage.xaml` — single column, scrollable

## To apply

When building a new page, copy the template above. Test by:
1. Run the app
2. Resize the window from 1920×1080 down to 800×600 to 400×300
3. No horizontal scrollbars should appear
4. Content should stack/reflow gracefully
