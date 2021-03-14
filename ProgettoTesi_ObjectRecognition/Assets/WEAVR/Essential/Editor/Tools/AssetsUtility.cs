namespace TXT.WEAVR.Tools
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class AssetsUtility
    {

        public static void WrapTextAssetWith(TextAsset asset, string preWrap, string postWrap, bool refresh = false) {
            File.WriteAllText(AssetDatabase.GetAssetPath(asset), preWrap + "\n" + asset.text + "\n" + postWrap);
            EditorUtility.SetDirty(asset);
            if (refresh) {
                AssetDatabase.Refresh();
            }
        }

        public static void WriteAllToTextAsset(TextAsset asset, string newText, bool refresh = false) {
            File.WriteAllText(AssetDatabase.GetAssetPath(asset), newText);
            EditorUtility.SetDirty(asset);
            if (refresh) {
                AssetDatabase.Refresh();
            }
        }

        public static Object[] LoadAllAssetsAt(Object asset, bool includeChildren) {
            var relativePath = Application.dataPath.Replace("Assets", "") + AssetDatabase.GetAssetPath(asset);
            List<string> allPaths = new List<string>(Directory.GetFiles(relativePath, "*", includeChildren ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            List<Object> allObjects = new List<Object>();
            foreach (var path in allPaths) {
                if (path.EndsWith(".meta")) {
                    continue;
                }
                string assetPath = "Assets" + path.Replace(Application.dataPath, "").Replace('\\', '/');
                var loadedAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(TextAsset));
                if (loadedAsset != null && loadedAsset is MonoScript) {
                    allObjects.Add(loadedAsset);
                }
            }
            return allObjects.ToArray();
        }

        public static bool IsAssetFolder(Object asset) {
            FileAttributes attr = File.GetAttributes(Application.dataPath.Replace("Assets", "") + AssetDatabase.GetAssetPath(asset));
            return (attr & FileAttributes.Directory) == FileAttributes.Directory;
        }
    }
}