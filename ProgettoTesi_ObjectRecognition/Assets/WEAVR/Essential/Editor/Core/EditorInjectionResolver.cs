using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Communication.Entities;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Procedure;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.EditorBridge
{
    [InitializeOnLoad]
    public static class EditorInjectionResolver
    {
        private static bool s_resolved;
        private static string s_cachedPlatform;

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        static void Resolve()
        {

            if (s_resolved) { return; }
            s_resolved = true;

            #region [  PREFAB UTILITY  ]
            //PrefabUtility.FindPrefabRoot = UnityEditor.PrefabUtility.FindPrefabRoot;
            PrefabUtility.IsPrefabAsset = UnityEditor.PrefabUtility.IsPartOfPrefabAsset;
            PrefabUtility.IsPrefabInstance = UnityEditor.PrefabUtility.IsPartOfPrefabInstance;
            PrefabUtility.GetPrefabInstanceStatus = o => (PrefabInstanceStatus)UnityEditor.PrefabUtility.GetPrefabInstanceStatus(o);
            //PrefabUtility.GetPrefabObject = UnityEditor.PrefabUtility.GetPrefabObject;
            PrefabUtility.GetPrefabParent = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource<UnityEngine.Object>;
            PrefabUtility.InstantiatePrefab = UnityEditor.PrefabUtility.InstantiatePrefab;
            PrefabUtility.InstantiatePrefabInScene = UnityEditor.PrefabUtility.InstantiatePrefab;

            PrefabUtility.UnpackInstance = (g, m) =>
            {
                if (g && UnityEditor.PrefabUtility.IsPartOfPrefabInstance(g))
                {
                    UnityEditor.PrefabUtility.UnpackPrefabInstance(g, (UnityEditor.PrefabUnpackMode)m, UnityEditor.InteractionMode.AutomatedAction);
                }
            };
            PrefabUtility.FixReferences = EditorTools.ResolveReferences;

            #endregion

            #region [  ASSET DATABASE  ]

            AssetDatabase.LoadAllAssetsAtPath = UnityEditor.AssetDatabase.LoadAllAssetsAtPath;
            AssetDatabase.GetAssetPath = UnityEditor.AssetDatabase.GetAssetPath;
            AssetDatabase.GetAssetPathById = UnityEditor.AssetDatabase.GetAssetPath;
            AssetDatabase.LoadAssetAtPathByType = UnityEditor.AssetDatabase.LoadAssetAtPath;
            Core.ObjectRetriever.GetAssetGloballyFunctor = (p, t) => UnityEditor.AssetDatabase.LoadAssetAtPath(p, t);

            #endregion

            #region [  EDITOR APPLICATION  ]

            EditorApplication.DirtyHierarchyWindowSorting = UnityEditor.EditorApplication.DirtyHierarchyWindowSorting;

            #endregion

            #region [  WEAVR DATA  ]

            ColorPalette.s_fullSave = c =>
            {
                Extensions.FullSave(c);
                EditorUtility.SetDirty(c);
            };

            // Since it is only possible to get the build target from the main thread, 
            // then save it right now and get the cached version
            s_cachedPlatform = GetCurrentPlatform();
            EntitiesExtensions.GetPlatformFunctor = () => s_cachedPlatform;
            #endregion

            #region [  GUID COMPONENT  ]

            GuidComponent.IsAnyTypeOfPrefabAsset = CheckIfObjectIsAnyTypeOfPrefabAsset;

            #endregion
        }

        private static string GetCurrentPlatform()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneOSX: return RuntimePlatform.OSXPlayer.ToString();
                case BuildTarget.StandaloneWindows: return RuntimePlatform.WindowsPlayer.ToString();
                case BuildTarget.iOS: return RuntimePlatform.IPhonePlayer.ToString();
                case BuildTarget.Android: return RuntimePlatform.Android.ToString();
                case BuildTarget.StandaloneWindows64: return RuntimePlatform.WindowsPlayer.ToString();
                case BuildTarget.WebGL: return RuntimePlatform.WebGLPlayer.ToString();
                case BuildTarget.WSAPlayer: return RuntimePlatform.WSAPlayerX86.ToString();
                case BuildTarget.StandaloneLinux64: return RuntimePlatform.LinuxPlayer.ToString();
                case BuildTarget.PS4: return RuntimePlatform.PS4.ToString();
                case BuildTarget.XboxOne: return RuntimePlatform.XboxOne.ToString();
                case BuildTarget.tvOS: return RuntimePlatform.tvOS.ToString();
                case BuildTarget.Switch: return RuntimePlatform.Switch.ToString();
                case BuildTarget.Lumin: return RuntimePlatform.Lumin.ToString();
                case BuildTarget.Stadia: return RuntimePlatform.Stadia.ToString();
            }
            return Application.platform.ToString();
        }

        private static bool CheckIfObjectIsAnyTypeOfPrefabAsset(GameObject go) => UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go) || IsEditingInPrefabMode(go);

        private static bool IsEditingInPrefabMode(GameObject go)
        {
            if (EditorUtility.IsPersistent(go))
            {
                // if the game object is stored on disk, it is a prefab of some kind, despite not returning true for IsPartOfPrefabAsset =/
                return true;
            }
            else
            {
                // If the GameObject is not persistent let's determine which stage we are in first because getting Prefab info depends on it
                var mainStage = StageUtility.GetMainStageHandle();
                var currentStage = StageUtility.GetStageHandle(go);
                if (currentStage != mainStage)
                {
                    var prefabStage = PrefabStageUtility.GetPrefabStage(go);
                    if (prefabStage != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static UnityEngine.SceneManagement.Scene ResolveScene(string scenePath, string sceneGuid)
        {

            return default;
        }

        private static void GetSceneNameAndId(string scenePath, out string name, out string guid)
        {
            var sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneObject)
            {
                UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sceneObject, out guid, out long fileId);
                Debug.Log($"Got Scene at {scenePath}");
                name = sceneObject.name;
                Debug.Log($"Scene: name = {name}");
                Debug.Log($"Scene: guid = {guid}");
            }
            else
            {
                Debug.Log($"Scene not found at {scenePath}");
                name = string.Empty;
                guid = string.Empty;
            }
        }
    }
}
