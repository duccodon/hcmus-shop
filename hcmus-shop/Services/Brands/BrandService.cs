using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
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

        public async Task<Result<List<BrandDto>>> GetAllAsync()
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<BrandsResponse>(
                        BrandQueries.GetAll
                    )
                );

            if (!result.IsSuccess)
                return Result<List<BrandDto>>.Failure(result.Error!);

            return Result<List<BrandDto>>.Success(result.Value!.Brands);
        }

        public async Task<Result<BrandDto?>> GetByIdAsync(int brandId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<BrandResponse>(
                        BrandQueries.GetById,
                        new { brandId }
                    )
                );

            if (!result.IsSuccess)
                return Result<BrandDto?>.Failure(result.Error!);

            return Result<BrandDto?>.Success(result.Value!.Brand);
        }

        public async Task<Result<BrandDto>> CreateAsync(string name, string? description = null)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<CreateBrandResponse>(
                        BrandQueries.Create,
                        new { name, description }
                    )
                );

            if (!result.IsSuccess)
                return Result<BrandDto>.Failure(result.Error!);

            return Result<BrandDto>.Success(result.Value!.CreateBrand);
        }

        public async Task<Result<BrandDto>> UpdateAsync(int brandId, string? name = null, string? description = null)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<UpdateBrandResponse>(
                        BrandQueries.Update,
                        new { brandId, name, description }
                    )
                );

            if (!result.IsSuccess)
                return Result<BrandDto>.Failure(result.Error!);

            return Result<BrandDto>.Success(result.Value!.UpdateBrand);
        }

        public async Task<Result<bool>> DeleteAsync(int brandId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<DeleteBrandResponse>(
                        BrandQueries.Delete,
                        new { brandId }
                    )
                );

            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Error!);

            return Result<bool>.Success(true);
        }

        // Private response wrappers
        private class BrandsResponse { public List<BrandDto> Brands { get; set; } = new(); }
        private class BrandResponse { public BrandDto? Brand { get; set; } }
        private class CreateBrandResponse { public BrandDto CreateBrand { get; set; } = new(); }
        private class UpdateBrandResponse { public BrandDto UpdateBrand { get; set; } = new(); }
        private class DeleteBrandResponse { public BrandDto DeleteBrand { get; set; } = new(); }
    }
}