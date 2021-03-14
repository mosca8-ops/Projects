using UnityEditor;

namespace TXT.WEAVR.License
{
    public class LicenseRemoveWindow : EditorWindow
    {
        [MenuItem("WEAVR/Licensing/Remove License", priority = 95)]
        public static void ShowSetupAsWindow()
        {
            RemoveLicense();
        }

        // Remove the actual license
        public static void RemoveLicense()
        {
            WeavrLE.Licensers.ForEach(l => l.RemoveLicense());
        }
    }
}