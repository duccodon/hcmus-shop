using hcmus_shop.Data.DTOs.Products;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<int> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    }
}
