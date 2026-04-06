using hcmus_shop.Data.DTOs.Products;
using hcmus_shop.Data.Mappings;
using hcmus_shop.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace hcmus_shop.Data.Repositories.Implementations
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbContextFactory<MyShopDbContext> _dbContextFactory;

        public ProductRepository(IDbContextFactory<MyShopDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<int> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var brandExists = await dbContext.Brands
                .AsNoTracking()
                .AnyAsync(brand => brand.BrandId == dto.BrandId, cancellationToken);

            if (!brandExists)
            {
                throw new InvalidOperationException("The selected brand does not exist.");
            }

            var skuExists = await dbContext.Products
                .AsNoTracking()
                .AnyAsync(product => product.Sku == dto.Sku, cancellationToken);

            if (skuExists)
            {
                throw new InvalidOperationException("SKU already exists. Please use a different SKU.");
            }

            if (dto.SeriesId.HasValue)
            {
                var seriesExists = await dbContext.Series
                    .AsNoTracking()
                    .AnyAsync(series => series.SeriesId == dto.SeriesId.Value && series.BrandId == dto.BrandId, cancellationToken);

                if (!seriesExists)
                {
                    throw new InvalidOperationException("The selected series is not valid for the selected brand.");
                }
            }

            var product = dto.ToEntity();

            if (dto.CategoryIds.Count > 0)
            {
                var categories = await dbContext.Categories
                    .Where(category => dto.CategoryIds.Contains(category.CategoryId))
                    .ToListAsync(cancellationToken);

                foreach (var category in categories)
                {
                    product.Categories.Add(category);
                }
            }

            dbContext.Products.Add(product);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresEx)
            {
                throw postgresEx.SqlState switch
                {
                    PostgresErrorCodes.ForeignKeyViolation => new InvalidOperationException("Invalid brand, series, or category reference."),
                    PostgresErrorCodes.UniqueViolation => new InvalidOperationException("Product SKU must be unique."),
                    PostgresErrorCodes.InvalidTextRepresentation => new InvalidOperationException("Specifications must be valid JSON. Use '{}' or leave it empty."),
                    _ => new InvalidOperationException($"Database error: {postgresEx.MessageText}"),
                };
            }

            return product.ProductId;
        }
    }
}
