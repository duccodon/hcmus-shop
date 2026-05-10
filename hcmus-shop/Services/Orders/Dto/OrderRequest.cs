using System.Collections.Generic;

namespace hcmus_shop.Services.Orders.Dto
{
    public class OrderFilterDto
    {
        public string? Status { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ProductInstanceFilterDto
    {
        public string? Search { get; set; }
        public int? ProductId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 200;
    }

    public class OrderItemInput
    {
        public int InstanceId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class CreateOrderInput
    {
        public string CustomerId { get; set; } = string.Empty;
        public string? PromotionCode { get; set; }
        public List<OrderItemInput> Items { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class UpdateOrderInput
    {
        public string? CustomerId { get; set; }
        public string? PromotionCode { get; set; }
        public List<OrderItemInput>? Items { get; set; }
        public string? Notes { get; set; }
    }
}
