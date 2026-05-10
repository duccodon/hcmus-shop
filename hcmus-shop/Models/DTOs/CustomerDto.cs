using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class CustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int LoyaltyPoints { get; set; }
        public string Rank { get; set; } = "Bronze";
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
    }

    public class CustomerPageDto
    {
        public List<CustomerDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
