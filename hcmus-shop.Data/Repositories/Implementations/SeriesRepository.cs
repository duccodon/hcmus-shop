using hcmus_shop.Data.DTOs.Series;
using hcmus_shop.Data.Mappings;
using hcmus_shop.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Implementations
{
    public class SeriesRepository : ISeriesRepository
    {
        private readonly IDbContextFactory<MyShopDbContext> _dbContextFactory;

        public SeriesRepository(IDbContextFactory<MyShopDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IReadOnlyList<SeriesDto>> GetByBrandIdAsync(int brandId, CancellationToken cancellationToken = default)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await dbContext.Series
                .AsNoTracking()
                .Where(series => series.BrandId == brandId)
                .OrderBy(series => series.Name)
                .Select(series => series.ToDto())
                .ToListAsync(cancellationToken);
        }
    }
}
