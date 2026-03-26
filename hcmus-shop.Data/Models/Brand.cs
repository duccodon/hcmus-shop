using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class Brand : BaseEntity
    {
        public int BrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Series> Series { get; set; } = new List<Series>();
    }
}
