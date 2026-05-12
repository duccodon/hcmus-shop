namespace hcmus_shop.Services.Users.Dto
{
    public class UserFilterDto
    {
        public string? Search { get; set; }
        public string? Role { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class CreateUserInput
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Sale";
    }

    public class UpdateUserInput
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string Role { get; set; } = "Sale";
    }
}
