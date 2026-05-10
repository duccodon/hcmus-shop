using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Products.Dto;
using hcmus_shop.ViewModels.Products;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Store
{
    public class StoreViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IConfigService _configService;
        private CancellationTokenSource? _searchDebounceCts;
        private bool _isInitialized;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private string _searchQuery = string.Empty;
        private bool _inStockOnly = true;
        private int _currentPage = 1;
        private int _selectedPageSize = 12;
        private int _totalCount;
        private StoreFilterOption? _selectedBrand;
        private StoreFilterOption? _selectedCategory;
        private StoreProductListItemViewModel? _selectedProductCard;
        private StoreProductDetailViewModel? _selectedProduct;

        public StoreViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService,
            IConfigService configService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
            _configService = configService;

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(LoadProductsAsync, () => !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CurrentPage > 1);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CurrentPage < TotalPages);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
        }

        public ObservableCollection<StoreProductListItemViewModel> Products { get; } = [];
        public ObservableCollection<StoreFilterOption> BrandOptions { get; } = [];
        public ObservableCollection<StoreFilterOption> CategoryOptions { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [12, 24, 48];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }

        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                if (SetProperty(ref _isInitialized, value))
                {
                    InitializeCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    InitializeCommand.NotifyCanExecuteChanged();
                    RefreshCommand.NotifyCanExecuteChanged();
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsEmpty));
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
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && Products.Count == 0 && !HasError;
        public bool HasSelectedProduct => SelectedProduct is not null;

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value) && IsInitialized)
                {
                    CurrentPage = 1;
                    DebounceSearch();
                }
            }
        }

        public bool InStockOnly
        {
            get => _inStockOnly;
            set
            {
                if (SetProperty(ref _inStockOnly, value) && IsInitialized)
                {
                    CurrentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public StoreFilterOption? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                if (SetProperty(ref _selectedBrand, value) && IsInitialized)
                {
                    CurrentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public StoreFilterOption? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && IsInitialized)
                {
                    CurrentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public int SelectedPageSize
        {
            get => _selectedPageSize;
            set
            {
                if (SetProperty(ref _selectedPageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    PreviousPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        public string ResultText =>
            _totalCount == 0
                ? "0 products"
                : $"{((_currentPage - 1) * SelectedPageSize) + 1}-{Math.Min(_currentPage * SelectedPageSize, _totalCount)} of {_totalCount}";

        public StoreProductListItemViewModel? SelectedProductCard
        {
            get => _selectedProductCard;
            set
            {
                if (SetProperty(ref _selectedProductCard, value) && value is not null)
                {
                    _ = LoadProductDetailAsync(value.ProductId);
                }
            }
        }

        public StoreProductDetailViewModel? SelectedProduct
        {
            get => _selectedProduct;
            private set
            {
                if (SetProperty(ref _selectedProduct, value))
                {
                    OnPropertyChanged(nameof(HasSelectedProduct));
                    OnPropertyChanged(nameof(SelectedProductName));
                    OnPropertyChanged(nameof(SelectedProductBrandName));
                    OnPropertyChanged(nameof(SelectedProductSku));
                    OnPropertyChanged(nameof(SelectedProductSeriesName));
                    OnPropertyChanged(nameof(SelectedProductCategoriesDisplay));
                    OnPropertyChanged(nameof(SelectedProductWarrantyDisplay));
                    OnPropertyChanged(nameof(SelectedProductSellingPriceDisplay));
                    OnPropertyChanged(nameof(SelectedProductStockDisplay));
                    OnPropertyChanged(nameof(SelectedProductAvailableSerialCount));
                    OnPropertyChanged(nameof(SelectedProductDescription));
                    OnPropertyChanged(nameof(SelectedProductSpecificationsDisplay));
                    OnPropertyChanged(nameof(SelectedProductPrimaryImageUri));
                    OnPropertyChanged(nameof(SelectedProductHasPrimaryImage));
                }
            }
        }

        public string SelectedProductName => SelectedProduct?.Name ?? string.Empty;
        public string SelectedProductBrandName => SelectedProduct?.BrandName ?? string.Empty;
        public string SelectedProductSku => SelectedProduct?.Sku ?? string.Empty;
        public string SelectedProductSeriesName => SelectedProduct?.SeriesName ?? string.Empty;
        public string SelectedProductCategoriesDisplay => SelectedProduct?.CategoriesDisplay ?? string.Empty;
        public string SelectedProductWarrantyDisplay => SelectedProduct?.WarrantyDisplay ?? string.Empty;
        public string SelectedProductSellingPriceDisplay => SelectedProduct?.SellingPriceDisplay ?? string.Empty;
        public string SelectedProductStockDisplay => SelectedProduct?.StockDisplay ?? string.Empty;
        public int SelectedProductAvailableSerialCount => SelectedProduct?.AvailableSerialCount ?? 0;
        public string SelectedProductDescription => SelectedProduct?.Description ?? string.Empty;
        public string SelectedProductSpecificationsDisplay => SelectedProduct?.SpecificationsDisplay ?? string.Empty;
        public Uri? SelectedProductPrimaryImageUri => SelectedProduct?.PrimaryImageUri;
        public bool SelectedProductHasPrimaryImage => SelectedProduct?.HasPrimaryImage == true;

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(Math.Max(_totalCount, 1) / (double)SelectedPageSize));

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await LoadFilterOptionsAsync();
            await LoadProductsAsync();
            IsInitialized = true;
        }

        private async Task LoadFilterOptionsAsync()
        {
            var brandResult = await _brandService.GetAllAsync();
            var categoryResult = await _categoryService.GetAllAsync();

            BrandOptions.Clear();
            BrandOptions.Add(new StoreFilterOption(null, "All brands"));
            if (brandResult.IsSuccess && brandResult.Value is not null)
            {
                foreach (var brand in brandResult.Value.OrderBy(item => item.Name))
                {
                    BrandOptions.Add(new StoreFilterOption(brand.BrandId, brand.Name));
                }
            }

            CategoryOptions.Clear();
            CategoryOptions.Add(new StoreFilterOption(null, "All categories"));
            if (categoryResult.IsSuccess && categoryResult.Value is not null)
            {
                foreach (var category in categoryResult.Value.OrderBy(item => item.Name))
                {
                    CategoryOptions.Add(new StoreFilterOption(category.CategoryId, category.Name));
                }
            }

            SelectedBrand ??= BrandOptions.FirstOrDefault();
            SelectedCategory ??= CategoryOptions.FirstOrDefault();
        }

        private async Task LoadProductsAsync()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _productService.GetAllAsync(new ProductFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    BrandId = SelectedBrand?.Id,
                    CategoryId = SelectedCategory?.Id,
                    InStockOnly = InStockOnly,
                    SortBy = "name",
                    SortOrder = "asc",
                    Page = CurrentPage,
                    PageSize = SelectedPageSize
                });

                if (!result.IsSuccess || result.Value is null)
                {
                    Products.Clear();
                    _totalCount = 0;
                    RebuildPageButtons();
                    ErrorMessage = result.Error ?? "Failed to load products.";
                    OnPropertyChanged(nameof(ResultText));
                    return;
                }

                Products.Clear();
                foreach (var product in result.Value.Items)
                {
                    Products.Add(StoreProductListItemViewModel.FromProduct(product, NormalizeImageUri));
                }

                _totalCount = result.Value.TotalCount;
                RebuildPageButtons();
                OnPropertyChanged(nameof(ResultText));

                if (Products.Count == 0)
                {
                    SelectedProductCard = null;
                    SelectedProduct = null;
                    return;
                }

                if (SelectedProductCard is null || Products.All(item => item.ProductId != SelectedProductCard.ProductId))
                {
                    SelectedProductCard = Products.First();
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProductDetailAsync(int productId)
        {
            var result = await _productService.GetByIdAsync(productId);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load product detail.";
                return;
            }

            SelectedProduct = StoreProductDetailViewModel.FromProduct(result.Value, NormalizeImageUri);
        }

        private async Task PreviousPageAsync()
        {
            if (CurrentPage <= 1)
            {
                return;
            }

            CurrentPage--;
            await LoadProductsAsync();
        }

        private async Task NextPageAsync()
        {
            if (CurrentPage >= TotalPages)
            {
                return;
            }

            CurrentPage++;
            await LoadProductsAsync();
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == CurrentPage)
            {
                return;
            }

            CurrentPage = page;
            await LoadProductsAsync();
        }

        private void DebounceSearch()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = new CancellationTokenSource();
            _ = DebounceSearchAsync(_searchDebounceCts.Token);
        }

        private async Task DebounceSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(350, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadProductsAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
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

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            PageButtons.Add(new PageButtonItem
            {
                Label = "<- Previous",
                PageNumber = CurrentPage - 1,
                IsEnabled = CurrentPage > 1,
                IsCurrent = false
            });

            int? previousPage = null;
            foreach (var page in BuildPageNumbers(CurrentPage, TotalPages))
            {
                if (previousPage.HasValue && page - previousPage.Value > 1)
                {
                    PageButtons.Add(new PageButtonItem
                    {
                        Label = "...",
                        PageNumber = -1,
                        IsEnabled = false,
                        IsCurrent = false
                    });
                }

                PageButtons.Add(new PageButtonItem
                {
                    Label = page.ToString(),
                    PageNumber = page,
                    IsEnabled = page != CurrentPage,
                    IsCurrent = page == CurrentPage
                });
                previousPage = page;
            }

            PageButtons.Add(new PageButtonItem
            {
                Label = "Next ->",
                PageNumber = CurrentPage + 1,
                IsEnabled = CurrentPage < TotalPages,
                IsCurrent = false
            });
        }

        private static IEnumerable<int> BuildPageNumbers(int currentPage, int totalPages)
        {
            var pages = new SortedSet<int> { 1, totalPages };
            for (var offset = -1; offset <= 1; offset++)
            {
                var value = currentPage + offset;
                if (value >= 1 && value <= totalPages)
                {
                    pages.Add(value);
                }
            }

            return pages;
        }
    }

    public class StoreFilterOption
    {
        public StoreFilterOption(int? id, string name)
        {
            Id = id;
            Name = name;
        }

        public int? Id { get; }
        public string Name { get; }
    }

    public class StoreProductListItemViewModel
    {
        private StoreProductListItemViewModel(
            int productId,
            string name,
            string sku,
            string brandName,
            double sellingPrice,
            int stockQuantity,
            IEnumerable<ProductImageDto> images,
            Func<string?, Uri?> imageResolver)
        {
            ProductId = productId;
            Name = name;
            Sku = sku;
            BrandName = brandName;
            PriceDisplay = sellingPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
            StockDisplay = stockQuantity > 0 ? $"{stockQuantity} in stock" : "Out of stock";

            var firstImage = images
                .OrderBy(image => image.DisplayOrder)
                .Select(image => image.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            ThumbnailUri = imageResolver(firstImage);
        }

        public int ProductId { get; }
        public string Name { get; }
        public string Sku { get; }
        public string BrandName { get; }
        public string PriceDisplay { get; }
        public string StockDisplay { get; }
        public Uri? ThumbnailUri { get; }
        public bool HasThumbnail => ThumbnailUri is not null;

        public static StoreProductListItemViewModel FromProduct(ProductDto product, Func<string?, Uri?> imageResolver)
        {
            return new StoreProductListItemViewModel(
                product.ProductId,
                product.Name,
                product.Sku,
                product.Brand?.Name ?? "Unknown brand",
                product.SellingPrice,
                product.StockQuantity,
                product.Images,
                imageResolver);
        }
    }

    public class StoreProductDetailViewModel
    {
        private StoreProductDetailViewModel(ProductDto product, Func<string?, Uri?> imageResolver)
        {
            ProductId = product.ProductId;
            Name = product.Name;
            Sku = product.Sku;
            BrandName = product.Brand?.Name ?? "Unknown brand";
            SeriesName = product.Series?.Name ?? "Standard lineup";
            Description = string.IsNullOrWhiteSpace(product.Description) ? "No description provided." : product.Description;
            WarrantyDisplay = $"{product.WarrantyMonths} months";
            CategoriesDisplay = product.Categories.Count == 0
                ? "No categories"
                : string.Join(", ", product.Categories.Select(category => category.Name));
            SellingPriceDisplay = product.SellingPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
            StockDisplay = $"{product.StockQuantity} units ready";
            AvailableSerialCount = product.Instances.Count(instance => string.Equals(instance.Status, "Available", StringComparison.OrdinalIgnoreCase));
            SpecificationsDisplay = FormatSpecifications(product.Specifications);

            var firstImage = product.Images
                .OrderBy(image => image.DisplayOrder)
                .Select(image => image.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            PrimaryImageUri = imageResolver(firstImage);
        }

        public int ProductId { get; }
        public string Name { get; }
        public string Sku { get; }
        public string BrandName { get; }
        public string SeriesName { get; }
        public string Description { get; }
        public string CategoriesDisplay { get; }
        public string WarrantyDisplay { get; }
        public string SellingPriceDisplay { get; }
        public string StockDisplay { get; }
        public int AvailableSerialCount { get; }
        public string SpecificationsDisplay { get; }
        public Uri? PrimaryImageUri { get; }
        public bool HasPrimaryImage => PrimaryImageUri is not null;

        public static StoreProductDetailViewModel FromProduct(ProductDto product, Func<string?, Uri?> imageResolver)
        {
            return new StoreProductDetailViewModel(product, imageResolver);
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
}
