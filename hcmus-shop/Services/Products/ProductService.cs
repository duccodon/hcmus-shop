using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using hcmus_shop.Services.Products.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IGraphQLClientService _graphQL;

        public ProductService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<ProductPageDto>> GetAllAsync(ProductFilterDto filter)
        {
            var request = new GetProductsRequest
            {
                Search = filter.Search,
                Name = filter.Name,
                Sku = filter.Sku,
                CategoryId = filter.CategoryId,
                BrandId = filter.BrandId,
                CategoryIds = filter.CategoryIds,
                BrandIds = filter.BrandIds,
                MinPrice = filter.MinPrice,
                MaxPrice = filter.MaxPrice,
                InStockOnly = filter.InStockOnly,
                Sorts = filter.Sorts,
                SortBy = filter.SortBy,
                SortOrder = filter.SortOrder,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            var result = await _graphQL.SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<ProductsResponse>(
                        ProductQueries.GetProducts,
                        request
                    ));

            if (!result.IsSuccess)
                return Result<ProductPageDto>.Failure(result.Error!);

            return Result<ProductPageDto>.Success(result.Value!.Products);
        }

        public async Task<Result<ProductDto?>> GetByIdAsync(int productId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<ProductResponse>(
                        ProductQueries.GetById,
                        new { productId }
                    ));

            if (!result.IsSuccess)
                return Result<ProductDto?>.Failure(result.Error!);

            return Result<ProductDto?>.Success(result.Value!.Product);
        }

        public async Task<Result<ProductDto>> CreateAsync(CreateProductInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<CreateProductResponse>(
                        ProductQueries.Create,
                        new { input }
                    ));

            if (!result.IsSuccess)
                return Result<ProductDto>.Failure(result.Error!);

            return Result<ProductDto>.Success(result.Value!.CreateProduct);
        }

        public async Task<Result<ProductDto>> UpdateAsync(int productId, UpdateProductInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<UpdateProductResponse>(
                        ProductQueries.Update,
                        new { productId, input }
                    ));

            if (!result.IsSuccess)
                return Result<ProductDto>.Failure(result.Error!);

            return Result<ProductDto>.Success(result.Value!.UpdateProduct);
        }

        public async Task<Result<bool>> DeleteAsync(int productId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<DeleteProductResponse>(
                        ProductQueries.Delete,
                        new { productId }
                    ));

            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Error!);

            return Result<bool>.Success(true);
        }
    }
}
