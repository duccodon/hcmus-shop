using hcmus_shop.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Series
{
    public interface ISeriesService
    {
        Task<List<SeriesDto>> GetByBrandAsync(int brandId);
        Task<SeriesDto?> GetByIdAsync(int seriesId);
        Task<SeriesDto> CreateAsync(int brandId, string name, string? description = null, string? targetSegment = null);
        Task<bool> DeleteAsync(int seriesId);
    }
}
