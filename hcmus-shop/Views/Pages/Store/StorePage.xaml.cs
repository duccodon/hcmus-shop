using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Store;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace hcmus_shop.Views
{
    public sealed partial class StorePage : Page
    {
        public StoreViewModel ViewModel { get; }

        public StorePage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<StoreViewModel>();
            DataContext = this;
            Loaded += StorePage_Loaded;
            Unloaded += StorePage_Unloaded;
        }

        private async void StorePage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void StorePage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= StorePage_Loaded;
            Unloaded -= StorePage_Unloaded;
        }
    }
}
