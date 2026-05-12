using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using hcmus_shop.Services.Users.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hcmus_shop.Contracts.Services
{
    public interface IUserService
    {
        Task<Result<UserPageDto>> GetAllAsync(UserFilterDto filter);
        Task<Result<UserDto?>> GetByIdAsync(string userId);
        Task<Result<UserDto>> CreateAsync(CreateUserInput input);
        Task<Result<UserDto>> UpdateAsync(string userId, UpdateUserInput input);
        Task<Result<bool>> DeleteAsync(string userId);
    }
}
