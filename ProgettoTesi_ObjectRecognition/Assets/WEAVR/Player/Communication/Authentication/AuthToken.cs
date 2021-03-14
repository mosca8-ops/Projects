
using System;

namespace TXT.WEAVR.Player.Communication.Auth
{
    [Serializable]
    public class AuthToken
    {
        public string Id_Token { get; set; }
        public string Access_Token { get; set; }
        public int Expires_In { get; set; }
        public string Token_Type { get; set; }
        public string Refresh_Token { get; set; }
        public string Scope { get; set; }
        public string Error_Description { get; set; }
    }
}
