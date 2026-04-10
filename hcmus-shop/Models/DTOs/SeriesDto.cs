namespace hcmus_shop.Models.DTOs
{
    public class SeriesDto
    {
        public int SeriesId { get; set; }
        public int BrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TargetSegment { get; set; }
    }
}
