using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface ICategoryService
    {
        Task<Result<List<CategoryDto>>> GetAllAsync();
        Task<Result<CategoryDto?>> GetByIdAsync(int categoryId);
        Task<Result<CategoryDto>> CreateAsync(string name, string? description = null);
        Task<Result<CategoryDto>> UpdateAsync(int categoryId, string? name = null, string? description = null);
        Task<Result<bool>> DeleteAsync(int categoryId);
    }
}
