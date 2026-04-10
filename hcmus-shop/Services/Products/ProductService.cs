using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
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

        public async Task<ProductPageDto> GetAllAsync(ProductFilterDto filter)
        {
            var query = @"
                query Products(
                    $search: String
                    $categoryId: Int
                    $brandId: Int
                    $minPrice: Float
                    $maxPrice: Float
                    $sortBy: String
                    $sortOrder: String
                    $page: Int
                    $pageSize: Int
                ) {
                    products(
                        search: $search
                        categoryId: $categoryId
                        brandId: $brandId
                        minPrice: $minPrice
                        maxPrice: $maxPrice
                        sortBy: $sortBy
                        sortOrder: $sortOrder
                        page: $page
                        pageSize: $pageSize
                    ) {
                        items {
                            productId
                            sku
                            name
                            sellingPrice
                            importPrice
                            stockQuantity
                            isActive
                            brand { brandId name }
                            categories { categoryId name }
                            images { imageId imageUrl displayOrder }
                        }
                        totalCount
                        page
                        pageSize
                    }
                }";

            var result = await _graphQL.QueryAsync<ProductsResponse>(query, new
            {
                filter.Search,
                filter.CategoryId,
                filter.BrandId,
                filter.MinPrice,
                filter.MaxPrice,
                filter.SortBy,
                filter.SortOrder,
                filter.Page,
                filter.PageSize,
            });

            return result.Products;
        }

        public async Task<ProductDto?> GetByIdAsync(int productId)
        {
            var query = @"
                query Product($productId: Int!) {
                    product(productId: $productId) {
                        productId
                        sku
                        name
                        description
                        importPrice
                        sellingPrice
                        stockQuantity
                        warrantyMonths
                        isActive
                        brand { brandId name }
                        series { seriesId name }
                        categories { categoryId name }
                        images { imageId imageUrl displayOrder }
                    }
                }";

            var result = await _graphQL.QueryAsync<ProductResponse>(query, new { productId });
            return result.Product;
        }

        public async Task<ProductDto> CreateAsync(CreateProductInput input)
        {
            var query = @"
                mutation CreateProduct($input: CreateProductInput!) {
                    createProduct(input: $input) {
                        productId
                        sku
                        name
                        sellingPrice
                        stockQuantity
                    }
                }";

            var result = await _graphQL.MutateAsync<CreateProductResponse>(query, new { input });
            return result.CreateProduct;
        }

        public async Task<ProductDto> UpdateAsync(int productId, UpdateProductInput input)
        {
            var query = @"
                mutation UpdateProduct($productId: Int!, $input: UpdateProductInput!) {
                    updateProduct(productId: $productId, input: $input) {
                        productId
                        sku
                        name
                        sellingPrice
                        stockQuantity
                    }
                }";

            var result = await _graphQL.MutateAsync<UpdateProductResponse>(query, new { productId, input });
            return result.UpdateProduct;
        }

        public async Task<bool> DeleteAsync(int productId)
        {
            var query = @"
                mutation DeleteProduct($productId: Int!) {
                    deleteProduct(productId: $productId) {
                        productId
                    }
                }";

            await _graphQL.MutateAsync<DeleteProductResponse>(query, new { productId });
            return true;
        }

        private class ProductsResponse { public ProductPageDto Products { get; set; } = new(); }
        private class ProductResponse { public ProductDto? Product { get; set; } }
        private class CreateProductResponse { public ProductDto CreateProduct { get; set; } = new(); }
        private class UpdateProductResponse { public ProductDto UpdateProduct { get; set; } = new(); }
        private class DeleteProductResponse { public ProductDto DeleteProduct { get; set; } = new(); }
    }
}
