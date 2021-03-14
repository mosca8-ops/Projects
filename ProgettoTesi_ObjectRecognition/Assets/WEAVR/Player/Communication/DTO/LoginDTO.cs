
using System;

namespace TXT.WEAVR.Player.Communication.DTO
{
    [Serializable]
    public class LoginDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }
        public string ClientId { get; set; }
        public string Scope { get; set; }
        public string Secret { get; set; }
        public AdditionalInfo AdditionalInfo { get; set; }
    }

    public class AdditionalInfo
    {
        public string PlayerVersion { get; set; }
        public string DeviceIdentifier { get; set; }
        public string DevicePlatform { get; set; }
        public string IP { get; set; }
        public Location Location { get; set; }
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public double? HorizontalAccuracy { get; set; }
        public double? VerticalAccuracy { get; set; }
        public double? Speed { get; set; }
        public double? Course { get; set; }
        public double Timestamp { get; set; }
    }
}
