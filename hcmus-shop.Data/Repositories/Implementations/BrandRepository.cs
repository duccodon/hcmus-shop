using hcmus_shop.Data.DTOs.Brands;
using hcmus_shop.Data.Mappings;
using hcmus_shop.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Implementations
{
    public class BrandRepository : IBrandRepository
    {
        private readonly IDbContextFactory<MyShopDbContext> _dbContextFactory;

        public BrandRepository(IDbContextFactory<MyShopDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IReadOnlyList<BrandDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await dbContext.Brands
                .AsNoTracking()
                .OrderBy(brand => brand.Name)
                .Select(brand => brand.ToDto())
                .ToListAsync(cancellationToken);
        }
    }
}
