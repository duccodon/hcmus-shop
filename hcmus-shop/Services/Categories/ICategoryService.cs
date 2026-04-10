using hcmus_shop.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Categories
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int categoryId);
        Task<CategoryDto> CreateAsync(string name, string? description = null);
        Task<CategoryDto> UpdateAsync(int categoryId, string? name = null, string? description = null);
        Task<bool> DeleteAsync(int categoryId);
    }
}
