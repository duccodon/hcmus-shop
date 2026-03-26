using System;

namespace hcmus_shop.Models
{
    public class InventoryLog : BaseEntity
    {
        public int LogId { get; set; }
        public int ProductId { get; set; }
        public int? InstanceId { get; set; }
        public Guid UserId { get; set; }
        public int QuantityChange { get; set; }
        public string ChangeType { get; set; } = string.Empty;   // Import, Export, Adjust, Return
        public string? Reason { get; set; }

        public Product Product { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
