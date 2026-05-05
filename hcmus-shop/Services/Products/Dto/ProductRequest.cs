using System.Collections.Generic;

namespace hcmus_shop.Services.Products.Dto
{
    public class ProductSortCriterionDto
    {
        public string Field { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
    }

    public class ProductFilterDto
    {
        public string? Search { get; set; }
        public string? Name { get; set; }
        public string? Sku { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? BrandIds { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; }
        public List<ProductSortCriterionDto>? Sorts { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreateProductInput
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BrandId { get; set; }
        public int? SeriesId { get; set; }
        public double ImportPrice { get; set; }
        public double SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Specifications { get; set; }
        public string? Description { get; set; }
        public int WarrantyMonths { get; set; } = 12;
        public List<int>? CategoryIds { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class UpdateProductInput
    {
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public int? BrandId { get; set; }
        public int? SeriesId { get; set; }
        public double? ImportPrice { get; set; }
        public double? SellingPrice { get; set; }
        public int? StockQuantity { get; set; }
        public string? Description { get; set; }
        public int? WarrantyMonths { get; set; }
        public bool? IsActive { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<string>? ImageUrls { get; set; }
    }

    public class GetProductsRequest
    {
        public string? Search { get; set; }
        public string? Name { get; set; }
        public string? Sku { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? BrandIds { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public bool? InStockOnly { get; set; }
        public List<ProductSortCriterionDto>? Sorts { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class GetProductByIdRequest
    {
        public int ProductId { get; set; }
    }

    public class DeleteProductRequest
    {
        public int ProductId { get; set; }
    }
}
