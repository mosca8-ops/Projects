
using System;
using TXT.WEAVR.Communication.Entities;

namespace TXT.WEAVR.Player.Communication.Auth
{
    [Serializable]
    public class AuthUser
    {
        public Guid Id { get; set; }
        public AuthToken Token { get; set; }
        public string[] Roles { get; set; }
        public User User { get; set; }
    }
}
