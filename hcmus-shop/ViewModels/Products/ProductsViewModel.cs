using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Brands;
using hcmus_shop.Services.Categories;
using hcmus_shop.Services.Products;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
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

        public ProductsViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;

            AddProductCommand = new RelayCommand(AddProduct);
            EditProductCommand = new RelayCommand<int>(EditProduct);
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
        public IAsyncRelayCommand<int> GoToPageCommand { get; }
        public IAsyncRelayCommand BulkToggleStatusCommand { get; }
        public IAsyncRelayCommand BulkDeleteCommand { get; }
        public IAsyncRelayCommand ClearFiltersCommand { get; }
        public IAsyncRelayCommand InitializeCommand { get; }

        public event EventHandler? NavigateToAddProductRequested;
        public event Action<int>? NavigateToEditProductRequested;
        public Func<int, Task<bool>>? ConfirmBulkDeleteAsync { get; set; }

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
                    foreach (var row in PagedProducts)
                    {
                        row.IsSelected = value;
                    }

                    NotifySelectionChanged();
                }
            }
        }

        public bool HasSelection => PagedProducts.Any(p => p.IsSelected);

        public string SelectionActionText => $"{PagedProducts.Count(p => p.IsSelected)} selected";

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
            try
            {
                var brandsTask = _brandService.GetAllAsync();
                var categoriesTask = _categoryService.GetAllAsync();

                await Task.WhenAll(brandsTask, categoriesTask);

                BrandOptions.Clear();
                BrandOptions.Add(new FilterOptionViewModel(null, "All brands"));
                foreach (var brand in brandsTask.Result.OrderBy(b => b.Name))
                {
                    BrandOptions.Add(new FilterOptionViewModel(brand.BrandId, brand.Name));
                }

                CategoryOptions.Clear();
                CategoryOptions.Add(new FilterOptionViewModel(null, "All categories"));
                foreach (var category in categoriesTask.Result.OrderBy(c => c.Name))
                {
                    CategoryOptions.Add(new FilterOptionViewModel(category.CategoryId, category.Name));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load filters: {ex.Message}";
            }
        }

        private async Task BulkToggleStatusAsync()
        {
            var selectedRows = PagedProducts.Where(p => p.IsSelected).ToList();
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
                    await _productService.UpdateAsync(row.ProductId, new UpdateProductInput
                    {
                        IsActive = !row.IsActive
                    });
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
                .Where(p => p.IsSelected)
                .Select(p => p.ProductId)
                .ToList();

            if (selectedIds.Count == 0)
            {
                return;
            }

            if (ConfirmBulkDeleteAsync is null)
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
                    await _productService.DeleteAsync(productId);
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
                var page = await _productService.GetAllAsync(new ProductFilterDto
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
                        item.Categories.Count > 0 ? string.Join(", ", item.Categories.Select(c => c.Name)) : "Uncategorized",
                        item.StockQuantity,
                        Convert.ToDecimal(item.SellingPrice),
                        item.IsActive);

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

            var accentBackground = new SolidColorBrush(ColorHelper.FromArgb(255, 111, 126, 255));
            var normalBackground = new SolidColorBrush(Colors.Transparent);
            var activeForeground = new SolidColorBrush(Colors.White);
            var normalForeground = App.Current.Resources["TextPrimary"] as Brush
                ?? new SolidColorBrush(Colors.Black);

            PageButtons.Add(new PageButtonItem
            {
                Label = "<- Previous",
                PageNumber = _currentPage - 1,
                IsEnabled = _currentPage > 1,
                IsCurrent = false,
                Background = normalBackground,
                Foreground = normalForeground,
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
                        Background = normalBackground,
                        Foreground = normalForeground,
                    });
                }

                PageButtons.Add(new PageButtonItem
                {
                    Label = page.ToString(),
                    PageNumber = page,
                    IsEnabled = page != _currentPage,
                    IsCurrent = page == _currentPage,
                    Background = page == _currentPage ? accentBackground : normalBackground,
                    Foreground = page == _currentPage ? activeForeground : normalForeground,
                });

                previousPage = page;
            }

            PageButtons.Add(new PageButtonItem
            {
                Label = "Next ->",
                PageNumber = _currentPage + 1,
                IsEnabled = _currentPage < TotalPages,
                IsCurrent = false,
                Background = normalBackground,
                Foreground = normalForeground,
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
                if (_isAllOnPageSelected)
                {
                    _isAllOnPageSelected = false;
                    OnPropertyChanged(nameof(IsAllOnPageSelected));
                }
                return;
            }

            var allSelected = PagedProducts.All(p => p.IsSelected);
            if (IsAllOnPageSelected != allSelected)
            {
                _isAllOnPageSelected = allSelected;
                OnPropertyChanged(nameof(IsAllOnPageSelected));
            }
        }

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectionActionText));
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
