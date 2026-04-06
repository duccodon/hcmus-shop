using hcmus_shop.Data.DTOs.Series;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Interfaces
{
    public interface ISeriesRepository
    {
        Task<IReadOnlyList<SeriesDto>> GetByBrandIdAsync(int brandId, CancellationToken cancellationToken = default);
    }
}
