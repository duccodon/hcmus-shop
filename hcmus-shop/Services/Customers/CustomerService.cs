using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Customers.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Customers
{
    public class CustomerService : ICustomerService
    {
        private readonly IGraphQLClientService _graphQL;

        public CustomerService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public async Task<Result<CustomerPageDto>> GetAllAsync(CustomerFilterDto filter)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<CustomersResponse>(
                    CustomerQueries.GetCustomers,
                    new GetCustomersRequest
                    {
                        Search = filter.Search,
                        Page = filter.Page,
                        PageSize = filter.PageSize
                    }));

            if (!result.IsSuccess)
            {
                return Result<CustomerPageDto>.Failure(result.Error!);
            }

            return Result<CustomerPageDto>.Success(result.Value!.Customers);
        }

        public async Task<Result<CustomerDto?>> GetByIdAsync(string customerId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.QueryAsync<CustomerResponse>(
                    CustomerQueries.GetCustomerById,
                    new { customerId }));

            if (!result.IsSuccess)
            {
                return Result<CustomerDto?>.Failure(result.Error!);
            }

            return Result<CustomerDto?>.Success(result.Value!.Customer);
        }

        public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<CreateCustomerResponse>(
                    CustomerQueries.CreateCustomer,
                    new { input }));

            if (!result.IsSuccess)
            {
                return Result<CustomerDto>.Failure(result.Error!);
            }

            return Result<CustomerDto>.Success(result.Value!.CreateCustomer);
        }

        public async Task<Result<CustomerDto>> UpdateAsync(string customerId, UpdateCustomerInput input)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<UpdateCustomerResponse>(
                    CustomerQueries.UpdateCustomer,
                    new { customerId, input }));

            if (!result.IsSuccess)
            {
                return Result<CustomerDto>.Failure(result.Error!);
            }

            return Result<CustomerDto>.Success(result.Value!.UpdateCustomer);
        }

        public async Task<Result<bool>> DeleteAsync(string customerId)
        {
            var result = await _graphQL.SafeExecuteAsync(() =>
                _graphQL.MutateAsync<DeleteCustomerResponse>(
                    CustomerQueries.DeleteCustomer,
                    new { customerId }));

            if (!result.IsSuccess)
            {
                return Result<bool>.Failure(result.Error!);
            }

            return Result<bool>.Success(result.Value!.DeleteCustomer);
        }
    }
}
