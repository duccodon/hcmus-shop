using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class Product : BaseEntity
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;                    // Unique
        public string Name { get; set; } = string.Empty;

        public int BrandId { get; set; }
        public int? SeriesId { get; set; }                                 // Có thể null nếu là phụ kiện

        public decimal ImportPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int StockQuantity { get; set; } = 0;
        public string? Specifications { get; set; }                        // JSONB: CPU, RAM, GPU, Screen...
        public string? Description { get; set; }
        public int WarrantyMonths { get; set; } = 12;
        public bool IsActive { get; set; } = true;

        // Foreign keys and navigation properties


        public Brand Brand { get; set; } = null!;
        public Series? Series { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductInstance> Instances { get; set; } = new List<ProductInstance>();
    }
}
