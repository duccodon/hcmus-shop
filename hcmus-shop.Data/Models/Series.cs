using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class Series : BaseEntity
    {
        public int SeriesId { get; set; }
        public int BrandId { get; set; }
        public string Name { get; set; } = string.Empty;           // ROG, TUF, VivoBook, Legion...
        public string? Description { get; set; }
        public string? TargetSegment { get; set; }                 // Gaming, Business, Student...

        public Brand Brand { get; set; } = null!;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
