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
        private const int DefaultPageSize = 10;
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IConfigService _configService;
        private readonly ISettingsService _settingsService;
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
        private StoreSortOption? _selectedSortField;
        private StoreSortOption? _selectedSortDirection;

        public StoreViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService,
            IConfigService configService,
            ISettingsService settingsService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
            _configService = configService;
            _settingsService = settingsService;
            _selectedPageSize = NormalizePageSize(_settingsService.PageSize);
            if (_settingsService.PageSize != _selectedPageSize)
            {
                _settingsService.PageSize = _selectedPageSize;
            }
            _settingsService.SettingsChanged += OnSettingsChanged;

            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);
            RefreshCommand = new AsyncRelayCommand(LoadProductsAsync, () => !IsLoading);
            PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => !IsLoading && CurrentPage > 1);
            NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => !IsLoading && CurrentPage < TotalPages);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            OpenProductCommand = new RelayCommand<StoreProductCardViewModel?>(OpenProduct);

            SortFieldOptions.Add(new StoreSortOption("name", "Name"));
            SortFieldOptions.Add(new StoreSortOption("price", "Price"));
            SortFieldOptions.Add(new StoreSortOption("stock", "Stock"));
            SortFieldOptions.Add(new StoreSortOption("createdAt", "Newest"));

            SortDirectionOptions.Add(new StoreSortOption("asc", "Ascending"));
            SortDirectionOptions.Add(new StoreSortOption("desc", "Descending"));
        }

        public ObservableCollection<StoreProductCardViewModel> Products { get; } = [];
        public ObservableCollection<StoreFilterOption> BrandOptions { get; } = [];
        public ObservableCollection<StoreFilterOption> CategoryOptions { get; } = [];
        public ObservableCollection<StoreSortOption> SortFieldOptions { get; } = [];
        public ObservableCollection<StoreSortOption> SortDirectionOptions { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [5, 10, 15, 20];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand RefreshCommand { get; }
        public IAsyncRelayCommand PreviousPageCommand { get; }
        public IAsyncRelayCommand NextPageCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IRelayCommand<StoreProductCardViewModel?> OpenProductCommand { get; }

        public Action<int>? NavigateToProductRequested { get; set; }

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

        public StoreSortOption? SelectedSortField
        {
            get => _selectedSortField;
            set
            {
                if (SetProperty(ref _selectedSortField, value) && IsInitialized)
                {
                    CurrentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public StoreSortOption? SelectedSortDirection
        {
            get => _selectedSortDirection;
            set
            {
                if (SetProperty(ref _selectedSortDirection, value) && IsInitialized)
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
                var normalizedValue = NormalizePageSize(value);
                if (SetProperty(ref _selectedPageSize, normalizedValue))
                {
                    _settingsService.PageSize = normalizedValue;
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

        private int TotalPages => Math.Max(1, (int)Math.Ceiling(Math.Max(_totalCount, 1) / (double)SelectedPageSize));

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            await LoadFilterOptionsAsync();
            SelectedSortField ??= SortFieldOptions.FirstOrDefault(option => option.Key == "createdAt") ?? SortFieldOptions.FirstOrDefault();
            SelectedSortDirection ??= SortDirectionOptions.FirstOrDefault(option => option.Key == "desc") ?? SortDirectionOptions.FirstOrDefault();
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
                    SortBy = SelectedSortField?.Key ?? "name",
                    SortOrder = SelectedSortDirection?.Key ?? "asc",
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
                    Products.Add(StoreProductCardViewModel.FromProduct(
                        product,
                        NormalizeImageUri,
                        GetProductBadge(product)));
                }

                _totalCount = result.Value.TotalCount;
                RebuildPageButtons();
                OnPropertyChanged(nameof(ResultText));
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetProductBadge(ProductDto product)
        {
            if (product.StockQuantity >= 8)
            {
                return "Best Seller";
            }

            if (product.Categories.Count > 0)
            {
                return product.Categories[0].Name;
            }

            return "Featured";
        }

        private void OpenProduct(StoreProductCardViewModel? product)
        {
            if (product is null)
            {
                return;
            }

            NavigateToProductRequested?.Invoke(product.ProductId);
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

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            var normalizedValue = NormalizePageSize(_settingsService.PageSize);
            if (_selectedPageSize == normalizedValue)
            {
                return;
            }

            _selectedPageSize = normalizedValue;
            OnPropertyChanged(nameof(SelectedPageSize));
            OnPropertyChanged(nameof(ResultText));
            CurrentPage = 1;
            if (IsInitialized)
            {
                _ = LoadProductsAsync();
            }
        }

        private static int NormalizePageSize(int value)
        {
            return value is 5 or 10 or 15 or 20 ? value : DefaultPageSize;
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

    public class StoreSortOption
    {
        public StoreSortOption(string key, string label)
        {
            Key = key;
            Label = label;
        }

        public string Key { get; }
        public string Label { get; }
    }

    public class StoreProductCardViewModel
    {
        private StoreProductCardViewModel(
            int productId,
            string badgeText,
            string name,
            string brandName,
            double sellingPrice,
            int stockQuantity,
            IEnumerable<ProductImageDto> images,
            IEnumerable<string> highlights,
            Func<string?, Uri?> imageResolver)
        {
            ProductId = productId;
            BadgeText = badgeText;
            Name = name;
            BrandName = brandName;
            PriceDisplay = sellingPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
            AvailabilityText = stockQuantity > 0 ? $"{stockQuantity} ready to ship" : "Out of stock";
            Highlights = [.. highlights.Take(4)];

            var firstImage = images
                .OrderBy(image => image.DisplayOrder)
                .Select(image => image.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            ThumbnailUri = imageResolver(firstImage);
        }

        public int ProductId { get; }
        public string BadgeText { get; }
        public string Name { get; }
        public string BrandName { get; }
        public string PriceDisplay { get; }
        public string AvailabilityText { get; }
        public IReadOnlyList<string> Highlights { get; }
        public Uri? ThumbnailUri { get; }
        public bool HasThumbnail => ThumbnailUri is not null;

        public static StoreProductCardViewModel FromProduct(
            ProductDto product,
            Func<string?, Uri?> imageResolver,
            string badgeText)
        {
            return new StoreProductCardViewModel(
                product.ProductId,
                badgeText,
                product.Name,
                product.Brand?.Name ?? "Unknown brand",
                product.SellingPrice,
                product.StockQuantity,
                product.Images,
                BuildHighlights(product),
                imageResolver);
        }

        private static IEnumerable<string> BuildHighlights(ProductDto product)
        {
            var lines = new List<string>();

            if (product.Specifications is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                var orderedKeys = new[] { "cpu", "processor", "gpu", "graphics", "ram", "memory", "display", "screen" };
                foreach (var key in orderedKeys)
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
                if (product.Categories.Count > 0)
                {
                    lines.Add(string.Join(" / ", product.Categories.Select(category => category.Name)));
                }

                lines.Add($"{product.WarrantyMonths} month warranty");
            }

            return lines.Take(4);
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

    }
}
