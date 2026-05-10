namespace hcmus_shop.GraphQL.Operations
{
    public static class CustomerQueries
    {
        public const string GetCustomers = @"
            query Customers($search: String, $page: Int, $pageSize: Int) {
                customers(search: $search, page: $page, pageSize: $pageSize) {
                    items {
                        customerId
                        name
                        phone
                        email
                        loyaltyPoints
                        rank
                        createdAt
                        updatedAt
                    }
                    totalCount
                    page
                    pageSize
                }
            }";

        public const string GetCustomerById = @"
            query Customer($customerId: ID!) {
                customer(customerId: $customerId) {
                    customerId
                    name
                    phone
                    email
                    loyaltyPoints
                    rank
                    createdAt
                    updatedAt
                }
            }";

        public const string CreateCustomer = @"
            mutation CreateCustomer($input: CreateCustomerInput!) {
                createCustomer(input: $input) {
                    customerId
                    name
                    phone
                    email
                    loyaltyPoints
                    rank
                    createdAt
                    updatedAt
                }
            }";

        public const string UpdateCustomer = @"
            mutation UpdateCustomer($customerId: ID!, $input: UpdateCustomerInput!) {
                updateCustomer(customerId: $customerId, input: $input) {
                    customerId
                    name
                    phone
                    email
                    loyaltyPoints
                    rank
                    createdAt
                    updatedAt
                }
            }";

        public const string DeleteCustomer = @"
            mutation DeleteCustomer($customerId: ID!) {
                deleteCustomer(customerId: $customerId)
            }";
    }
}
