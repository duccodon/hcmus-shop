using System;
using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class Customer : BaseEntity
    {
        public Guid CustomerId { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int LoyaltyPoints { get; set; } = 0;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
