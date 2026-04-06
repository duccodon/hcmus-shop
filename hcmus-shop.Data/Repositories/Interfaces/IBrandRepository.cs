using hcmus_shop.Data.DTOs.Brands;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Interfaces
{
    public interface IBrandRepository
    {
        Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
