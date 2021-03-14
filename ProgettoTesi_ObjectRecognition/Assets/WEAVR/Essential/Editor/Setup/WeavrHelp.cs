using System.IO;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    class WeavrHelp
    {
        private static readonly string VERSION = "2.0.0";
        private static readonly string URL = $"https://help.pace.de/xr/weavr/{VERSION}/index.html";

        private static readonly string FILENAME = $"Pacelab WEAVR - User Guide {VERSION}.pdf";

        // Start is called before the first frame update
        [MenuItem("WEAVR/WEAVR Manual", priority = 115)]
        public static void OpenBrowserWithManual()
        {
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                Application.OpenURL(URL);
            }
            else
            {
                Application.OpenURL(Path.Combine("Assets", "WEAVR", FILENAME));
            }
        }
    }
}