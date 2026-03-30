using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Services;
using hcmus_shop.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
            //NavigateTo("Dashboard");  //test forbidden page
            NavigateToDefault();
        }

        private void ConfigureNavigationByFeatureFlag()
        {
            DashboardItem.Visibility = CanAccessFeature("Dashboard") ? Visibility.Visible : Visibility.Collapsed;
            SalesItem.Visibility = CanAccessFeature("Sales") ? Visibility.Visible : Visibility.Collapsed;
            AdminItem.Visibility = CanAccessFeature("Admin") ? Visibility.Visible : Visibility.Collapsed;
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
                        ContentFrame.Navigate(typeof(DashboardPage));
                    }
                    else
                    {
                        ContentFrame.Navigate(typeof(ForbiddenPage));
                    }
                    break;
                case "Sales":
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
                AppNavigationView.SelectedItem = SalesItem;
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
