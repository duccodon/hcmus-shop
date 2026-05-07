# UI Theming Rules

## Rule 1: app forces Light theme

`App.xaml` has `RequestedTheme="Light"` on the `Application` element.

**This means**: every WinUI control (ComboBox, ToggleSwitch, Button, TextBox, PasswordBox, etc.) renders with its Light theme brushes — regardless of the user's Windows system theme.

**Why we lock to Light**:
- The app's visual identity is light cards (cream/white) on a Mica backdrop
- Without this lock, controls auto-pick Dark theme brushes when the user's system is in Dark mode → invisible/illegible UI on our light cards
- Locking to Light gives us consistent control rendering across all machines

**How it propagates**: WinUI cascades `RequestedTheme` through the visual tree. Setting it on `Application` is the highest level — every Page, Window, and control under it inherits Light unless explicitly overridden.

## Rule 2: explicit Foreground on TextBlocks

WinUI default `TextBlock` foreground inherits from the theme. Even with Light theme locked, **always set Foreground explicitly** on TextBlocks to guarantee the right color:

```xml
<TextBlock Foreground="{StaticResource TextPrimary}" Text="Section title" />
<TextBlock Foreground="{StaticResource TextSecondary}" Text="Helper text" />
```

Why explicit? Two reasons:
1. Defensive — survives theme overrides further up the tree
2. Self-documenting — readers see the intent without tracing inheritance

## Rule 3: don't fight WinUI hover/pressed states

WinUI's Light theme has well-designed hover/pressed states for all standard controls. After **Rule 1**, the default hover behaviour is fine for our cream cards.

If you ever encounter a specific control that still misbehaves on a particular page:
1. **First** — check if the page has a `<Page.Resources>` block accidentally overriding system brushes. Remove it.
2. **Only if necessary** — add a page-scoped override using the documented WinUI brush keys (e.g. `ButtonBackgroundPointerOver`).

Don't override globally in `App.xaml` unless the issue affects every page.

## Rule 4: brand colors are separate from theme

Our brand colors (brown accent, login backgrounds) are defined in `Resources/Styles/ThemeResources.xaml` as `SolidColorBrush` resources:

```xml
<SolidColorBrush x:Key="LoginPrimaryBrush" Color="#7B4F2E" />
<SolidColorBrush x:Key="CardBackground" Color="#FFFBF8" />
<SolidColorBrush x:Key="TextPrimary" Color="#1F1F1F" />
<SolidColorBrush x:Key="TextSecondary" Color="#5F5A56" />
```

These are **independent of the WinUI theme system** — they're our own custom brushes. We use them via `{StaticResource ...}` everywhere.

The AccentButton brand color is overridden via `AccentButtonBackground*` keys in `LoginStyles.xaml`. That's a special case because AccentButton has a fixed system style.

## Rule 5: when in doubt, test on a colleague's machine with system Dark mode

The "invisible on hover" bug usually only manifests when the developer's system theme differs from the assumed one. Before merging UI work:
1. Toggle Windows Settings → Personalization → Colors → "Choose your default app mode" → Dark
2. Run the app
3. If anything looks broken, the page is fighting with the theme — fix it.

Now that **Rule 1** is in place, this should rarely matter — but it's still a good check before final demos.

## Quick checklist when adding a new page

- [ ] No `<Page.Resources>` block overriding system brushes (unless really necessary, scoped, and documented)
- [ ] `Background` set explicitly (`DashboardBackground` for content pages, `LoginRightPanelBrush` for auth)
- [ ] All `<TextBlock>` have `Foreground="{StaticResource TextPrimary|TextSecondary}"`
- [ ] Buttons use one of: `AccentButtonStyle` (primary), `LoginSecondaryButtonStyle` (outline), default `Button` (secondary action)
- [ ] Test by hovering every interactive element — text and icons must remain readable

## Files involved

- `App.xaml` — `RequestedTheme="Light"`
- `Resources/Styles/ThemeResources.xaml` — brand color tokens
- `Resources/Styles/ButtonStyles.xaml` — button variants
- `Resources/Auth/LoginStyles.xaml` — login-specific styles + AccentButton brand colors
