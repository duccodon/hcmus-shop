using hcmus_shop.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace hcmus_shop.Services.Auth.Dto
{
    public class LoginResponse
    {
        public AuthPayloadDto Login { get; set; } = new();
    }

    public class MeResponse
    {
        public UserDto? Me { get; set; }
    }
}
