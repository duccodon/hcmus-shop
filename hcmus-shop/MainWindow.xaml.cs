using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Views;
using DashboardPageView = hcmus_shop.Views.Dashboard.DashboardPage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;


namespace hcmus_shop
{
    /// <summary>
    /// Post-login window. Hosts a NavigationView (sidebar) and a content Frame.
    /// Tracks the user's last opened screen and restores it on next login if
    /// "Open last screen on startup" is enabled in Settings.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly IAuthService _authService;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ISettingsService _settings;
        private readonly IOnboardingService _onboarding;

        public MainWindow()
        {
            InitializeComponent();
            _authService = Ioc.Default.GetRequiredService<IAuthService>();
            _featureFlagService = Ioc.Default.GetRequiredService<IFeatureFlagService>();
            _settings = Ioc.Default.GetRequiredService<ISettingsService>();
            _onboarding = Ioc.Default.GetRequiredService<IOnboardingService>();
            ConfigureNavigationByFeatureFlag();
            NavigateToDefault();
            StartOnboardingIfFirstTime();
        }

        // ---- Onboarding ----

        private void StartOnboardingIfFirstTime()
        {
            if (_onboarding.IsCompleted) return;
            WelcomeTip.IsOpen = true;
        }

        private void OnTipNext(TeachingTip sender, object args)
        {
            sender.IsOpen = false;
            if (sender == WelcomeTip)
            {
                DashboardTip.Target = DashboardItem;
                DashboardTip.IsOpen = true;
            }
            else if (sender == DashboardTip)
            {
                ProductsTip.Target = ProductsItem;
                ProductsTip.IsOpen = true;
            }
            else if (sender == ProductsTip)
            {
                SettingsTip.Target = SettingsItem;
                SettingsTip.IsOpen = true;
            }
        }

        private void OnTipFinish(TeachingTip sender, object args)
        {
            sender.IsOpen = false;
            _onboarding.MarkCompleted();
        }

        private void OnTipSkip(TeachingTip sender, object args)
        {
            sender.IsOpen = false;
            _onboarding.MarkCompleted();
        }

        private void ConfigureNavigationByFeatureFlag()
        {
            foreach (var item in GetNavigationItems())
            {
                if (item.Tag is not string tag)
                {
                    item.Visibility = Visibility.Visible;
                    continue;
                }

                item.Visibility = CanAccessFeature(tag) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void AppNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer?.Tag is string target)
            {
                NavigateTo(target);
            }
        }

        private void NavigateTo(string target)
        {
            switch (target)
            {
                case "Dashboard":
                    NavigateOrForbid(typeof(DashboardPageView), "Dashboard");
                    break;
                case "Sales":
                    NavigateOrForbid(typeof(SalesPage), target);
                    break;
                case "Products":
                    NavigateOrForbid(typeof(ProductsPage), target);
                    break;
                case "Store":
                    NavigateOrForbid(typeof(StorePage), target);
                    break;
                case "Messages":
                    NavigateOrForbid(typeof(MessagesPage), target);
                    break;
                case "Inventory":
                    NavigateOrForbid(typeof(InventoryPage), target);
                    break;
                case "Settings":
                    NavigateOrForbid(typeof(SettingsPage), target);
                    break;
                case "Admin":
                    NavigateOrForbid(typeof(AdminPage), "Admin");
                    break;
                case "Logout":
                    _authService.Logout();
                    if (Application.Current is App app)
                    {
                        app.OpenLoginWindow();
                    }
                    Close();
                    return; // don't track Logout as last screen
            }

            // Track this navigation as the "last screen" for restore on next login.
            if (target != "Logout")
            {
                _settings.LastScreen = target;
            }
        }

        private bool CanAccessFeature(string featureName)
        {
            // "Settings" is always accessible to logged-in users (no role gating).
            if (string.Equals(featureName, "Settings", StringComparison.OrdinalIgnoreCase))
                return _authService.CurrentUser != null;

            return _featureFlagService.IsFeatureEnabledForRole(_authService.CurrentUser?.Role, featureName);
        }

        private void NavigateToDefault()
        {
            // If user enabled "remember last screen" and we have one saved, try it first.
            if (_settings.RememberLastScreen
                && !string.IsNullOrWhiteSpace(_settings.LastScreen)
                && CanAccessFeature(_settings.LastScreen))
            {
                NavigateTo(_settings.LastScreen);
                SelectNavItem(_settings.LastScreen);
                return;
            }

            if (CanAccessFeature("Dashboard"))
            {
                NavigateTo("Dashboard");
                AppNavigationView.SelectedItem = DashboardItem;
                return;
            }

            var firstAccessible = GetNavigationItems()
                .Where(item => item.Tag is string tag && tag is not "Dashboard" and not "Admin")
                .Select(item => new { Item = item, Tag = (string)item.Tag })
                .FirstOrDefault(entry => CanAccessFeature(entry.Tag));

            if (firstAccessible is not null)
            {
                NavigateTo(firstAccessible.Tag);
                AppNavigationView.SelectedItem = firstAccessible.Item;
                return;
            }

            if (CanAccessFeature("Admin"))
            {
                NavigateTo("Admin");
                AppNavigationView.SelectedItem = AdminItem;
                return;
            }

            ContentFrame.Navigate(typeof(ForbiddenPage));
            AppNavigationView.SelectedItem = null;
        }

        private void SelectNavItem(string tag)
        {
            var item = GetNavigationItems().FirstOrDefault(i => (i.Tag as string) == tag);
            if (item != null) AppNavigationView.SelectedItem = item;
        }

        private IEnumerable<NavigationViewItem> GetNavigationItems()
        {
            return AppNavigationView.MenuItems.OfType<NavigationViewItem>();
        }

        private void NavigateOrForbid(Type pageType, string featureName)
        {
            if (CanAccessFeature(featureName))
            {
                ContentFrame.Navigate(pageType);
            }
            else
            {
                ContentFrame.Navigate(typeof(ForbiddenPage));
            }
        }
    }
}
