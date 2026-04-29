using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Auth.Dto;
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

        // ========================
        // LOGIN
        // ========================
        public async Task<bool> LoginAsync(string username, string password)
        {
            var request = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.MutateAsync<LoginResponse>(
                        AuthQueries.Login,
                        request
                    )
                );

            if (!result.IsSuccess)
            {
                return false;
            }

            Token = result.Value!.Login.Token;
            CurrentUser = result.Value.Login.User;

            _graphQL.SetAuthToken(Token);
            SaveToken(Token);

            return true;
        }

        // ========================
        // AUTO LOGIN
        // ========================
        public async Task<bool> TryAutoLoginAsync()
        {
            var savedToken = LoadToken();
            if (string.IsNullOrEmpty(savedToken))
                return false;

            _graphQL.SetAuthToken(savedToken);

            var result = await (_graphQL as GraphQLClientService)!
                .SafeExecuteAsync(() =>
                    _graphQL.QueryAsync<MeResponse>(AuthQueries.Me)
                );

            if (!result.IsSuccess || result.Value?.Me == null)
            {
                _graphQL.SetAuthToken(null);
                ClearToken();
                return false;
            }

            Token = savedToken;
            CurrentUser = result.Value.Me;
            return true;
        }

        // ========================
        // ROLE CHECK
        // ========================
        public bool HasRole(string role)
        {
            if (CurrentUser is null)
                return false;

            if (string.Equals(CurrentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(CurrentUser.Role, role, StringComparison.OrdinalIgnoreCase);
        }

        // ========================
        // LOGOUT
        // ========================
        public void Logout()
        {
            CurrentUser = null;
            Token = null;

            _graphQL.SetAuthToken(null);
            ClearToken();
        }

        // ========================
        // TOKEN STORAGE
        // ========================
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
    }
}