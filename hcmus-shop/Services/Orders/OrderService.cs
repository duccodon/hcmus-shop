using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Orders.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IGraphQLClientService _graphQL;

        public OrderService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<OrderPageDto>> GetAllAsync(OrderFilterDto filter)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<OrdersResponse>(
                    OrderQueries.GetOrders,
                    new
                    {
                        status = filter.Status,
                        fromDate = filter.FromDate,
                        toDate = filter.ToDate,
                        search = filter.Search,
                        page = filter.Page,
                        pageSize = filter.PageSize
                    }));

            if (!result.IsSuccess)
            {
                return Result<OrderPageDto>.Failure(result.Error!);
            }

            return Result<OrderPageDto>.Success(result.Value!.Orders);
        }

        public async Task<Result<OrderDto?>> GetByIdAsync(string orderId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<OrderResponse>(
                    OrderQueries.GetOrderById,
                    new { orderId }));

            if (!result.IsSuccess)
            {
                return Result<OrderDto?>.Failure(result.Error!);
            }

            return Result<OrderDto?>.Success(result.Value!.Order);
        }

        public async Task<Result<ProductInstancePageDto>> GetAvailableInstancesAsync(ProductInstanceFilterDto filter)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<AvailableProductInstancesResponse>(
                    OrderQueries.GetAvailableProductInstances,
                    new
                    {
                        search = filter.Search,
                        productId = filter.ProductId,
                        page = filter.Page,
                        pageSize = filter.PageSize
                    }));

            if (!result.IsSuccess)
            {
                return Result<ProductInstancePageDto>.Failure(result.Error!);
            }

            return Result<ProductInstancePageDto>.Success(result.Value!.AvailableProductInstances);
        }

        public async Task<Result<OrderDto>> CreateAsync(CreateOrderInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<CreateOrderResponse>(
                    OrderQueries.CreateOrder,
                    new { input }));

            if (!result.IsSuccess)
            {
                return Result<OrderDto>.Failure(result.Error!);
            }

            return Result<OrderDto>.Success(result.Value!.CreateOrder);
        }

        public async Task<Result<OrderDto>> UpdateAsync(string orderId, UpdateOrderInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<UpdateOrderResponse>(
                    OrderQueries.UpdateOrder,
                    new { orderId, input }));

            if (!result.IsSuccess)
            {
                return Result<OrderDto>.Failure(result.Error!);
            }

            return Result<OrderDto>.Success(result.Value!.UpdateOrder);
        }

        public async Task<Result<OrderDto>> UpdateStatusAsync(string orderId, string status)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<UpdateOrderStatusResponse>(
                    OrderQueries.UpdateOrderStatus,
                    new { orderId, status }));

            if (!result.IsSuccess)
            {
                return Result<OrderDto>.Failure(result.Error!);
            }

            return Result<OrderDto>.Success(result.Value!.UpdateOrderStatus);
        }

        public async Task<Result<bool>> DeleteAsync(string orderId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<DeleteOrderResponse>(
                    OrderQueries.DeleteOrder,
                    new { orderId }));

            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Error!);
            }

            return Result<bool>.Success(result.Value!.DeleteOrder);
        }
    }
}
