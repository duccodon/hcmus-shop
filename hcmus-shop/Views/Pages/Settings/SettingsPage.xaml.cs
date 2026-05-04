using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            ViewModel = Ioc.Default.GetRequiredService<SettingsViewModel>();
            InitializeComponent();
            DataContext = ViewModel;
        }
    }
}
