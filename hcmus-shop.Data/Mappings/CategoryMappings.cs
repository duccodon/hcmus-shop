using hcmus_shop.Data.DTOs.Categories;
using hcmus_shop.Models;

namespace hcmus_shop.Data.Mappings
{
    public static class CategoryMappings
    {
        public static CategoryDto ToDto(this Category category)
            => new()
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
            };
    }
}
