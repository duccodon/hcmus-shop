using hcmus_shop.Contracts.Services;
using hcmus_shop.GraphQL.Operations;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Auth.Dto;
using hcmus_shop.Services.GraphQL;
using System;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;
using Windows.Storage;

namespace hcmus_shop.Services.Auth
{
    public class AuthService : IAuthService
    {
        private const string TokenKey = "auth_token";
        private const string TokenProtectionDescriptor = "LOCAL=user";
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
        public async Task<Result<bool>> LoginAsync(string username, string password, bool rememberMe = false)
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
                // Distinguish between "server unreachable" and "wrong credentials".
                // SafeExecuteAsync prefixes errors: "Network: ..." or "GraphQL: ...".
                var err = result.Error ?? "Unknown error.";
                if (err.StartsWith("Network:", StringComparison.OrdinalIgnoreCase))
                {
                    return Result<bool>.Failure("Cannot connect to server. Check the URL in Config.");
                }
                if (err.IndexOf("Invalid username or password", StringComparison.OrdinalIgnoreCase) >= 0
                    || err.IndexOf("login", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return Result<bool>.Failure("Invalid username or password.");
                }
                return Result<bool>.Failure(err);
            }

            Token = result.Value!.Login.Token;
            CurrentUser = result.Value.Login.User;
            _graphQL.SetAuthToken(Token);

            // Only persist the token if user opted in to "Remember me".
            // Otherwise the session is ephemeral — closing the app forces re-login.
            if (rememberMe)
            {
                await SaveTokenAsync(Token);
            }
            else
            {
                ClearToken();
            }

            return Result<bool>.Success(true);
        }

        // ========================
        // AUTO LOGIN
        // ========================
        public async Task<bool> TryAutoLoginAsync()
        {
            var savedToken = await LoadTokenAsync();
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
        private async Task SaveTokenAsync(string token)
        {
            var provider = new DataProtectionProvider(TokenProtectionDescriptor);
            var plainBuffer = CryptographicBuffer.ConvertStringToBinary(token, BinaryStringEncoding.Utf8);
            var protectedBuffer = await provider.ProtectAsync(plainBuffer);
            ApplicationData.Current.LocalSettings.Values[TokenKey] = CryptographicBuffer.EncodeToBase64String(protectedBuffer);
        }

        private async Task<string?> LoadTokenAsync()
        {
            if (ApplicationData.Current.LocalSettings.Values[TokenKey] is not string storedValue
                || string.IsNullOrWhiteSpace(storedValue))
            {
                return null;
            }

            if (storedValue.Contains('.', StringComparison.Ordinal))
            {
                // Legacy plain-text JWT from earlier builds. Accept once, then re-encrypt on success.
                return storedValue;
            }

            try
            {
                var provider = new DataProtectionProvider();
                IBuffer protectedBuffer = CryptographicBuffer.DecodeFromBase64String(storedValue);
                var plainBuffer = await provider.UnprotectAsync(protectedBuffer);
                return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, plainBuffer);
            }
            catch
            {
                ClearToken();
                return null;
            }
        }

        private void ClearToken()
        {
            ApplicationData.Current.LocalSettings.Values.Remove(TokenKey);
        }
    }
}
