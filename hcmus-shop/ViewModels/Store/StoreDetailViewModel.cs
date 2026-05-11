using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Store
{
    public class StoreDetailViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IConfigService _configService;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private ProductDto? _product;
        private StoreGalleryImageViewModel? _selectedImage;

        public StoreDetailViewModel(IProductService productService, IConfigService configService)
        {
            _productService = productService;
            _configService = configService;
            SelectImageCommand = new RelayCommand<StoreGalleryImageViewModel?>(SelectImage);
        }

        public ObservableCollection<StoreGalleryImageViewModel> Images { get; } = [];
        public ObservableCollection<string> HighlightLines { get; } = [];
        public ObservableCollection<string> CategoryChips { get; } = [];

        public IRelayCommand<StoreGalleryImageViewModel?> SelectImageCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(HasProduct));
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(HasProduct));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool HasProduct => _product is not null && !HasError && !IsLoading;

        public StoreGalleryImageViewModel? SelectedImage
        {
            get => _selectedImage;
            private set
            {
                if (SetProperty(ref _selectedImage, value))
                {
                    OnPropertyChanged(nameof(SelectedImageUri));
                    OnPropertyChanged(nameof(HasSelectedImage));
                }
            }
        }

        public Uri? SelectedImageUri => SelectedImage?.ImageUri;
        public bool HasSelectedImage => SelectedImageUri is not null;

        public string Name => _product?.Name ?? string.Empty;
        public string BrandName => _product?.Brand?.Name ?? string.Empty;
        public string Sku => _product?.Sku ?? string.Empty;
        public string SeriesName => _product?.Series?.Name ?? "Standard lineup";
        public string PriceDisplay => (_product?.SellingPrice ?? 0).ToString("N0", CultureInfo.InvariantCulture) + " VND";
        public string StockDisplay => _product is null ? string.Empty : $"{_product.StockQuantity} units available";
        public string WarrantyDisplay => _product is null ? string.Empty : $"{_product.WarrantyMonths} month warranty";
        public string Description => string.IsNullOrWhiteSpace(_product?.Description) ? "No product description provided." : _product!.Description!;
        public string SpecificationsDisplay => FormatSpecifications(_product?.Specifications);

        public async Task LoadAsync(int productId)
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _productService.GetByIdAsync(productId);
                if (!result.IsSuccess || result.Value is null)
                {
                    _product = null;
                    Images.Clear();
                    HighlightLines.Clear();
                    CategoryChips.Clear();
                    SelectedImage = null;
                    ErrorMessage = result.Error ?? "Failed to load product details.";
                    NotifyProductChanged();
                    return;
                }

                _product = result.Value;
                RebuildCollections(result.Value);
                NotifyProductChanged();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RebuildCollections(ProductDto product)
        {
            Images.Clear();
            foreach (var image in product.Images.OrderBy(item => item.DisplayOrder))
            {
                var imageUri = NormalizeImageUri(image.ImageUrl);
                if (imageUri is null)
                {
                    continue;
                }

                Images.Add(new StoreGalleryImageViewModel(image.ImageId, imageUri));
            }

            SelectedImage = Images.FirstOrDefault();

            HighlightLines.Clear();
            foreach (var line in BuildHighlights(product))
            {
                HighlightLines.Add(line);
            }

            CategoryChips.Clear();
            foreach (var category in product.Categories.Select(item => item.Name))
            {
                CategoryChips.Add(category);
            }
        }

        private void SelectImage(StoreGalleryImageViewModel? image)
        {
            if (image is null)
            {
                return;
            }

            SelectedImage = image;
        }

        private Uri? NormalizeImageUri(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri;
            }

            if (!Uri.TryCreate(_configService.GetServerUrl(), UriKind.Absolute, out var graphQlUri))
            {
                return null;
            }

            var baseOrigin = new Uri(graphQlUri.GetLeftPart(UriPartial.Authority));
            var normalizedPath = imageUrl.StartsWith("/", StringComparison.Ordinal) ? imageUrl : $"/{imageUrl}";

            return Uri.TryCreate(baseOrigin, normalizedPath, out var resolvedUri)
                ? resolvedUri
                : null;
        }

        private void NotifyProductChanged()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(BrandName));
            OnPropertyChanged(nameof(Sku));
            OnPropertyChanged(nameof(SeriesName));
            OnPropertyChanged(nameof(PriceDisplay));
            OnPropertyChanged(nameof(StockDisplay));
            OnPropertyChanged(nameof(WarrantyDisplay));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(SpecificationsDisplay));
            OnPropertyChanged(nameof(HasProduct));
        }

        private static IReadOnlyList<string> BuildHighlights(ProductDto product)
        {
            var lines = new List<string>();

            if (product.Specifications is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in new[] { "cpu", "processor", "gpu", "graphics", "ram", "memory", "display", "screen", "storage" })
                {
                    if (!TryGetPropertyIgnoreCase(jsonElement, key, out var property))
                    {
                        continue;
                    }

                    var value = property.ToString();
                    if (!string.IsNullOrWhiteSpace(value) && !lines.Contains(value, StringComparer.OrdinalIgnoreCase))
                    {
                        lines.Add(value);
                    }
                }
            }

            if (lines.Count == 0)
            {
                lines.Add(product.Brand?.Name ?? "Laptop");
                lines.Add($"{product.WarrantyMonths} month warranty");
            }

            return [.. lines.Take(5)];
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement property)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }

            property = default;
            return false;
        }

        private static string FormatSpecifications(object? specifications)
        {
            if (specifications is null)
            {
                return "No technical specifications provided.";
            }

            if (specifications is JsonElement jsonElement)
            {
                return jsonElement.ToString();
            }

            return specifications.ToString() ?? "No technical specifications provided.";
        }
    }

    public class StoreGalleryImageViewModel
    {
        public StoreGalleryImageViewModel(int imageId, Uri imageUri)
        {
            ImageId = imageId;
            ImageUri = imageUri;
        }

        public int ImageId { get; }
        public Uri ImageUri { get; }
    }
}
