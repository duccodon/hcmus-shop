using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface ISeriesService
    {
        Task<Result<List<SeriesDto>>> GetByBrandAsync(int brandId);
        Task<Result<SeriesDto?>> GetByIdAsync(int seriesId);
        Task<Result<SeriesDto>> CreateAsync(int brandId, string name, string? description = null, string? targetSegment = null);
        Task<Result<bool>> DeleteAsync(int seriesId);
    }
}
