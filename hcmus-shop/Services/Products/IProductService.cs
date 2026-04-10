using hcmus_shop.Models.DTOs;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Products
{
    public interface IProductService
    {
        Task<ProductPageDto> GetAllAsync(ProductFilterDto filter);
        Task<ProductDto?> GetByIdAsync(int productId);
        Task<ProductDto> CreateAsync(CreateProductInput input);
        Task<ProductDto> UpdateAsync(int productId, UpdateProductInput input);
        Task<bool> DeleteAsync(int productId);
    }
}
