using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IBrandService
    {
        Task<Result<List<BrandDto>>> GetAllAsync();
        Task<Result<BrandDto?>> GetByIdAsync(int brandId);
        Task<Result<BrandDto>> CreateAsync(string name, string? description = null);
        Task<Result<BrandDto>> UpdateAsync(int brandId, string? name = null, string? description = null);
        Task<Result<bool>> DeleteAsync(int brandId);
    }
}
