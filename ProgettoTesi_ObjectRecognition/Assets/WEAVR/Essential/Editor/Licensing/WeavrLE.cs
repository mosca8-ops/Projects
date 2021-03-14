using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace TXT.WEAVR.License
{
    [InitializeOnLoad]
    public class WeavrLE : WeavrLP
    {
        private static List<ILicenserEditor> _licensers;
        public new static List<ILicenserEditor> Licensers {
            get {
                if (_licensers == null)
                {
                    InitializeLicenser();
                }
                return _licensers;
            }
        }

        static WeavrLE()
        {
            InitializeLicenser();
        }

        private static void InitializeLicenser()
        {
            _licensers = new List<ILicenserEditor>() {
                new FreeLicenserEditor(),
            };
        }

        public new static bool IsValid()
        {
            return Licensers.All(l => l.IsValid());
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoadRuntimeMethod()
        {
            if (!IsValid())
            {
                if (Application.platform == RuntimePlatform.LinuxEditor
                    || Application.platform == RuntimePlatform.OSXEditor
                    || Application.platform == RuntimePlatform.WindowsEditor)
                {
                    EditorApplication.isPlaying = false;
                }
            }
        }

        [PostProcessBuild(0)]
        private static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            // If the licenser is RLM we have to copy the DLL
            if (_licensers.OfType<RLMLicenserEditor>().Any() || WeavrLP.Licensers.OfType<RLMLicenserPlayer>().Any())
            {
                if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
                {
                    var projectDirectory = Path.GetDirectoryName(pathToBuiltProject);
                    File.Copy(Path.Combine(projectDirectory, $"{Application.productName}_Data", "Plugins", RLMLicenserEditor.RLM_DLL_NAME), Path.Combine(projectDirectory, RLMLicenserEditor.RLM_DLL_NAME), true);
                }
            }

            if (!IsValid())
            {
                var attr = File.GetAttributes(pathToBuiltProject);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    Directory.Delete(pathToBuiltProject, true);
                }
                else
                {
                    File.Delete(pathToBuiltProject);
                }
                throw new System.Exception("License Not Valid");
            }
        }

    }
}