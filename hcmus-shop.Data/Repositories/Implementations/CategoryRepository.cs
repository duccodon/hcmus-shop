using hcmus_shop.Data.DTOs.Categories;
using hcmus_shop.Data.Mappings;
using hcmus_shop.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbContextFactory<MyShopDbContext> _dbContextFactory;

        public CategoryRepository(IDbContextFactory<MyShopDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await dbContext.Categories
                .AsNoTracking()
                .OrderBy(category => category.Name)
                .Select(category => category.ToDto())
                .ToListAsync(cancellationToken);
        }
    }
}
