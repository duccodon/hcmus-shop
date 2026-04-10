using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Services.Auth;
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
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly IAuthService _authService;
        private readonly IFeatureFlagService _featureFlagService;

        public MainWindow()
        {
            InitializeComponent();
            _authService = Ioc.Default.GetRequiredService<IAuthService>();
            _featureFlagService = Ioc.Default.GetRequiredService<IFeatureFlagService>();
            ConfigureNavigationByFeatureFlag();
            //NavigateTo("Sales");  //test forbidden page
            NavigateToDefault();
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
                    break;
            }
        }

        private bool CanAccessFeature(string featureName)
        {
            return _featureFlagService.IsFeatureEnabledForRole(_authService.CurrentUser?.Role, featureName);
        }

        private void NavigateToDefault()
        {
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
