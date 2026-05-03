namespace hcmus_shop.GraphQL.Operations
{
	public static class AuthQueries
	{
		public const string Login = @"
            mutation Login($username: String!, $password: String!) {
                login(username: $username, password: $password) {
                    token
                    user {
                        userId
                        username
                        fullName
                        role
                    }
                }
            }";

		public const string Me = @"
            query {
                me {
                    userId
                    username
                    fullName
                    role
                }
            }";
	}
}