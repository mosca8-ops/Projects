using System.Collections.Generic;

namespace TXT.WEAVR.License
{
    public class WiredNodeLicenserEditor : WiredNodeLicenserPlayer, ILicenserEditor
    {

        public IEnumerable<string> GetDetails()
        {
            var returnValue = new List<string>();

            if (IsValid())
            {
                returnValue.Add($"Valid Node-Locked License");
            }
            else
            {
                returnValue.Add($"Invalid Node-Locked License");
            }

            return returnValue;
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