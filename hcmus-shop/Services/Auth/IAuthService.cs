using hcmus_shop.Models.DTOs;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Auth
{
    public interface IAuthService
    {
        UserDto? CurrentUser { get; }
        string? Token { get; }
        Task<bool> LoginAsync(string username, string password);
        Task<bool> TryAutoLoginAsync();
        bool HasRole(string role);
        void Logout();
    }
}
