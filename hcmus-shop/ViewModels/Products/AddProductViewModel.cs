using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Data.DTOs.Products;
using hcmus_shop.Data.Repositories.Interfaces;
using hcmus_shop.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace hcmus_shop.ViewModels.Products
{
    public class AddProductViewModel : ObservableObject
    {
        private readonly IProductRepository _productRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISeriesRepository _seriesRepository;
        private ImagePreview _mainPreview = new();
        private int? _selectedBrandId;
        private int? _selectedSeriesId;
        private bool _isSaving;
        private bool _isInitialized;
        private string _saveErrorMessage = string.Empty;

        public AddProductViewModel(
            IProductRepository productRepository,
            IBrandRepository brandRepository,
            ICategoryRepository categoryRepository,
            ISeriesRepository seriesRepository)
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
            _seriesRepository = seriesRepository;

            SelectPreviewCommand = new RelayCommand<ImagePreview?>(SelectPreview);
            AddCategoryCommand = new RelayCommand(AddCategory);
            SaveProductCommand = new AsyncRelayCommand(SaveProductAsync, () => !IsSaving);
            InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsInitialized);

            DraftProduct = new CreateProductDto
            {
                Sku = string.Empty,
                Name = string.Empty,
                Description = string.Empty,
                Specifications = string.Empty,
                WarrantyMonths = 12,
                ImportPrice = 0,
                SellingPrice = 0,
                IsActive = true,
                BrandId = 0,
            };
        }

        public CreateProductDto DraftProduct { get; }

        public ObservableCollection<ImagePreview> PreviewImages { get; } = [];

        public ObservableCollection<LookupOptionViewModel> BrandOptions { get; } = [];

        public ObservableCollection<LookupOptionViewModel> SeriesOptions { get; } = [];

        public ObservableCollection<CategoryOptionViewModel> CategoryOptions { get; } = [];

        public IRelayCommand<ImagePreview?> SelectPreviewCommand { get; }

        public IRelayCommand AddCategoryCommand { get; }

        public IAsyncRelayCommand SaveProductCommand { get; }

        public IAsyncRelayCommand InitializeCommand { get; }

        public event EventHandler? ProductSaved;

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

        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (SetProperty(ref _isSaving, value))
                {
                    SaveProductCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string SaveErrorMessage
        {
            get => _saveErrorMessage;
            private set => SetProperty(ref _saveErrorMessage, value);
        }

        public int? SelectedBrandId
        {
            get => _selectedBrandId;
            set
            {
                if (SetProperty(ref _selectedBrandId, value))
                {
                    DraftProduct.BrandId = value ?? 0;
                    SelectedSeriesId = null;
                    _ = LoadSeriesOptionsAsync();
                }
            }
        }

        public int? SelectedSeriesId
        {
            get => _selectedSeriesId;
            set
            {
                if (SetProperty(ref _selectedSeriesId, value))
                {
                    DraftProduct.SeriesId = value;
                }
            }
        }

        public ImagePreview MainPreview
        {
            get => _mainPreview;
            private set
            {
                if (SetProperty(ref _mainPreview, value))
                {
                    OnPropertyChanged(nameof(HasMainPreview));
                }
            }
        }

        public bool HasMainPreview => MainPreview.Bitmap is not null;

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

        public void AddImagePreview(ImagePreview preview)
        {
            preview.DisplayOrder = PreviewImages.Count;
            PreviewImages.Add(preview);

            if (!HasMainPreview)
            {
                MainPreview = preview;
            }
        }

        private void SelectPreview(ImagePreview? preview)
        {
            if (preview is null)
            {
                return;
            }

            MainPreview = preview;
        }

        private void AddCategory()
        {
        }

        private async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                return;
            }

            var brands = await _brandRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();

            BrandOptions.Clear();
            foreach (var brand in brands)
            {
                BrandOptions.Add(new LookupOptionViewModel(brand.BrandId, brand.Name));
            }

            CategoryOptions.Clear();
            foreach (var category in categories)
            {
                CategoryOptions.Add(new CategoryOptionViewModel(category.CategoryId, category.Name));
            }

            SelectedBrandId = BrandOptions.FirstOrDefault()?.Id;
            IsInitialized = true;
        }

        private async Task LoadSeriesOptionsAsync()
        {
            SeriesOptions.Clear();
            if (!SelectedBrandId.HasValue)
            {
                return;
            }

            var seriesItems = await _seriesRepository.GetByBrandIdAsync(SelectedBrandId.Value);
            foreach (var series in seriesItems)
            {
                SeriesOptions.Add(new LookupOptionViewModel(series.SeriesId, series.Name));
            }
        }

        private async Task SaveProductAsync()
        {
            if (IsSaving)
            {
                return;
            }

            SaveErrorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(DraftProduct.Sku) || string.IsNullOrWhiteSpace(DraftProduct.Name))
            {
                SaveErrorMessage = "SKU and product name are required.";
                return;
            }

            if (DraftProduct.BrandId <= 0)
            {
                SaveErrorMessage = "Please select a brand.";
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

            IsSaving = true;
            try
            {
                await _productRepository.CreateAsync(DraftProduct);
                ProductSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SaveErrorMessage = ex.Message;
            }
            finally
            {
                IsSaving = false;
            }
        }

    }
}
