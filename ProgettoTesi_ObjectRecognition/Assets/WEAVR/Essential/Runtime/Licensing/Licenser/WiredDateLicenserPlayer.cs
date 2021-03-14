using System.Globalization;
using UnityEngine;

namespace TXT.WEAVR.License
{
    public class WiredDateLicenserPlayer : ILicenserPlayer
    {
        protected const string playerPrefLastDate = "LAST_DATE";
        private const string year = "2020";
        private const string month = "09";
        private const string day = "21";
        protected static readonly System.DateTime _expireDate = System.DateTime.ParseExact($"{year}-{month}-{day}T00:00:00Z", formatDate, CultureInfo.InvariantCulture);
        protected const string formatDate = "yyyy-MM-ddTHH:mm:ssZ";

        public bool IsValid()
        {
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                //try to get from server
            }

            var saved = System.DateTime.Now;
            var stringSaved = PlayerPrefs.GetString(playerPrefLastDate);
            if (string.IsNullOrEmpty(stringSaved) || !System.DateTime.TryParseExact(stringSaved, formatDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out saved))
            {
                saved = System.DateTime.Now;
            }

            var now = System.DateTime.Now;
            // there is a problem, user set a before date
            if (now < saved)
            {
                Debug.LogError($"Date error.");
                return false;
            }

            PlayerPrefs.SetString(playerPrefLastDate, now.ToUniversalTime().ToString(formatDate, CultureInfo.InvariantCulture));

            if (now > _expireDate)
            {
                Debug.LogError($"Date is expired [{_expireDate}]");
                return false;
            }

            return true;
        }
    }
}
