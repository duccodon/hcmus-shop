using hcmus_shop.Data.DTOs.Products;
using hcmus_shop.Models;
using System.Linq;

namespace hcmus_shop.Data.Mappings
{
    public static class ProductMappings
    {
        public static Product ToEntity(this CreateProductDto dto)
            => new()
            {
                Sku = dto.Sku,
                Name = dto.Name,
                Description = dto.Description,
                Specifications = dto.Specifications,
                ImportPrice = dto.ImportPrice,
                SellingPrice = dto.SellingPrice,
                WarrantyMonths = dto.WarrantyMonths,
                IsActive = dto.IsActive,
                StockQuantity = dto.StockQuantity,
                BrandId = dto.BrandId,
                SeriesId = dto.SeriesId,
                Images = dto.ImageUrls
                    .Select((url, index) => new ProductImage
                    {
                        ImageUrl = url,
                        DisplayOrder = index,
                    })
                    .ToList(),
            };
    }
}
