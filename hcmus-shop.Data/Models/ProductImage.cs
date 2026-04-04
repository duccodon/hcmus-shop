namespace hcmus_shop.Models
{
    public class ProductImage : BaseEntity
    {
        [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
        public ProductImage()
        {
            ImageUrl = string.Empty;
        }

        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public required string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public Product Product { get; set; } = null!;
    }
}
