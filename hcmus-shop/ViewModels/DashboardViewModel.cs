using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private string welcomeMessage = "Welcome back!";
    }
}
