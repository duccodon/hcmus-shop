using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views.Dashboard
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            // Resolve from DI so the ViewModel gets IDashboardService injected.
            ViewModel = Ioc.Default.GetRequiredService<DashboardViewModel>();
            InitializeComponent();

            // Trigger first load when the page becomes visible.
            Loaded += async (s, e) => await ViewModel.RefreshAsync();
        }
    }
}
