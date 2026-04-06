using System.Collections.Generic;

namespace hcmus_shop.Data.DTOs.Products
{
    public class CreateProductDto
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Specifications { get; set; }
        public decimal ImportPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int WarrantyMonths { get; set; }
        public bool IsActive { get; set; } = true;
        public int StockQuantity { get; set; }
        public int BrandId { get; set; }
        public int? SeriesId { get; set; }
        public List<int> CategoryIds { get; set; } = [];
        public List<string> ImageUrls { get; set; } = [];
    }
}
