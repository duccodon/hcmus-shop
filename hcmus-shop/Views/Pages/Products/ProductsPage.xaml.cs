using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Products;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace hcmus_shop.Views
{
    public sealed partial class ProductsPage : Page
    {
        public ProductsViewModel ViewModel { get; }

        public ProductsPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<ProductsViewModel>();
            DataContext = ViewModel;

            ViewModel.NavigateToAddProductRequested += ViewModel_NavigateToAddProductRequested;
            ViewModel.NavigateToEditProductRequested += ViewModel_NavigateToEditProductRequested;
            ViewModel.ConfirmBulkDeleteAsync = ShowBulkDeleteConfirmAsync;
            ViewModel.ConfirmRowDeleteAsync = ShowRowDeleteConfirmAsync;
            ViewModel.RequestImportFilePathAsync = RequestImportFilePathAsync;
            ViewModel.RequestExportFilePathAsync = RequestExportFilePathAsync;
            Loaded += ProductsPage_Loaded;
            Unloaded += ProductsPage_Unloaded;
        }

        private async void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void ProductsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ProductsPage_Loaded;
            Unloaded -= ProductsPage_Unloaded;
            ViewModel.NavigateToAddProductRequested -= ViewModel_NavigateToAddProductRequested;
            ViewModel.NavigateToEditProductRequested -= ViewModel_NavigateToEditProductRequested;
            ViewModel.ConfirmBulkDeleteAsync = null;
            ViewModel.ConfirmRowDeleteAsync = null;
            ViewModel.RequestImportFilePathAsync = null;
            ViewModel.RequestExportFilePathAsync = null;
        }

        private void ViewModel_NavigateToAddProductRequested(object? sender, EventArgs e)
        {
            Frame?.Navigate(typeof(AddProductPage));
        }

        private void ViewModel_NavigateToEditProductRequested(int productId)
        {
            Frame?.Navigate(typeof(EditProductPage), productId);
        }

        private async Task<bool> ShowBulkDeleteConfirmAsync(int selectedCount)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Products",
                Content = $"Delete {selectedCount} selected products? This will deactivate them.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async Task<bool> ShowRowDeleteConfirmAsync(int productId)
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Product",
                Content = $"Delete product ID {productId}? This will deactivate it.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private async Task<string?> RequestImportFilePathAsync()
        {
            if ((Application.Current as App)?.CurrentWindow is not Window window)
            {
                return null;
            }

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xlsm");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }

        private async Task<string?> RequestExportFilePathAsync()
        {
            if ((Application.Current as App)?.CurrentWindow is not Window window)
            {
                return null;
            }

            var picker = new FileSavePicker
            {
                SuggestedFileName = $"products-{DateTime.Now:yyyyMMdd-HHmmss}"
            };
            picker.FileTypeChoices.Add("CSV file", new List<string> { ".csv" });
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

            StorageFile? file = await picker.PickSaveFileAsync();
            return file?.Path;
        }
    }
}
