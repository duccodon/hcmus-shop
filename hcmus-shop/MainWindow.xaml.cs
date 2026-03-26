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

        public MainWindow()
        {
            InitializeComponent();
            _authService = Ioc.Default.GetRequiredService<IAuthService>();
            ConfigureNavigationByRole();
            NavigateTo("Dashboard");
            AppNavigationView.SelectedItem = DashboardItem;
        }

        private void ConfigureNavigationByRole()
        {
            var isAdmin = _authService.HasRole("Admin");
            AdminItem.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
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
                    ContentFrame.Navigate(typeof(DashboardPage));
                    break;
                case "Sales":
                    ContentFrame.Navigate(typeof(SalesPage));
                    break;
                case "Admin":
                    if (_authService.HasRole("Admin"))
                    {
                        ContentFrame.Navigate(typeof(AdminPage));
                    }
                    break;
            }
        }
    }
}
