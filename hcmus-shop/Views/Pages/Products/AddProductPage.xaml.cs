using hcmus_shop.Models;
using hcmus_shop.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace hcmus_shop.Views
{
    public sealed partial class AddProductPage : Page, INotifyPropertyChanged
    {
        private BitmapImage? _mainPreviewBitmap;

        public Product DraftProduct { get; } = new()
        {
            Sku = string.Empty,
            Name = "Puffer Jacket With Pocket Detail",
            Description = "Cropped puffer jacket made of technical fabric. High neck and long sleeves. Flap pocket at the chest and in-seam side pockets at the hip. Inside pocket detail. Hem with elastic interior. Zip-up front.",
            Specifications = "{\n  \"Material\": \"Technical Fabric\",\n  \"Fit\": \"Regular\",\n  \"Closure\": \"Zip-up front\"\n}",
            WarrantyMonths = 12,
            ImportPrice = 35,
            SellingPrice = 47,
            IsActive = true,
            BrandId = 1,
        };

        // UI staging only — no ProductImage here
        public ObservableCollection<ImagePreview> PreviewImages { get; } = [];

        public ObservableCollection<string> AvailableCategories { get; } =
        [
            "Jacket",
            "Accessories",
            "Clothes",
            "Shoes",
            "Electronics",
            "Beauty",
        ];

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddProductPage()
        {
            InitializeComponent();
        }

        public BitmapImage? MainPreviewBitmap
        {
            get => _mainPreviewBitmap;
            private set
            {
                _mainPreviewBitmap = value;
                OnPropertyChanged(nameof(MainPreviewBitmap));
                OnPropertyChanged(nameof(MainImageVisibility));
                OnPropertyChanged(nameof(EmptyPreviewVisibility));
            }
        }

        public Visibility MainImageVisibility =>
            _mainPreviewBitmap is not null ? Visibility.Visible : Visibility.Collapsed;

        public Visibility EmptyPreviewVisibility =>
            _mainPreviewBitmap is null ? Visibility.Visible : Visibility.Collapsed;

        public double ImportPriceValue
        {
            get => decimal.ToDouble(DraftProduct.ImportPrice);
            set
            {
                DraftProduct.ImportPrice = Convert.ToDecimal(value);
                OnPropertyChanged(nameof(ImportPriceValue));
            }
        }

        public double SellingPriceValue
        {
            get => decimal.ToDouble(DraftProduct.SellingPrice);
            set
            {
                DraftProduct.SellingPrice = Convert.ToDecimal(value);
                OnPropertyChanged(nameof(SellingPriceValue));
            }
        }

        public double WarrantyMonthsValue
        {
            get => DraftProduct.WarrantyMonths;
            set
            {
                DraftProduct.WarrantyMonths = Math.Max(0, Convert.ToInt32(value));
                OnPropertyChanged(nameof(WarrantyMonthsValue));
            }
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
            if (window is null) return;

            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null) return;

            var bitmap = new BitmapImage();
            using (var stream = await file.OpenReadAsync())
            {
                await bitmap.SetSourceAsync(stream);
            }

            var preview = new ImagePreview
            {
                Bitmap = bitmap,
                File = file,
                DisplayOrder = PreviewImages.Count,
            };

            PreviewImages.Add(preview);

            if (_mainPreviewBitmap is null)
                MainPreviewBitmap = bitmap;
        }

        private void ThumbnailButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: ImagePreview preview }) return;
            MainPreviewBitmap = preview.Bitmap;
        }

        private async Task SaveProductAsync()
        {
            Console.WriteLine("save product");
            // 1. Upload each image to S3, then build real ProductImage for DB
            //foreach (var preview in PreviewImages)
            //{
            //    using var stream = await preview.File.OpenReadAsync();
            //    var key = $"products/{DraftProduct.Sku}/{preview.File.Name}";

            //    await s3Client.PutObjectAsync(new PutObjectRequest
            //    {
            //        BucketName = "your-bucket",
            //        Key = key,
            //        InputStream = stream.AsStream(),
            //        ContentType = preview.File.ContentType,
            //    });

            //    // Only now do we create the real ProductImage
            //    DraftProduct.Images.Add(new ProductImage
            //    {
            //        ImageUrl = $"https://your-bucket.s3.amazonaws.com/{key}",
            //        DisplayOrder = preview.DisplayOrder,
            //    });
            //}

            // 2. Save DraftProduct (with Images) to DB here
        }

        private void CategoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = CategoryListView.SelectedItems.OfType<string>().ToList();
            DraftProduct.Categories.Clear();
            foreach (var categoryName in selected)
            {
                DraftProduct.Categories.Add(new Category { Name = categoryName });
            }
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}