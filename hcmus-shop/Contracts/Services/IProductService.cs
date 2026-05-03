using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IProductService
    {
        Task<Result<ProductPageDto>> GetAllAsync(ProductFilterDto filter);
        Task<Result<ProductDto?>> GetByIdAsync(int productId);
        Task<Result<ProductDto>> CreateAsync(CreateProductInput input);
        Task<Result<ProductDto>> UpdateAsync(int productId, UpdateProductInput input);
        Task<Result<bool>> DeleteAsync(int productId);
    }
}
