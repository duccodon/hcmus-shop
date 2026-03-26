using System;
using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class Order : BaseEntity
    {
        public Guid OrderId { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public Guid UserId { get; set; }                    // Người bán
        public int? PromotionId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; } = 0;
        public decimal FinalAmount { get; set; }

        public string Status { get; set; } = "Created";     // Created, Paid, Cancelled
        public string? Notes { get; set; }

        public Customer Customer { get; set; } = null!;
        public User User { get; set; } = null!;
        public Promotion? Promotion { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
