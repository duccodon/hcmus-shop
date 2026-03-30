using BCrypt.Net;
using hcmus_shop.Data;
using hcmus_shop.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IDbContextFactory<MyShopDbContext> _dbContextFactory;

        public AuthService(IDbContextFactory<MyShopDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public User? CurrentUser { get; private set; }

        public async Task<bool> LoginAsync(string username, string password)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user is null)
            {
                return false;
            }

            var isValid = !string.IsNullOrWhiteSpace(user.PasswordHash)
                && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isValid)
            {
                return false;
            }

            CurrentUser = user;
            return true;
        }

        public bool HasRole(string role)
        {
            if (CurrentUser is null)
            {
                return false;
            }

            if (string.Equals(CurrentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(CurrentUser.Role, role, StringComparison.OrdinalIgnoreCase);
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}
