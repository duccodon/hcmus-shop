using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.Models.DTOs;
using hcmus_shop.ViewModels.Customers;
using hcmus_shop.ViewModels.Orders;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace hcmus_shop.Views
{
    public sealed partial class OrderDetailPage : Page
    {
        public OrdersViewModel ViewModel { get; }

        public OrderDetailPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<OrdersViewModel>();
            DataContext = this;
            ViewModel.RequestInvoicePathAsync = RequestInvoicePathAsync;
            ViewModel.ConfirmDeleteOrderAsync = ShowDeleteConfirmAsync;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string orderId)
            {
                await ViewModel.LoadOrderDetailAsync(orderId);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.RequestInvoicePathAsync = null;
            ViewModel.ConfirmDeleteOrderAsync = null;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(OrdersPage));
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedOrder is not null)
            {
                Frame?.Navigate(typeof(OrderEditorPage), ViewModel.SelectedOrder.OrderId);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.DeleteOrderCommand.ExecuteAsync(null);
            if (!ViewModel.HasError)
            {
                BackButton_Click(sender, e);
            }
        }

        private async Task<string?> RequestInvoicePathAsync(string suggestedFileName)
        {
            if ((Application.Current as App)?.CurrentWindow is not Window window)
            {
                return null;
            }

            var picker = new FileSavePicker();
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
            picker.FileTypeChoices.Add("PDF Document", [".pdf"]);
            picker.SuggestedFileName = suggestedFileName;

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }

        private async Task<bool> ShowDeleteConfirmAsync(OrderDto order)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Order",
                Content = $"Delete order {order.OrderId}?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}
