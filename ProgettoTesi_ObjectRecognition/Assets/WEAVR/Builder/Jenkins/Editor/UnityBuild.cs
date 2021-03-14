using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Builder
{
    public class UnityBuild
    {

        private static readonly bool IsDevelopmentBuild = false;

        private static readonly Dictionary<BuildTarget, PlatformSpecificBuildOptions> PlatformBuildOptions = new Dictionary<BuildTarget, PlatformSpecificBuildOptions>()
        {
            { BuildTarget.StandaloneWindows64,        new PlatformSpecificBuildOptions(BuildOptions.Development | BuildOptions.CompressWithLz4,                                 BuildOptions.CompressWithLz4HC) },
            //{ BuildTarget.StandaloneLinuxUniversal,   new PlatformSpecificBuildOptions(BuildOptions.Development | BuildOptions.CompressWithLz4,                                 BuildOptions.CompressWithLz4HC) },
            { BuildTarget.StandaloneOSX,              new PlatformSpecificBuildOptions(BuildOptions.Development | BuildOptions.CompressWithLz4,                                 BuildOptions.CompressWithLz4HC) },
            { BuildTarget.Android,                    new PlatformSpecificBuildOptions(BuildOptions.Development | BuildOptions.CompressWithLz4,                                 BuildOptions.CompressWithLz4HC) },
            { BuildTarget.iOS,                        new PlatformSpecificBuildOptions(BuildOptions.Development | BuildOptions.CompressWithLz4 | BuildOptions.SymlinkLibraries, BuildOptions.CompressWithLz4HC) },
            { BuildTarget.WebGL,                      new PlatformSpecificBuildOptions(BuildOptions.Development | BuildOptions.CompressWithLz4,                                 BuildOptions.CompressWithLz4HC) },
        };

        private static BuildOptions GetPlatformBuildOptions(BuildTarget buildPlatform)
        {
            if (IsDevelopmentBuild == true)
                return PlatformBuildOptions[buildPlatform].BuildOptionsDevelopment;
            else
                return PlatformBuildOptions[buildPlatform].BuildOptionsRelease;
        }

        private static readonly string AndroidSdkDirectory = Environment.GetEnvironmentVariable("ANDROID_SDK");
        public static void SetupAndroidSdkPath()
        {
            EditorPrefs.SetString("AndroidSdkRoot", AndroidSdkDirectory);
        }

        private static void PrepareEnvironment()
        {
            // Delete temp files in order to recreate them
            string root = Path.Combine(Application.dataPath, "..");

/*
            // ScriptAssemblies
            {
                string scriptAssemblies = Path.Combine(root, "Library", "ScriptAssemblies");
                if (Directory.Exists(scriptAssemblies))
                {
                    Directory.Delete(scriptAssemblies, true);
                }
            }
*/
        }


        private static void ApplyGeneralSettings()
        {
            // General Settings
            PlayerSettings.companyName = "TXT e-solutions";
            PlayerSettings.bundleVersion = Weavr.VERSION;

            //Splash
            PlayerSettings.SplashScreen.show = false;

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;

#if !UNITY_2019_3_OR_NEWER
            PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
#endif
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Master);
        }

        private static string DateTimeString => DateTime.Now.ToString("yyyyMMdd_HHmmss_", CultureInfo.InvariantCulture);
        private static string GetTempPath(string type) => Path.Combine(Path.GetTempPath(), "WEAVR", $"{DateTimeString}{type}");

        /*
         * 
         * Method called by external
         * 
         */

        public static void BuildEditor()
        {
            Debug.Log("STARTING BuildEditor");

            PrepareEnvironment();
            ApplyGeneralSettings();

            string pathTemp = GetTempPath("Editor");

            WeavrDllBuilder.CreateBuildSync(null);

            Debug.Log("ENDED BuildEditor");
        }

        public static void BuildWindowsOPS()
        {

            Debug.Log("STARTING BuildWindowsOPS");

            PrepareEnvironment();
            string pathTemp = GetTempPath("Windows_OPS");

            ApplyGeneralSettings();
            PlayerSettings.productName = "WEAVR Player OPS";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            BuildPlayerOptions playerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/WEAVR/Essential/Assets/Scenes/Player/PLAYER OPS.unity" },
                locationPathName = pathTemp,
                assetBundleManifestPath = null,
                targetGroup = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneWindows64,
                options = GetPlatformBuildOptions(BuildTarget.StandaloneWindows64)
            };

            BuildPipeline.BuildPlayer(playerOptions);

            Debug.Log("ENDED BuildWindowsOPS");
        }

        public static void BuildWindowsVR()
        {
            Debug.Log("STARTING BuildWindowsVR");

            PrepareEnvironment();
            string pathTemp = GetTempPath("Windows_VR");

            ApplyGeneralSettings();
            PlayerSettings.productName = "WEAVR Player VR";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            BuildPlayerOptions playerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/WEAVR/Essential/Assets/Scenes/Player/PLAYER VR.unity" },
                locationPathName = pathTemp,
                assetBundleManifestPath = null,
                targetGroup = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneWindows64,
                options = GetPlatformBuildOptions(BuildTarget.StandaloneWindows64)
            };

            BuildPipeline.BuildPlayer(playerOptions);

            Debug.Log("ENDED BuildWindowsVR");
        }

        public static void BuildAndroid()
        {
            Debug.Log("STARTING BuildAndroid");

            PrepareEnvironment();
            SetupAndroidSdkPath();

            string pathTemp = GetTempPath("Android");

            ApplyGeneralSettings();
            PlayerSettings.productName = "WEAVR Player";
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            BuildPlayerOptions playerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/WEAVR/Essential/Assets/Scenes/Player/PLAYER OPS.unity" },
                locationPathName = pathTemp,
                assetBundleManifestPath = null,
                targetGroup = BuildTargetGroup.Android,
                target = BuildTarget.Android,
                options = GetPlatformBuildOptions(BuildTarget.Android)
            };

            BuildPipeline.BuildPlayer(playerOptions);

            Debug.Log("ENDED BuildAndroid");
        }

        public static void BuildIOS()
        {
            Debug.Log("STARTING BuildIOS");

            PrepareEnvironment();


            string pathTemp = GetTempPath("iOS");

            ApplyGeneralSettings();
            PlayerSettings.productName = "WEAVR Player";
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            BuildPlayerOptions playerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/WEAVR/Essential/Assets/Scenes/Player/PLAYER OPS.unity" },
                locationPathName = pathTemp,
                assetBundleManifestPath = null,
                targetGroup = BuildTargetGroup.iOS,
                target = BuildTarget.iOS,
                options = GetPlatformBuildOptions(BuildTarget.iOS)
            };

            BuildPipeline.BuildPlayer(playerOptions);

            Debug.Log("ENDED BuildIOS");
        }

        public static void BuildWSA()
        {
            Debug.Log("STARTING BuildWSA");

            PrepareEnvironment();

            string pathTemp = GetTempPath("WSA");

            ApplyGeneralSettings();
            PlayerSettings.productName = "WEAVR Player";
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            BuildPlayerOptions playerOptions = new BuildPlayerOptions()
            {
                scenes = new string[] { "Assets/WEAVR/Essential/Assets/Scenes/Player/PLAYER OPS.unity" },
                locationPathName = pathTemp,
                assetBundleManifestPath = null,
                targetGroup = BuildTargetGroup.WSA,
                target = BuildTarget.WSAPlayer,
                options = GetPlatformBuildOptions(BuildTarget.WSAPlayer)
            };

            BuildPipeline.BuildPlayer(playerOptions);

            Debug.Log("ENDED BuildWSA");
        }
    }
}
