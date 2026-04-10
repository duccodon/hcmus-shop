using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class BrandDto
    {
        public int BrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public int ProductCount { get; set; }
        public List<SeriesDto> Series { get; set; } = new();
    }
}
