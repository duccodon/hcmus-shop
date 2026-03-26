using System;
using System.Collections.Generic;

namespace hcmus_shop.Models
{
    public class User : BaseEntity
    {
        public Guid UserId { get; set; } = Guid.NewGuid();              // UUID
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Sale";                      // Admin, Sale, Manager

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
