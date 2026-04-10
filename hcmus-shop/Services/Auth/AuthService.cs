using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.GraphQL;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace hcmus_shop.Services.Auth
{
    public class AuthService : IAuthService
    {
        private const string TokenKey = "auth_token";
        private readonly IGraphQLClientService _graphQL;

        public AuthService(IGraphQLClientService graphQL)
        {
            _graphQL = graphQL;
        }

        public UserDto? CurrentUser { get; private set; }
        public string? Token { get; private set; }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var query = @"
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

                var result = await _graphQL.MutateAsync<LoginResponse>(query, new { username, password });

                Token = result.Login.Token;
                CurrentUser = result.Login.User;
                _graphQL.SetAuthToken(Token);
                SaveToken(Token);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            var savedToken = LoadToken();
            if (string.IsNullOrEmpty(savedToken)) return false;

            try
            {
                _graphQL.SetAuthToken(savedToken);

                var query = @"
                    query {
                        me {
                            userId
                            username
                            fullName
                            role
                        }
                    }";

                var result = await _graphQL.QueryAsync<MeResponse>(query);

                if (result.Me == null) return false;

                Token = savedToken;
                CurrentUser = result.Me;
                return true;
            }
            catch
            {
                _graphQL.SetAuthToken(null);
                ClearToken();
                return false;
            }
        }

        public bool HasRole(string role)
        {
            if (CurrentUser is null) return false;

            if (string.Equals(CurrentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(CurrentUser.Role, role, StringComparison.OrdinalIgnoreCase);
        }

        public void Logout()
        {
            CurrentUser = null;
            Token = null;
            _graphQL.SetAuthToken(null);
            ClearToken();
        }

        private void SaveToken(string token)
        {
            ApplicationData.Current.LocalSettings.Values[TokenKey] = token;
        }

        private string? LoadToken()
        {
            return ApplicationData.Current.LocalSettings.Values[TokenKey] as string;
        }

        private void ClearToken()
        {
            ApplicationData.Current.LocalSettings.Values.Remove(TokenKey);
        }

        // Response types for deserialization
        private class LoginResponse
        {
            public AuthPayloadDto Login { get; set; } = new();
        }

        private class MeResponse
        {
            public UserDto? Me { get; set; }
        }
    }
}
