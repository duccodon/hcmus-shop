using hcmus_shop.Models.DTOs;

namespace hcmus_shop.Services.Users.Dto
{
    public class UsersResponse
    {
        public UserPageDto Users { get; set; } = new();
    }

    public class UserResponse
    {
        public UserDto? User { get; set; }
    }

    public class CreateUserResponse
    {
        public UserDto CreateUser { get; set; } = new();
    }

    public class UpdateUserResponse
    {
        public UserDto UpdateUser { get; set; } = new();
    }

    public class DeleteUserResponse
    {
        public bool DeleteUser { get; set; }
    }
}
