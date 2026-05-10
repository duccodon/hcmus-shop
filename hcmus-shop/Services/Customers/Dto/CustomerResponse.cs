using hcmus_shop.Models.DTOs;

namespace hcmus_shop.Services.Customers.Dto
{
    public class CustomersResponse
    {
        public CustomerPageDto Customers { get; set; } = new();
    }

    public class CustomerResponse
    {
        public CustomerDto? Customer { get; set; }
    }

    public class CreateCustomerResponse
    {
        public CustomerDto CreateCustomer { get; set; } = new();
    }

    public class UpdateCustomerResponse
    {
        public CustomerDto UpdateCustomer { get; set; } = new();
    }

    public class DeleteCustomerResponse
    {
        public bool DeleteCustomer { get; set; }
    }
}
