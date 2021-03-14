using System.Collections.Generic;

namespace TXT.WEAVR.License
{
    public class WiredDateLicenserEditor : WiredDateLicenserPlayer, ILicenserEditor
    {
        public IEnumerable<string> GetDetails()
        {
            var returnValue = new List<string>();

            if (IsValid())
            {
                returnValue.Add($"Expiration: {_expireDate.ToString(formatDate)}");
            }
            else
            {
                returnValue.Add($"License is expired : {_expireDate.ToString(formatDate)}");
            }

            return returnValue;
        }

        public new bool IsValid()
        {
            return base.IsValid();
        }

        public void LoadLicense(string pathFile)
        {

        }

        public void RefreshLicense()
        {

        }

        public void RemoveLicense()
        {

        }
    }
}