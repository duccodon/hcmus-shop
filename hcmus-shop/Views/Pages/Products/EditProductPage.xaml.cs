using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels.Products;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace hcmus_shop.Views
{
    public sealed partial class EditProductPage : Page
    {
        public EditProductViewModel ViewModel { get; }

        public EditProductPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<EditProductViewModel>();
            DataContext = ViewModel;

            Unloaded += EditProductPage_Unloaded;

            ViewModel.ProductSaved += ViewModel_ProductSaved;
            ViewModel.ProductDeleted += ViewModel_ProductDeleted;
            ViewModel.DeleteConfirmationRequested += ConfirmDeleteAsync;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is int productId)
            {
                await ViewModel.InitializeAsync(productId);
            }
        }

        private void EditProductPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= EditProductPage_Unloaded;

            ViewModel.ProductSaved -= ViewModel_ProductSaved;
            ViewModel.ProductDeleted -= ViewModel_ProductDeleted;
            ViewModel.DeleteConfirmationRequested -= ConfirmDeleteAsync;
        }

        private async Task<bool> ConfirmDeleteAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "Delete Product",
                Content = "This will deactivate the product. Do you want to continue?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }

        private void ViewModel_ProductSaved(object? sender, EventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(ProductsPage));
        }

        private void ViewModel_ProductDeleted(object? sender, EventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(ProductsPage));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame?.CanGoBack == true)
            {
                Frame.GoBack();
                return;
            }

            Frame?.Navigate(typeof(ProductsPage));
        }

        private async void PickLocalImagesButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".webp");
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            var window = (Application.Current as App)?.CurrentWindow;
            if (window is null)
            {
                return;
            }

            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

            var files = await picker.PickMultipleFilesAsync();
            if (files is null || files.Count == 0)
            {
                return;
            }

            foreach (StorageFile file in files)
            {
                ViewModel.AddPendingImageFile(file);
            }
        }

        private void RemovePendingImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }

            if (button.Tag is PendingImageFileViewModel pending)
            {
                ViewModel.RemovePendingImageFile(pending);
            }
        }
    }
}
