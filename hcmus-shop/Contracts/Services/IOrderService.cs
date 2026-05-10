using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Orders.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IOrderService
    {
        Task<Result<OrderPageDto>> GetAllAsync(OrderFilterDto filter);
        Task<Result<OrderDto?>> GetByIdAsync(string orderId);
        Task<Result<ProductInstancePageDto>> GetAvailableInstancesAsync(ProductInstanceFilterDto filter);
        Task<Result<OrderDto>> CreateAsync(CreateOrderInput input);
        Task<Result<OrderDto>> UpdateAsync(string orderId, UpdateOrderInput input);
        Task<Result<OrderDto>> UpdateStatusAsync(string orderId, string status);
        Task<Result<bool>> DeleteAsync(string orderId);
    }
}
