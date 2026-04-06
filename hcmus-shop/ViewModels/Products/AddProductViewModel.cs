using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hcmus_shop.Models;
using hcmus_shop.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace hcmus_shop.ViewModels.Products
{
    public class AddProductViewModel : ObservableObject
    {
        private ImagePreview _mainPreview = new();

        public AddProductViewModel()
        {
            SelectPreviewCommand = new RelayCommand<ImagePreview?>(SelectPreview);
            AddCategoryCommand = new RelayCommand(AddCategory);

            DraftProduct = new Product
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

            CategoryOptions =
            [
                new CategoryOptionViewModel("Jacket"),
                new CategoryOptionViewModel("Accessories"),
                new CategoryOptionViewModel("Clothes"),
                new CategoryOptionViewModel("Shoes"),
                new CategoryOptionViewModel("Electronics"),
                new CategoryOptionViewModel("Beauty"),
            ];

            foreach (var option in CategoryOptions)
            {
                option.PropertyChanged += CategoryOption_PropertyChanged;
            }
        }

        public Product DraftProduct { get; }

        public ObservableCollection<ImagePreview> PreviewImages { get; } = [];

        public ObservableCollection<CategoryOptionViewModel> CategoryOptions { get; }

        public IRelayCommand<ImagePreview?> SelectPreviewCommand { get; }

        public IRelayCommand AddCategoryCommand { get; }

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

        private void CategoryOption_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(CategoryOptionViewModel.IsSelected))
            {
                return;
            }

            DraftProduct.Categories.Clear();

            var selected = CategoryOptions
                .Where(option => option.IsSelected)
                .Select(option => new Category { Name = option.Name });

            foreach (var category in selected)
            {
                DraftProduct.Categories.Add(category);
            }
        }
    }
}
