namespace hcmus_shop.Models
{
    public class ProductInstance : BaseEntity
    {
        public int InstanceId { get; set; }
        public int ProductId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;        // Unique
        public string Status { get; set; } = "Available";               // Available, Sold, Faulty, Returned

        public Product Product { get; set; } = null!;
    }
}
