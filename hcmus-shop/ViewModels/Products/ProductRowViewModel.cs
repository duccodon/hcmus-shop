using CommunityToolkit.Mvvm.ComponentModel;

namespace hcmus_shop.ViewModels.Products
{
    public class ProductRowViewModel : ObservableObject
    {
        private bool _isSelected;
        private bool _isActive;

        public ProductRowViewModel(
            int productId,
            string sku,
            string name,
            string categoryDisplay,
            int stockQuantity,
            decimal sellingPrice,
            bool isActive)
        {
            ProductId = productId;
            Sku = sku;
            Name = name;
            CategoryDisplay = categoryDisplay;
            StockQuantity = stockQuantity;
            SellingPrice = sellingPrice;
            _isActive = isActive;
        }

        public int ProductId { get; }
        public string Sku { get; }
        public string Name { get; }
        public string CategoryDisplay { get; }
        public int StockQuantity { get; }
        public decimal SellingPrice { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusTone));
                }
            }
        }

        public string PriceDisplay => $"${SellingPrice:0}";

        public string StockLabel => StockQuantity <= 0
            ? "Out of Stock"
            : StockQuantity < 10 ? "Low Stock" : string.Empty;

        public string StockTone => StockQuantity <= 0
            ? "Danger"
            : StockQuantity < 10 ? "Warning" : "None";

        public string StatusText => IsActive
            ? "Published"
            : StockQuantity <= 0 ? "Stock Out" : "Inactive";

        public string StatusTone => StatusText switch
        {
            "Published" => "Published",
            "Inactive" => "Inactive",
            _ => "StockOut",
        };
    }
}
