using hcmus_shop.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Brands
{
    public interface IBrandService
    {
        Task<List<BrandDto>> GetAllAsync();
        Task<BrandDto?> GetByIdAsync(int brandId);
        Task<BrandDto> CreateAsync(string name, string? description = null);
        Task<BrandDto> UpdateAsync(int brandId, string? name = null, string? description = null);
        Task<bool> DeleteAsync(int brandId);
    }
}
