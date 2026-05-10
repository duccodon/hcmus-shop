namespace hcmus_shop.Services.Customers.Dto
{
    public class CustomerFilterDto
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreateCustomerInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateCustomerInput
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class GetCustomersRequest
    {
        public string? Search { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
