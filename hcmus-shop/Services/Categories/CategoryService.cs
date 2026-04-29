using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly IGraphQLClientService _graphQL;

        public CategoryService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<List<CategoryDto>>> GetAllAsync()
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<CategoriesResponse>(
                        CategoryQueries.GetAll
                    )
                );

            if (!result.IsSuccess)
                return Result<List<CategoryDto>>.Failure(result.Error!);

            return Result<List<CategoryDto>>.Success(result.Value!.Categories);
        }

        public async Task<Result<CategoryDto?>> GetByIdAsync(int categoryId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<CategoryResponse>(
                        CategoryQueries.GetById,
                        new { categoryId }
                    )
                );

            if (!result.IsSuccess)
                return Result<CategoryDto?>.Failure(result.Error!);

            return Result<CategoryDto?>.Success(result.Value!.Category);
        }

        public async Task<Result<CategoryDto>> CreateAsync(string name, string? description = null)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<CreateCategoryResponse>(
                        CategoryQueries.Create,
                        new { name, description }
                    )
                );

            if (!result.IsSuccess)
                return Result<CategoryDto>.Failure(result.Error!);

            return Result<CategoryDto>.Success(result.Value!.CreateCategory);
        }

        public async Task<Result<CategoryDto>> UpdateAsync(int categoryId, string? name = null, string? description = null)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<UpdateCategoryResponse>(
                        CategoryQueries.Update,
                        new { categoryId, name, description }
                    )
                );

            if (!result.IsSuccess)
                return Result<CategoryDto>.Failure(result.Error!);

            return Result<CategoryDto>.Success(result.Value!.UpdateCategory);
        }

        public async Task<Result<bool>> DeleteAsync(int categoryId)
        {
            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<DeleteCategoryResponse>(
                        CategoryQueries.Delete,
                        new { categoryId }
                    )
                );

            if (!result.IsSuccess)
                return Result<bool>.Failure(result.Error!);

            return Result<bool>.Success(true);
        }

        private class CategoriesResponse { public List<CategoryDto> Categories { get; set; } = new(); }
        private class CategoryResponse { public CategoryDto? Category { get; set; } }
        private class CreateCategoryResponse { public CategoryDto CreateCategory { get; set; } = new(); }
        private class UpdateCategoryResponse { public CategoryDto UpdateCategory { get; set; } = new(); }
        private class DeleteCategoryResponse { public CategoryDto DeleteCategory { get; set; } = new(); }
    }
}