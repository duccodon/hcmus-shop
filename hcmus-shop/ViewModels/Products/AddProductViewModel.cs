using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Contracts.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Products
{
    public partial class AddProductViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly ISeriesService _seriesService;

        [ObservableProperty]
        public partial ImagePreview MainPreview { get; set; } = new();

        [ObservableProperty]
        public partial int? SelectedBrandId { get; set; }

        [ObservableProperty]
        private int? _selectedSeriesId;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isInitialized;

        [ObservableProperty]
        private string? _errorMessage;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        partial void OnErrorMessageChanged(string? value)
        {
            OnPropertyChanged(nameof(HasError));
        }

        public AddProductViewModel(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService,
            ISeriesService seriesService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
            _seriesService = seriesService;

            DraftProduct = new CreateProductInput
            {
                Sku = string.Empty,
                Name = string.Empty,
                Description = string.Empty,
                WarrantyMonths = 12,
                ImportPrice = 0,
                SellingPrice = 0,
                BrandId = 0,
            };
        }

        public CreateProductInput DraftProduct { get; }

        public ObservableCollection<ImagePreview> PreviewImages { get; } = [];

        public ObservableCollection<LookupOptionViewModel> BrandOptions { get; } = [];

        public ObservableCollection<LookupOptionViewModel> SeriesOptions { get; } = [];

        public ObservableCollection<CategoryOptionViewModel> CategoryOptions { get; } = [];

        public event EventHandler? ProductSaved;

        public bool HasMainPreview => MainPreview.Bitmap is not null;

        public double ImportPriceValue
        {
            get => DraftProduct.ImportPrice;
            set
            {
                DraftProduct.ImportPrice = value;
                OnPropertyChanged(nameof(ImportPriceValue));
            }
        }

        public double SellingPriceValue
        {
            get => DraftProduct.SellingPrice;
            set
            {
                DraftProduct.SellingPrice = value;
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

        public void AddImagePreview(ImagePreview preview)
        {
            preview.DisplayOrder = PreviewImages.Count;
            PreviewImages.Add(preview);

            if (!HasMainPreview)
            {
                MainPreview = preview;
            }
        }

        [RelayCommand]
        private void SelectPreview(ImagePreview? preview)
        {
            if (preview is null)
            {
                return;
            }

            MainPreview = preview;
        }

        [RelayCommand]
        private void AddCategory()
        {
        }

        [RelayCommand(CanExecute = nameof(CanInitialize))]
        private async Task InitializeAsync()
        {
            if (IsInitialized) return;

            IsBusy = true;
            ErrorMessage = null;

            var brandsResult = await _brandService.GetAllAsync();
            if (!brandsResult.IsSuccess)
            {
                ErrorMessage = brandsResult.Error;
                IsBusy = false;
                return;
            }

            var categoriesResult = await _categoryService.GetAllAsync();
            if (!categoriesResult.IsSuccess)
            {
                ErrorMessage = categoriesResult.Error;
                IsBusy = false;
                return;
            }

            BrandOptions.Clear();
            foreach (var brand in brandsResult.Value)
            {
                BrandOptions.Add(new LookupOptionViewModel(brand.BrandId, brand.Name));
            }

            CategoryOptions.Clear();
            foreach (var category in categoriesResult.Value)
            {
                CategoryOptions.Add(new CategoryOptionViewModel(category.CategoryId, category.Name));
            }

            SelectedBrandId = BrandOptions.FirstOrDefault()?.Id;

            IsInitialized = true;
            IsBusy = false;
        }

        private async Task LoadSeriesOptionsAsync()
        {
            SeriesOptions.Clear();

            if (!SelectedBrandId.HasValue) return;

            var result = await _seriesService.GetByBrandAsync(SelectedBrandId.Value);

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error;
                return;
            }

            foreach (var series in result.Value)
            {
                SeriesOptions.Add(new LookupOptionViewModel(series.SeriesId, series.Name));
            }
        }

        [RelayCommand(CanExecute = nameof(CanSaveProduct))]
        private async Task SaveProductAsync()
        {
            if (IsBusy)
            {
                return;
            }

            ErrorMessage = null;
            if (string.IsNullOrWhiteSpace(DraftProduct.Sku) || string.IsNullOrWhiteSpace(DraftProduct.Name))
            {
                ErrorMessage = "SKU and product name are required.";
                return;
            }

            if (DraftProduct.BrandId <= 0)
            {
                ErrorMessage = "Please select a brand.";
                return;
            }

            DraftProduct.CategoryIds =
            [
                .. CategoryOptions
                    .Where(option => option.IsSelected)
                    .Select(option => option.CategoryId)
            ];

            DraftProduct.ImageUrls =
            [
                .. PreviewImages
                    .Where(preview => preview.File is not null)
                    .Select(preview => preview.File!.Path)
            ];

            DraftProduct.Specifications = string.IsNullOrWhiteSpace(DraftProduct.Specifications)
                ? null
                : DraftProduct.Specifications;

            IsBusy = true;
            try
            {
                await _productService.CreateAsync(DraftProduct);
                ProductSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSaveProduct() => !IsBusy;

        private bool CanInitialize() => !IsBusy && !IsInitialized;

        partial void OnMainPreviewChanged(ImagePreview value)
        {
            OnPropertyChanged(nameof(HasMainPreview));
        }

        partial void OnSelectedBrandIdChanged(int? value)
        {
            DraftProduct.BrandId = value ?? 0;
            SelectedSeriesId = null;
            _ = LoadSeriesOptionsAsync();
        }

        partial void OnSelectedSeriesIdChanged(int? value)
        {
            DraftProduct.SeriesId = value;
        }

        partial void OnIsBusyChanged(bool value)
        {
            SaveProductCommand.NotifyCanExecuteChanged();
            InitializeCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsInitializedChanged(bool value)
        {
            InitializeCommand.NotifyCanExecuteChanged();
        }

    }
}
