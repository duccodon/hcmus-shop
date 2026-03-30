using hcmus_shop.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views.Dashboard
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            ViewModel = new DashboardViewModel();
            InitializeComponent();
        }

        public DashboardViewModel ViewModel { get; }
    }
}
