using hcmus_shop.Models.DTOs;

namespace hcmus_shop.Services.Orders.Dto
{
    public class OrdersResponse
    {
        public OrderPageDto Orders { get; set; } = new();
    }

    public class OrderResponse
    {
        public OrderDto? Order { get; set; }
    }

    public class AvailableProductInstancesResponse
    {
        public ProductInstancePageDto AvailableProductInstances { get; set; } = new();
    }

    public class CreateOrderResponse
    {
        public OrderDto CreateOrder { get; set; } = new();
    }

    public class UpdateOrderResponse
    {
        public OrderDto UpdateOrder { get; set; } = new();
    }

    public class UpdateOrderStatusResponse
    {
        public OrderDto UpdateOrderStatus { get; set; } = new();
    }

    public class DeleteOrderResponse
    {
        public bool DeleteOrder { get; set; }
    }
}
