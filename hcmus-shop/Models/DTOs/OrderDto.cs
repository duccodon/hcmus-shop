using System.Collections.Generic;

namespace hcmus_shop.Models.DTOs
{
    public class ProductInstanceDto
    {
        public int InstanceId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
        public ProductDto? Product { get; set; }
    }

    public class ProductInstancePageDto
    {
        public List<ProductInstanceDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public ProductInstanceDto Instance { get; set; } = new();
        public double UnitSalePrice { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public CustomerDto? Customer { get; set; }
        public UserDto User { get; set; } = new();
        public List<OrderItemDto> OrderItems { get; set; } = new();
        public PromotionDto? Promotion { get; set; }
        public double Subtotal { get; set; }
        public double DiscountAmount { get; set; }
        public double FinalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? UpdatedAt { get; set; }
    }

    public class OrderPageDto
    {
        public List<OrderDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
