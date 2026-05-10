using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Reports;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class ReportsPage : Page
    {
        public ReportsViewModel ViewModel { get; }

        public ReportsPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<ReportsViewModel>();
            DataContext = this;
            Loaded += ReportsPage_Loaded;
        }

        private async void ReportsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }
    }
}
