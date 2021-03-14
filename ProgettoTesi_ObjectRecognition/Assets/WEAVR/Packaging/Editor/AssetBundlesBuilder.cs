using Newtonsoft.Json;
using System.IO;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Packaging
{


    public class AssetBundlesBuilder
    {
        public static string AssetBundleFolderPath = "AssetBundles";

        private static readonly BuildTarget[] _targets = new BuildTarget[] 
        { 
            BuildTarget.StandaloneWindows, 
            BuildTarget.Android, 
            BuildTarget.iOS, 
            BuildTarget.StandaloneOSX, 
            BuildTarget.StandaloneLinux64, 
            BuildTarget.WSAPlayer 
        };

        [MenuItem("Assets/AssetBundles/Build All AssetBundles")]
        public static void BuildAllAssetBundles(bool clear = false)
        {
            foreach (var target in _targets)
            {
                string assetBundleDirectory = Path.Combine(AssetBundleFolderPath, target.ToString());

                if (clear)
                {
                    try
                    {
                        if (Directory.Exists(assetBundleDirectory))
                        {
                            Directory.Delete(assetBundleDirectory, true);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                if (!Directory.Exists(assetBundleDirectory))
                {
                    Directory.CreateDirectory(assetBundleDirectory);
                }
                BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, target);
            }
        }

        [MenuItem("Assets/AssetBundles/Build AssetBundles/Android")]
        public static void BuildAndroidAssetBundles(bool clear = false)
        {
            string assetBundleDirectory = Path.Combine(AssetBundleFolderPath, "Android");

            if (clear)
            {
                try
                {
                    if (Directory.Exists(assetBundleDirectory))
                    {
                        Directory.Delete(assetBundleDirectory, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
        }

        [MenuItem("Assets/AssetBundles/Build AssetBundles/Windows Standalone")]
        public static void BuildStandaloneWindowsAssetBundles(bool clear = false)
        {
            string assetBundleDirectory = Path.Combine(AssetBundleFolderPath, "StandaloneWindows");

            if (clear)
            {
                try
                {
                    if (Directory.Exists(assetBundleDirectory))
                    {
                        Directory.Delete(assetBundleDirectory, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
        }

        [MenuItem("Assets/AssetBundles/Build AssetBundles/iOS")]
        public static void BuildiOSAssetBundles(bool clear = false)
        {
            string assetBundleDirectory = Path.Combine(AssetBundleFolderPath, "iOS");

            if (clear)
            {
                try
                {
                    if (Directory.Exists(assetBundleDirectory))
                    {
                        Directory.Delete(assetBundleDirectory, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.iOS);
        }

        [MenuItem("Assets/AssetBundles/Build AssetBundles/HoloLens")]
        public static void BuildWSAPlayerAssetBundles(bool clear = false)
        {
            string assetBundleDirectory = Path.Combine(AssetBundleFolderPath, "WSAPlayer");

            if (clear)
            {
                try
                {
                    if (Directory.Exists(assetBundleDirectory))
                    {
                        Directory.Delete(assetBundleDirectory, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.WSAPlayer);
        }

        public static void BuildCurrentPlatformAssetBundles(bool clear = false, string subfolder = "")
        {
            string assetBundleDirectory = Path.Combine(GetCurrentPlatformAssetBundlePath(), subfolder);

            if (clear)
            {
                try
                {
                    if (Directory.Exists(assetBundleDirectory))
                    {
                        Directory.Delete(assetBundleDirectory, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
        }

        public static void BuildCurrentPlatformAssetBundles(AssetBundleBuild[] assetBundles, bool clear = false, string subfolder = "")
        {
            string assetBundleDirectory = Path.Combine(GetCurrentPlatformAssetBundlePath(), subfolder);

            if (clear)
            {
                try
                {
                    if (Directory.Exists(assetBundleDirectory))
                    {
                        Directory.Delete(assetBundleDirectory, true);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            var manifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory, assetBundles, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
            if(manifest != null)
            {
                // Remove the manifest as we don't need it
                var directoryName = Path.GetFileName(assetBundleDirectory);
                foreach(var file in Directory.GetFiles(assetBundleDirectory))
                {
                    if (Path.GetFileName(file).StartsWith(directoryName))
                    {
                        File.Delete(file);
                    }
                }
            }
        }

        public static void SetCurrentPlatformCorrectBuildSettings()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
                    break;
                case BuildTarget.StandaloneWindows:
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    break;
                case BuildTarget.StandaloneLinux64:
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
                    break;
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    break;
                case BuildTarget.Android:
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_4_6);
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    break;
                case BuildTarget.iOS:
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.iOS, ApiCompatibilityLevel.NET_4_6);
                    PlayerSettings.colorSpace = ColorSpace.Linear;
                    break;
                case BuildTarget.WSAPlayer:
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WSA, ApiCompatibilityLevel.NET_4_6);
                    PlayerSettings.colorSpace = ColorSpace.Gamma;
                    break;
                case BuildTarget.WebGL:
                    PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.WebGL, ApiCompatibilityLevel.NET_4_6);
                    PlayerSettings.colorSpace = ColorSpace.Gamma;
                    break;
            }
        }


        public static string GetCurrentPlatformAssetBundlePath()
        {
            return Path.Combine(AssetBundleFolderPath, EditorUserBuildSettings.activeBuildTarget.ToString());
        }


        public static void WriteJsonMetadataFile(string procedureGUID, object jsonObject, string subfolder = "")
        {
            string assetBundleDirectory = Path.Combine(GetCurrentPlatformAssetBundlePath(), subfolder);

            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            string jsonFilePath = Path.Combine(assetBundleDirectory, "Preview" + ".json");

            if (File.Exists(jsonFilePath))
            {
                File.Delete(jsonFilePath);
            }

            string jsonContent = JsonConvert.SerializeObject(jsonObject);
            File.WriteAllText(jsonFilePath, jsonContent);
        }

        public static void ConvertBundleToWEAVRFile(string subfolder, string fileName, bool deleteFolder = false)
        {
            string assetBundleDirectory = Path.Combine(GetCurrentPlatformAssetBundlePath(), subfolder);
            string filepath = Path.Combine(GetCurrentPlatformAssetBundlePath(), fileName);

            if (Directory.Exists(assetBundleDirectory))
            {
                GZIP.CreateTarGZ(filepath, assetBundleDirectory);
                if (deleteFolder)
                {
                    Directory.Delete(assetBundleDirectory, true);
                }
            }
            else
            {
                WeavrDebug.LogError(typeof(AssetBundlesBuilder), $"Unable to create the weavr file, cannot find folder: {assetBundleDirectory}");
            }
        }
    }
}