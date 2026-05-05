using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Products.Dto;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Products
{
    public class ProductsViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IGraphQLClientService _graphQLClientService;

        private CancellationTokenSource? _searchDebounceCts;
        private string _searchQuery = string.Empty;
        private int _selectedPageSize = 10;
        private bool _isAllOnPageSelected;
        private int _currentPage = 1;
        private int _totalCount;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _isInitialized;
        private int? _selectedCategoryId;
        private int? _selectedBrandId;
        private string _selectedSortField = "name";
        private string _selectedSortOrder = "asc";
        private int _loadVersion;
        private bool _suppressFilterReload;
        private bool _suppressSortReload;
        private bool _suppressSelectAllChange;

        public ProductsViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService,
            IGraphQLClientService graphQLClientService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
            _graphQLClientService = graphQLClientService;

            AddProductCommand = new RelayCommand(AddProduct);
            EditProductCommand = new RelayCommand<int>(EditProduct);
            DeleteProductCommand = new AsyncRelayCommand<int>(DeleteSingleProductAsync);
            GoToPageCommand = new AsyncRelayCommand<int>(GoToPageAsync);
            BulkToggleStatusCommand = new AsyncRelayCommand(BulkToggleStatusAsync);
            BulkDeleteCommand = new AsyncRelayCommand(BulkDeleteAsync);
            ClearFiltersCommand = new AsyncRelayCommand(ClearFiltersAsync, () => !IsLoading);
            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized && !IsLoading);

            SortFieldOptions.Add(new SortOptionViewModel("name", "Name"));
            SortFieldOptions.Add(new SortOptionViewModel("sellingPrice", "Price"));
            SortFieldOptions.Add(new SortOptionViewModel("stockQuantity", "Stock"));

            SortOrderOptions.Add(new SortOptionViewModel("asc", "Asc"));
            SortOrderOptions.Add(new SortOptionViewModel("desc", "Desc"));
        }

        public ObservableCollection<ProductRowViewModel> PagedProducts { get; } = [];
        public ObservableCollection<int> PageSizeOptions { get; } = [10, 20, 50];
        public ObservableCollection<PageButtonItem> PageButtons { get; } = [];
        public ObservableCollection<FilterOptionViewModel> CategoryOptions { get; } = [];
        public ObservableCollection<FilterOptionViewModel> BrandOptions { get; } = [];
        public ObservableCollection<SortOptionViewModel> SortFieldOptions { get; } = [];
        public ObservableCollection<SortOptionViewModel> SortOrderOptions { get; } = [];

        public IRelayCommand AddProductCommand { get; }
        public IRelayCommand<int> EditProductCommand { get; }
        public IAsyncRelayCommand<int> DeleteProductCommand { get; }
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand BulkToggleStatusCommand { get; }
        public IAsyncRelayCommand BulkDeleteCommand { get; }
        public IAsyncRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand InitializeCommand { get; }

        public event EventHandler? NavigateToAddProductRequested;
        public event Action<int>? NavigateToEditProductRequested;
        public Func<int, Task<bool>>? ConfirmBulkDeleteAsync { get; set; }
        public Func<int, Task<bool>>? ConfirmRowDeleteAsync { get; set; }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _currentPage = 1;
                    DebounceSearch();
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
                    _currentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

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
                    ClearFiltersCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(IsBusy));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool IsBusy => IsLoading;

        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(IsEmpty));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public bool IsEmpty => !IsLoading && !HasError && PagedProducts.Count == 0;

        public string SelectedSortField
        {
            get => _selectedSortField;
            set
            {
                if (SetProperty(ref _selectedSortField, value))
                {
                    if (_suppressSortReload)
                    {
                        return;
                    }

                    _currentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public string SelectedSortOrder
        {
            get => _selectedSortOrder;
            set
            {
                if (SetProperty(ref _selectedSortOrder, value))
                {
                    if (_suppressSortReload)
                    {
                        return;
                    }

                    _currentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public int? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set
            {
                if (SetProperty(ref _selectedCategoryId, value))
                {
                    if (_suppressFilterReload)
                    {
                        return;
                    }

                    _currentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public int? SelectedBrandId
        {
            get => _selectedBrandId;
            set
            {
                if (SetProperty(ref _selectedBrandId, value))
                {
                    if (_suppressFilterReload)
                    {
                        return;
                    }

                    _currentPage = 1;
                    _ = LoadProductsAsync();
                }
            }
        }

        public bool IsAllOnPageSelected
        {
            get => _isAllOnPageSelected;
            set
            {
                if (SetProperty(ref _isAllOnPageSelected, value))
                {
                    if (_suppressSelectAllChange)
                    {
                        return;
                    }

                    foreach (var row in PagedProducts)
                    {
                        row.IsSelected = value;
                    }

                    NotifySelectionChanged();
                }
            }
        }

        public bool HasSelection => PagedProducts.Any(product => product.IsSelected);
        public string SelectionActionText => $"{PagedProducts.Count(product => product.IsSelected)} selected";

        public string ResultText
        {
            get
            {
                if (_totalCount == 0)
                {
                    return "Result 0 of 0";
                }

                var start = ((_currentPage - 1) * SelectedPageSize) + 1;
                var end = Math.Min(_currentPage * SelectedPageSize, _totalCount);
                return $"Result {start}-{end} of {_totalCount}";
            }
        }

        private int TotalPages =>
            Math.Max(1, (int)Math.Ceiling((double)Math.Max(_totalCount, 1) / SelectedPageSize));

        private void AddProduct()
        {
            NavigateToAddProductRequested?.Invoke(this, EventArgs.Empty);
        }

        private void EditProduct(int productId)
        {
            if (productId <= 0)
            {
                return;
            }

            NavigateToEditProductRequested?.Invoke(productId);
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            try
            {
                _suppressSortReload = true;
                if (!SortFieldOptions.Any(option => option.Value == SelectedSortField))
                {
                    SelectedSortField = "name";
                }

                if (!SortOrderOptions.Any(option => option.Value == SelectedSortOrder))
                {
                    SelectedSortOrder = "asc";
                }
                _suppressSortReload = false;

                await LoadFilterOptionsAsync();
                await LoadProductsAsync(clearError: false);
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                _suppressSortReload = false;
                ErrorMessage = $"Failed to initialize products page: {ex.Message}";
            }
        }

        private async Task GoToPageAsync(int page)
        {
            if (page < 1 || page > TotalPages || page == _currentPage)
            {
                return;
            }

            _currentPage = page;
            await LoadProductsAsync();
        }

        private async Task ClearFiltersAsync()
        {
            _suppressFilterReload = true;
            SelectedCategoryId = null;
            SelectedBrandId = null;
            _suppressFilterReload = false;

            _currentPage = 1;
            await LoadProductsAsync();
        }

        private async Task LoadFilterOptionsAsync()
        {
            var brandsTask = _brandService.GetAllAsync();
            var categoriesTask = _categoryService.GetAllAsync();
            await Task.WhenAll(brandsTask, categoriesTask);

            var brandsResult = brandsTask.Result;
            if (!brandsResult.IsSuccess || brandsResult.Value is null)
            {
                ErrorMessage = brandsResult.Error ?? "Failed to load brands.";
                return;
            }

            var categoriesResult = categoriesTask.Result;
            if (!categoriesResult.IsSuccess || categoriesResult.Value is null)
            {
                ErrorMessage = categoriesResult.Error ?? "Failed to load categories.";
                return;
            }

            BrandOptions.Clear();
            BrandOptions.Add(new FilterOptionViewModel(null, "All brands"));
            foreach (var brand in brandsResult.Value.OrderBy(brand => brand.Name))
            {
                BrandOptions.Add(new FilterOptionViewModel(brand.BrandId, brand.Name));
            }

            CategoryOptions.Clear();
            CategoryOptions.Add(new FilterOptionViewModel(null, "All categories"));
            foreach (var category in categoriesResult.Value.OrderBy(category => category.Name))
            {
                CategoryOptions.Add(new FilterOptionViewModel(category.CategoryId, category.Name));
            }
        }

        private async Task BulkToggleStatusAsync()
        {
            var selectedRows = PagedProducts.Where(product => product.IsSelected).ToList();
            if (selectedRows.Count == 0)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                foreach (var row in selectedRows)
                {
                    var result = await _productService.UpdateAsync(row.ProductId, new UpdateProductInput
                    {
                        IsActive = !row.IsActive
                    });

                    if (!result.IsSuccess)
                    {
                        ErrorMessage = result.Error ?? "Failed to update product status.";
                        return;
                    }
                }

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BulkDeleteAsync()
        {
            var selectedIds = PagedProducts
                .Where(product => product.IsSelected)
                .Select(product => product.ProductId)
                .ToList();

            if (selectedIds.Count == 0 || ConfirmBulkDeleteAsync is null)
            {
                return;
            }

            var confirmed = await ConfirmBulkDeleteAsync(selectedIds.Count);
            if (!confirmed)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                foreach (var productId in selectedIds)
                {
                    var result = await _productService.DeleteAsync(productId);
                    if (!result.IsSuccess)
                    {
                        ErrorMessage = result.Error ?? "Failed to delete product.";
                        return;
                    }
                }

                if ((_currentPage - 1) * SelectedPageSize >= Math.Max(_totalCount - selectedIds.Count, 0) && _currentPage > 1)
                {
                    _currentPage--;
                }

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSingleProductAsync(int productId)
        {
            if (productId <= 0 || ConfirmRowDeleteAsync is null)
            {
                return;
            }

            var confirmed = await ConfirmRowDeleteAsync(productId);
            if (!confirmed)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _productService.DeleteAsync(productId);
                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Error ?? "Failed to delete product.";
                    return;
                }

                if (PagedProducts.Count <= 1 && _currentPage > 1)
                {
                    _currentPage--;
                }

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadProductsAsync(bool clearError = true)
        {
            var requestVersion = ++_loadVersion;

            IsLoading = true;
            if (clearError)
            {
                ErrorMessage = string.Empty;
            }

            try
            {
                var result = await _productService.GetAllAsync(new ProductFilterDto
                {
                    Search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                    CategoryId = SelectedCategoryId,
                    BrandId = SelectedBrandId,
                    SortBy = SelectedSortField,
                    SortOrder = SelectedSortOrder,
                    Page = _currentPage,
                    PageSize = SelectedPageSize
                });

                if (requestVersion != _loadVersion)
                {
                    return;
                }

                if (!result.IsSuccess || result.Value is null)
                {
                    _totalCount = 0;
                    PagedProducts.Clear();
                    RebuildPageButtons();
                    UpdateSelectAllState();
                    NotifySelectionChanged();
                    OnPropertyChanged(nameof(ResultText));
                    OnPropertyChanged(nameof(IsEmpty));
                    ErrorMessage = result.Error ?? "Failed to load products.";
                    return;
                }

                var page = result.Value;
                _totalCount = page.TotalCount;
                if (_currentPage > TotalPages)
                {
                    _currentPage = TotalPages;
                }

                PagedProducts.Clear();
                foreach (var item in page.Items)
                {
                    var row = new ProductRowViewModel(
                        item.ProductId,
                        item.Sku,
                        item.Name,
                        item.Categories.Count > 0 ? string.Join(", ", item.Categories.Select(category => category.Name)) : "Uncategorized",
                        item.StockQuantity,
                        Convert.ToDecimal(item.SellingPrice),
                        item.IsActive,
                        BuildThumbnailUri(item));

                    row.PropertyChanged += Row_PropertyChanged;
                    PagedProducts.Add(row);
                }

                RebuildPageButtons();
                UpdateSelectAllState();
                NotifySelectionChanged();
                OnPropertyChanged(nameof(ResultText));
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch (Exception ex)
            {
                if (requestVersion == _loadVersion)
                {
                    _totalCount = 0;
                    PagedProducts.Clear();
                    RebuildPageButtons();
                    UpdateSelectAllState();
                    NotifySelectionChanged();
                    OnPropertyChanged(nameof(ResultText));
                    OnPropertyChanged(nameof(IsEmpty));
                    ErrorMessage = ex.Message;
                }
            }
            finally
            {
                if (requestVersion == _loadVersion)
                {
                    IsLoading = false;
                }
            }
        }

        private void DebounceSearch()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;
            _ = DebounceSearchAsync(token);
        }

        private async Task DebounceSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(400, token);
                if (!token.IsCancellationRequested)
                {
                    await LoadProductsAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void RebuildPageButtons()
        {
            PageButtons.Clear();

            PageButtons.Add(new PageButtonItem
            {
                Label = "<- Previous",
                PageNumber = _currentPage - 1,
                IsEnabled = _currentPage > 1,
                IsCurrent = false,
            });

            var pages = BuildPageNumbers(_currentPage, TotalPages);
            int? previousPage = null;

            foreach (var page in pages)
            {
                if (previousPage.HasValue && page - previousPage.Value > 1)
                {
                    PageButtons.Add(new PageButtonItem
                    {
                        Label = "...",
                        PageNumber = -1,
                        IsEnabled = false,
                        IsCurrent = false,
                    });
                }

                PageButtons.Add(new PageButtonItem
                {
                    Label = page.ToString(),
                    PageNumber = page,
                    IsEnabled = page != _currentPage,
                    IsCurrent = page == _currentPage,
                });

                previousPage = page;
            }

            PageButtons.Add(new PageButtonItem
            {
                Label = "Next ->",
                PageNumber = _currentPage + 1,
                IsEnabled = _currentPage < TotalPages,
                IsCurrent = false,
            });
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProductRowViewModel.IsSelected))
            {
                return;
            }

            NotifySelectionChanged();
            UpdateSelectAllState();
        }

        private void UpdateSelectAllState()
        {
            if (PagedProducts.Count == 0)
            {
                SetSelectAll(false);
                return;
            }

            var allSelected = PagedProducts.All(product => product.IsSelected);
            if (IsAllOnPageSelected != allSelected)
            {
                SetSelectAll(allSelected);
            }
        }

        private void SetSelectAll(bool value)
        {
            _suppressSelectAllChange = true;
            IsAllOnPageSelected = value;
            _suppressSelectAllChange = false;
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectionActionText));
        }

        private Uri? BuildThumbnailUri(ProductDto product)
        {
            var imageUrl = product.Images
                .OrderBy(image => image.DisplayOrder)
                .Select(image => image.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            return NormalizeImageUri(imageUrl);
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

            if (!Uri.TryCreate(_graphQLClientService.ServerUrl, UriKind.Absolute, out var graphQlUri))
            {
                return null;
            }

            var baseOrigin = new Uri(graphQlUri.GetLeftPart(UriPartial.Authority));
            var normalizedPath = imageUrl.StartsWith("/", StringComparison.Ordinal) ? imageUrl : $"/{imageUrl}";

            return Uri.TryCreate(baseOrigin, normalizedPath, out var resolvedUri)
                ? resolvedUri
                : null;
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
}
