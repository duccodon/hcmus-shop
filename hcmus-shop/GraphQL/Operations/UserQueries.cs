namespace hcmus_shop.GraphQL.Operations
{
    public static class UserQueries
    {
        public const string GetUsers = @"
            query Users($search: String, $role: String, $page: Int, $pageSize: Int) {
                users(search: $search, role: $role, page: $page, pageSize: $pageSize) {
                    items {
                        userId
                        username
                        fullName
                        role
                        createdAt
                    }
                    totalCount
                    page
                    pageSize
                }
            }";

        public const string GetUserById = @"
            query User($userId: ID!) {
                user(userId: $userId) {
                    userId
                    username
                    fullName
                    role
                    createdAt
                }
            }";

        public const string CreateUser = @"
            mutation CreateUser($input: CreateUserInput!) {
                createUser(input: $input) {
                    userId
                    username
                    fullName
                    role
                    createdAt
                }
            }";

        public const string UpdateUser = @"
            mutation UpdateUser($userId: ID!, $input: UpdateUserInput!) {
                updateUser(userId: $userId, input: $input) {
                    userId
                    username
                    fullName
                    role
                    createdAt
                }
            }";

        public const string DeleteUser = @"
            mutation DeleteUser($userId: ID!) {
                deleteUser(userId: $userId)
            }";
    }
}
