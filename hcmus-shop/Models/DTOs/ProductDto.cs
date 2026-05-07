using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BrandId { get; set; }
        public int? SeriesId { get; set; }
        public double ImportPrice { get; set; }
        public double SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public int WarrantyMonths { get; set; }
        public bool IsActive { get; set; }
        public BrandDto? Brand { get; set; }
        public SeriesDto? Series { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
        public List<ProductImageDto> Images { get; set; } = new();
    }

    public class ProductImageDto
    {
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class ProductPageDto
    {
        public List<ProductDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
