using System;

namespace hcmus_shop.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public Guid OrderId { get; set; }
        public int InstanceId { get; set; }                 // Bán theo serial
        public decimal UnitSalePrice { get; set; }
        public int Quantity { get; set; } = 1;

        public Order Order { get; set; } = null!;
        public ProductInstance Instance { get; set; } = null!;
    }
}
