using System.Collections.Generic;

namespace TXT.WEAVR.License
{
    public interface ILicenserEditor : ILicenserPlayer
    {

        void RefreshLicense();

        void RemoveLicense();

        void LoadLicense(string pathFile);

        IEnumerable<string> GetDetails();
    }
}