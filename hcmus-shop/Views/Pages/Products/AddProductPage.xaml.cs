using CommunityToolkit.Mvvm.DependencyInjection;
using hcmus_shop.ViewModels;
using hcmus_shop.ViewModels.Products;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace hcmus_shop.Views
{
    public sealed partial class AddProductPage : Page
    {
        public AddProductViewModel ViewModel { get; }

        public AddProductPage()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<AddProductViewModel>();
            DataContext = ViewModel;
            Loaded += AddProductPage_Loaded;
            Unloaded += AddProductPage_Unloaded;
            ViewModel.ProductSaved += ViewModel_ProductSaved;
        }

        private async void AddProductPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsInitialized)
            {
                await ViewModel.InitializeCommand.ExecuteAsync(null);
            }
        }

        private void AddProductPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.ProductSaved -= ViewModel_ProductSaved;
            Loaded -= AddProductPage_Loaded;
            Unloaded -= AddProductPage_Unloaded;
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

        private async void AddImageButton_Click(object sender, RoutedEventArgs e)
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

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            var bitmap = new BitmapImage();
            using (var stream = await file.OpenReadAsync())
            {
                await bitmap.SetSourceAsync(stream);
            }

            ViewModel.AddImagePreview(new ImagePreview
            {
                Bitmap = bitmap,
                File = file,
            });
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
    }
}