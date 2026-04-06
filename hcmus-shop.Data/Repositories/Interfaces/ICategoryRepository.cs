using hcmus_shop.Data.DTOs.Categories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
