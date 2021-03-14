
using System;

namespace TXT.WEAVR.Player.Communication.Auth
{
    [Serializable]
    public class ChangePasswordModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Token { get; set; }
    }
}
