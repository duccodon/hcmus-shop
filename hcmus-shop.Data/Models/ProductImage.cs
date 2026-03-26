namespace hcmus_shop.Models
{
    public class ProductImage : BaseEntity
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public required string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public Product Product { get; set; } = null!;
    }
}
