using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.License
{
    public class WeavrLP
    {
        private static List<ILicenserPlayer> _licensers;
        public static List<ILicenserPlayer> Licensers {
            get {
                if (_licensers == null)
                {
                    InitializeLicenser();
                }
                return _licensers;
            }
        }

        public WeavrLP()
        {
            InitializeLicenser();
        }

        private static void InitializeLicenser(bool reinitialize = false)
        {
            _licensers = new List<ILicenserPlayer>() {
                new FreeLicenserPlayer(),
            };
        }

        public static bool IsValid()
        {
            return Licensers.All(l => l.IsValid());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoadRuntimeMethod()
        {
            try
            {
                if (!IsValid())
                {
                    Quit();
                }
            }
            catch
            {
                Quit();
            }
        }

        private static void Quit()
        {
            if (Application.platform == RuntimePlatform.LinuxEditor
                                || Application.platform == RuntimePlatform.OSXEditor
                                || Application.platform == RuntimePlatform.WindowsEditor)
            {
                Application.Quit();
            }
            else if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                Application.OpenURL("about:blank");
            }
            else
            {
                Application.Quit();
            }
        }
    }

}