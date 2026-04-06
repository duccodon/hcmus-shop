using hcmus_shop.Data.DTOs.Brands;
using hcmus_shop.Models;

namespace hcmus_shop.Data.Mappings
{
    public static class BrandMappings
    {
        public static BrandDto ToDto(this Brand brand)
            => new()
            {
                BrandId = brand.BrandId,
                Name = brand.Name,
            };
    }
}
