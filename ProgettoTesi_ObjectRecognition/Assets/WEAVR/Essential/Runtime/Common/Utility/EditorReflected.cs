using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This namespace is designed to make available and mask editor code during runtime
/// </summary>
namespace TXT.WEAVR.EditorBridge {

    public enum PrefabInstanceStatus
    {
        //
        // Summary:
        //     The object is not part of a Prefab instance.
        NotAPrefab = 0,
        //
        // Summary:
        //     The Prefab instance is connected to its Prefab Asset.
        Connected = 1,
        //
        // Summary:
        //     The Prefab instance is not connected to its Prefab Asset.
        Disconnected = 2,
        //
        // Summary:
        //     The Prefab instance is missing its Prefab Asset.
        MissingAsset = 3
    }

    public enum PrefabUnpackMode
    {
        //
        // Summary:
        //     Use this mode to only unpack the outermost layer of a Prefab.
        OutermostRoot = 0,
        //
        // Summary:
        //     Use this to strip away all Prefab information from a Prefab instance.
        Completely = 1
    }



    #region [  PREFAB UTILITY  ]
    public static class PrefabUtility
    {
        // Existing UNITY EDITOR methods
        //public static Func<UnityEngine.Object, UnityEngine.Object> GetPrefabObject = delegate { return null; };
        public static Func<UnityEngine.Object, UnityEngine.Object> GetPrefabParent = delegate { return null; };
        public static Func<UnityEngine.Object, UnityEngine.Object> InstantiatePrefab = delegate { return null; };
        public static Func<UnityEngine.Object, Scene, UnityEngine.Object> InstantiatePrefabInScene = delegate { return null; };
        //public static Func<GameObject, GameObject> FindPrefabRoot = delegate { return null; };
        public static Func<UnityEngine.Object, PrefabInstanceStatus> GetPrefabInstanceStatus = delegate { return PrefabInstanceStatus.NotAPrefab; };

        public static Func<UnityEngine.Object, bool> IsPrefabAsset = delegate { return false; };
        public static Func<UnityEngine.Object, bool> IsPrefabInstance = delegate { return false; };
        public static Func<UnityEngine.Object, bool> IsAnyTypeOfPrefab = o => IsPrefabAsset(o) || IsPrefabInstance(o);

        // WEAVR EDITOR methods
        public static Action<GameObject, GameObject> FixReferences = delegate { };
        public static Action<GameObject, PrefabUnpackMode> UnpackInstance = delegate { };
    }
    #endregion

    #region [  ASSET DATABASE  ]
    public static class AssetDatabase
    {
        public delegate UnityEngine.Object d_LoadAssetAtPath<T>(string path);

        public static Func<string, UnityEngine.Object[]> LoadAllAssetsAtPath = delegate { return null; };
        public static Func<UnityEngine.Object, string> GetAssetPath = delegate { return null; };
        public static Func<int, string> GetAssetPathById = delegate { return null; };
        public static Func<string, Type, UnityEngine.Object> LoadAssetAtPathByType = delegate { return null; };

        public static T LoadAssetAtPath<T>(string path) where T: UnityEngine.Object {
            return LoadAssetAtPathByType(path, typeof(T)) as T;
        }
        
    }
    #endregion

    #region [  EDITOR APPLICATION  ]

    public static class EditorApplication
    {
        public static Action DirtyHierarchyWindowSorting = delegate { };
    }

    #endregion

    #region [  GENERICS STRUCTURE ]

    internal class GenericsTypes<HolderT> where HolderT: new()
    {
        Dictionary<Type, HolderT> m_types;

        public GenericsTypes() {
            m_types = new Dictionary<Type, HolderT>();
        }

        public HolderT Get<T>() {
            HolderT holder = default(HolderT);
            if(!m_types.TryGetValue(typeof(T), out holder)) {
                m_types[typeof(T)] = new HolderT();
            }
            return holder;
        }
    }

    #endregion
}
