using System.Collections.Generic;

namespace TXT.WEAVR.License
{
    public class FreeLicenserEditor : FreeLicenserPlayer, ILicenserEditor
    {
        public IEnumerable<string> GetDetails()
        {
            return new List<string>(1) { "Free license for develop." };
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