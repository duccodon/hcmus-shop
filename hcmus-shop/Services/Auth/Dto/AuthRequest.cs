using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.Services.Auth.Dto
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
