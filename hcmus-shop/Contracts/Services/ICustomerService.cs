using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Customers.Dto;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface ICustomerService
    {
        Task<Result<CustomerPageDto>> GetAllAsync(CustomerFilterDto filter);
        Task<Result<CustomerDto?>> GetByIdAsync(string customerId);
        Task<Result<CustomerDto>> CreateAsync(CreateCustomerInput input);
        Task<Result<CustomerDto>> UpdateAsync(string customerId, UpdateCustomerInput input);
        Task<Result<bool>> DeleteAsync(string customerId);
    }
}
