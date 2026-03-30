using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Services.Auth;
using hcmus_shop.Views;
using DashboardPageView = hcmus_shop.Views.Dashboard.DashboardPage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace hcmus_shop
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private static readonly Dictionary<string, string> TagToFeatureMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = "Dashboard",
            ["Sales"] = "Sales",
            ["Products"] = "Sales",
            ["Store"] = "Sales",
            ["Messages"] = "Sales",
            ["Inventory"] = "Sales",
            ["Admin"] = "Admin"
        };

        private readonly IAuthService _authService;
        private readonly IFeatureFlagService _featureFlagService;

        public MainWindow()
        {
            InitializeComponent();
            _authService = Ioc.Default.GetRequiredService<IAuthService>();
            _featureFlagService = Ioc.Default.GetRequiredService<IFeatureFlagService>();
            ConfigureNavigationByFeatureFlag();
            NavigateTo("Sales");  //test forbidden page
            //NavigateToDefault();
        }

        private void ConfigureNavigationByFeatureFlag()
        {
            foreach (var item in AppNavigationView.MenuItems.OfType<NavigationViewItem>())
            {
                if (item.Tag is not string tag || !TagToFeatureMap.TryGetValue(tag, out var featureName))
                {
                    item.Visibility = Visibility.Visible;
                    continue;
                }

                item.Visibility = CanAccessFeature(featureName) ? Visibility.Visible : Visibility.Collapsed;
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
                    if (CanAccessFeature("Dashboard"))
                    {
                        ContentFrame.Navigate(typeof(DashboardPageView));
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(ForbiddenPage));
                    }
                    break;
                case "Sales":
                case "Products":
                case "Store":
                case "Messages":
                case "Inventory":
                    if (CanAccessFeature("Sales"))
                    {
                        ContentFrame.Navigate(typeof(SalesPage));
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(ForbiddenPage));
                    }
                    break;
                case "Admin":
                    if (CanAccessFeature("Admin"))
                    {
                        ContentFrame.Navigate(typeof(AdminPage));
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(ForbiddenPage));
                    }
                    break;
                case "Logout":
                    _authService.Logout();
                    var loginWindow = new LoginWindow();
                    loginWindow.Activate();
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

            if (CanAccessFeature("Sales"))
            {
                NavigateTo("Sales");
                AppNavigationView.SelectedItem = ProductsItem;
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
    }
}
