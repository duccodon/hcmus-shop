using hcmus_shop.Models;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Auth
{
    public interface IAuthService
    {
        User? CurrentUser { get; }
        Task<bool> LoginAsync(string username, string password);
        bool HasRole(string role);
        void Logout();
    }
}
