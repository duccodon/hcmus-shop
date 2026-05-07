using System.Collections.Generic;

namespace hcmus_shop.ViewModels.Products
{
    public class AddProductDraft
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Specifications { get; set; } = string.Empty;
        public double ImportPrice { get; set; }
        public double SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public int WarrantyMonths { get; set; } = 12;
        public int? SelectedBrandId { get; set; }
        public int? SelectedSeriesId { get; set; }
        public List<int> SelectedCategoryIds { get; set; } = [];
        public List<string> ImageFilePaths { get; set; } = [];
    }
}
