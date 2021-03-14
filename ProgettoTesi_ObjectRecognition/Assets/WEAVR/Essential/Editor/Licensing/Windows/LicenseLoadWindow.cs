using UnityEditor;

namespace TXT.WEAVR.License
{
    public class LicenseLoadWindow : EditorWindow
    {

        [MenuItem("WEAVR/Licensing/Load Editor License", priority = 95)]
        public static void Apply()
        {
            LicenseLoadWindow.LoadLicense();
        }

        public static void LoadLicense()
        {
            string path = EditorUtility.OpenFilePanel("Load WEAVR Editor License File", "", "lic");
            if (path.Length != 0)
            {
                LicenseRemoveWindow.RemoveLicense();
                WeavrLE.Licensers.ForEach(l => l.LoadLicense(path));
            }
        }

    }
}