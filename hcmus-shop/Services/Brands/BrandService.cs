using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Brands
{
    public class BrandService : IBrandService
    {
        private readonly IGraphQLClientService _graphQL;

        public BrandService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<List<BrandDto>> GetAllAsync()
        {
            var query = @"
                query {
                    brands {
                        brandId
                        name
                        description
                        productCount
                    }
                }";

            var result = await _graphQL.QueryAsync<BrandsResponse>(query);
            return result.Brands;
        }

        public async Task<BrandDto?> GetByIdAsync(int brandId)
        {
            var query = @"
                query Brand($brandId: Int!) {
                    brand(brandId: $brandId) {
                        brandId
                        name
                        description
                        series { seriesId name targetSegment }
                    }
                }";

            var result = await _graphQL.QueryAsync<BrandResponse>(query, new { brandId });
            return result.Brand;
        }

        public async Task<BrandDto> CreateAsync(string name, string? description = null)
        {
            var query = @"
                mutation CreateBrand($name: String!, $description: String) {
                    createBrand(name: $name, description: $description) {
                        brandId
                        name
                        description
                    }
                }";

            var result = await _graphQL.MutateAsync<CreateBrandResponse>(query, new { name, description });
            return result.CreateBrand;
        }

        public async Task<BrandDto> UpdateAsync(int brandId, string? name = null, string? description = null)
        {
            var query = @"
                mutation UpdateBrand($brandId: Int!, $name: String, $description: String) {
                    updateBrand(brandId: $brandId, name: $name, description: $description) {
                        brandId
                        name
                        description
                    }
                }";

            var result = await _graphQL.MutateAsync<UpdateBrandResponse>(query, new { brandId, name, description });
            return result.UpdateBrand;
        }

        public async Task<bool> DeleteAsync(int brandId)
        {
            var query = @"
                mutation DeleteBrand($brandId: Int!) {
                    deleteBrand(brandId: $brandId) {
                        brandId
                    }
                }";

            await _graphQL.MutateAsync<DeleteBrandResponse>(query, new { brandId });
            return true;
        }

        private class BrandsResponse { public List<BrandDto> Brands { get; set; } = new(); }
        private class BrandResponse { public BrandDto? Brand { get; set; } }
        private class CreateBrandResponse { public BrandDto CreateBrand { get; set; } = new(); }
        private class UpdateBrandResponse { public BrandDto UpdateBrand { get; set; } = new(); }
        private class DeleteBrandResponse { public BrandDto DeleteBrand { get; set; } = new(); }
    }
}
