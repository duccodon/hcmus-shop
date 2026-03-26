using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class Category : BaseEntity
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
