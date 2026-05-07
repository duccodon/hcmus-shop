using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IAuthService
    {
        UserDto? CurrentUser { get; }
        string? Token { get; }

        /// <summary>
        /// Returns Result.Success(true) on successful login.
        /// Returns Result.Failure with a specific reason:
        ///   - "Cannot connect to server: ..." for network errors
        ///   - "Invalid username or password." for auth failures
        ///   - other GraphQL errors as raw messages
        /// </summary>
        Task<Result<bool>> LoginAsync(string username, string password, bool rememberMe = false);

        Task<bool> TryAutoLoginAsync();
        bool HasRole(string role);
        void Logout();
    }
}
