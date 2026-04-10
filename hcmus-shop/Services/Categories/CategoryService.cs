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

        public async Task<List<CategoryDto>> GetAllAsync()
        {
            var query = @"
                query {
                    categories {
                        categoryId
                        name
                        description
                        productCount
                    }
                }";

            var result = await _graphQL.QueryAsync<CategoriesResponse>(query);
            return result.Categories;
        }

        public async Task<CategoryDto?> GetByIdAsync(int categoryId)
        {
            var query = @"
                query Category($categoryId: Int!) {
                    category(categoryId: $categoryId) {
                        categoryId
                        name
                        description
                    }
                }";

            var result = await _graphQL.QueryAsync<CategoryResponse>(query, new { categoryId });
            return result.Category;
        }

        public async Task<CategoryDto> CreateAsync(string name, string? description = null)
        {
            var query = @"
                mutation CreateCategory($name: String!, $description: String) {
                    createCategory(name: $name, description: $description) {
                        categoryId
                        name
                        description
                    }
                }";

            var result = await _graphQL.MutateAsync<CreateCategoryResponse>(query, new { name, description });
            return result.CreateCategory;
        }

        public async Task<CategoryDto> UpdateAsync(int categoryId, string? name = null, string? description = null)
        {
            var query = @"
                mutation UpdateCategory($categoryId: Int!, $name: String, $description: String) {
                    updateCategory(categoryId: $categoryId, name: $name, description: $description) {
                        categoryId
                        name
                        description
                    }
                }";

            var result = await _graphQL.MutateAsync<UpdateCategoryResponse>(query, new { categoryId, name, description });
            return result.UpdateCategory;
        }

        public async Task<bool> DeleteAsync(int categoryId)
        {
            var query = @"
                mutation DeleteCategory($categoryId: Int!) {
                    deleteCategory(categoryId: $categoryId) {
                        categoryId
                    }
                }";

            await _graphQL.MutateAsync<DeleteCategoryResponse>(query, new { categoryId });
            return true;
        }

        private class CategoriesResponse { public List<CategoryDto> Categories { get; set; } = new(); }
        private class CategoryResponse { public CategoryDto? Category { get; set; } }
        private class CreateCategoryResponse { public CategoryDto CreateCategory { get; set; } = new(); }
        private class UpdateCategoryResponse { public CategoryDto UpdateCategory { get; set; } = new(); }
        private class DeleteCategoryResponse { public CategoryDto DeleteCategory { get; set; } = new(); }
    }
}
