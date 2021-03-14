
using System;

namespace TXT.WEAVR.Player.Communication.Auth
{
    [Serializable]
    public class ConnectToken
    {
        public string Client_Id { get; set; }
        public string Client_Secret { get; set; }
        public string Token_Type { get; set; }
        public string Grant_Type { get; set; }
        public string Scope { get; set; }
        public string Refresh_Token { get; set; }
    }
}
