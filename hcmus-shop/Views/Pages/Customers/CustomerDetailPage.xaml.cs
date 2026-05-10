using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Customers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.Views
{
    public sealed partial class CustomerDetailPage : Page
    {
        public CustomerDetailViewModel ViewModel { get; }

        public CustomerDetailPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<CustomerDetailViewModel>();
            DataContext = this;
            ViewModel.GoBackRequested = NavigateBack;
            ViewModel.ConfirmDeleteAsync = ConfirmDeleteAsync;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string customerId)
            {
                await ViewModel.LoadAsync(customerId);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.GoBackRequested = null;
            ViewModel.ConfirmDeleteAsync = null;
        }

        private void NavigateBack()
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(CustomersPage));
        }

        private async Task<bool> ConfirmDeleteAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Customer",
                Content = $"Delete customer {ViewModel.Name}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
