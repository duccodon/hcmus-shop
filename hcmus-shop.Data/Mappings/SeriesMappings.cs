using hcmus_shop.Data.DTOs.Series;
using hcmus_shop.Models;

namespace hcmus_shop.Data.Mappings
{
    public static class SeriesMappings
    {
        public static SeriesDto ToDto(this Series series)
            => new()
            {
                SeriesId = series.SeriesId,
                Name = series.Name,
                BrandId = series.BrandId,
            };
    }
}
