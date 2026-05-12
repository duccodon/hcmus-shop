namespace hcmus_shop.Models.DTOs
{
    public class UserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class UserPageDto
    {
        public System.Collections.Generic.List<UserDto> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class AuthPayloadDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
    }
}
