using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Store;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace hcmus_shop.Views
{
    public sealed partial class StoreDetailPage : Page
    {
        public StoreDetailViewModel ViewModel { get; }

        public StoreDetailPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<StoreDetailViewModel>();
            DataContext = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is int productId && productId > 0)
            {
                await ViewModel.LoadAsync(productId);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
            }
        }
    }
}
