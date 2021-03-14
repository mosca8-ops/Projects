using System;
using System.Linq;
using System.Reflection;
using TXT.WEAVR;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class UniqueIDMonitor
{
    private const string k_LocalIdentifierField = "m_LocalIdentfierInFile";

    private static Func<object, object> s_getInspectorMode;
    private static Func<object, object> Get_InspectorMode
    {
        get
        {
            if(s_getInspectorMode == null)
            {
                s_getInspectorMode = typeof(SerializedObject).PropertyGet("inspectorMode");
            }
            return s_getInspectorMode;
        }
    }

    private static Action<object, object> s_setInspectorMode;
    private static Action<object, object> Set_InspectorMode
    {
        get
        {
            if (s_setInspectorMode == null)
            {
                s_setInspectorMode = typeof(SerializedObject).PropertySet("inspectorMode");
            }
            return s_setInspectorMode;
        }
    }

    static UniqueIDMonitor()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        SceneManager.sceneLoaded += EditorSceneManager_SceneLoaded;

        IDBookkeeper.SetIDValue = SetUniqueIdValue;
    }

    private static bool CanUpdate => !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer && !EditorApplication.isPlaying;

    private static void EditorSceneManager_SceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (IDBookkeeper.GetSingleton(scene).AutoUpdate && loadMode == LoadSceneMode.Single && CanUpdate)
        {
            IDBookkeeper.IndexScene(scene);
        }
    }

    public static T Instantiate<T>(T obj) where T : Component
    {
        try
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            var newObj = UnityEngine.Object.Instantiate(obj);
            foreach(var uid in newObj.GetComponentsInChildren<UniqueID>(true))
            {
                if (uid)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(uid);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(uid);
                    }
                }
            }
            return newObj;
        }
        finally
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
    }

    public static GameObject Instantiate(GameObject obj)
    {
        try
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            var newObj = UnityEngine.Object.Instantiate(obj);
            foreach (var uid in newObj.GetComponentsInChildren<UniqueID>(true))
            {
                if (uid)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(uid);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(uid);
                    }
                }
            }
            return newObj;
        }
        finally
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }
    }

    static void OnHierarchyChanged()
    {
        if (!CanUpdate) { return; }
        for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
        {
            var singleton = IDBookkeeper.GetSingleton(EditorSceneManager.GetSceneAt(i));
            if (singleton != null && singleton.AutoUpdate)
            {
                IDBookkeeper.IndexScene(EditorSceneManager.GetSceneAt(i));
            }
        }
        //if (IDBookkeeper.GetSingleton().AutoUpdate && CanUpdate)
        //{
        //    IDBookkeeper.IndexCurrentScene();
        //}
    }

    private static void SetUniqueIdValue(UniqueID component, string id)
    {
        if (component == null || string.IsNullOrEmpty(id))
        {
            return;
        }

        //long localId = GetObjectLocalIdInFile(component.transform);

        //if (localId == 0 && string.IsNullOrEmpty(id))
        //{
        //    return;
        //}

        SerializedObject obj = new SerializedObject(component);
        obj.Update();
        SerializedProperty property = obj.FindProperty("m_uniqueId");
        if (property.stringValue != id)
        {
            if (!string.IsNullOrEmpty(property.stringValue))
            {
                WeavrDebug.LogWarning(component, $"Unique ID changed from {property.stringValue} to {id}");
            }
            property.stringValue = id;// localId != 0 ? localId.ToString() : id;
            obj.FindProperty("m_timestamp").longValue = DateTime.Now.Ticks;

            if (obj.ApplyModifiedPropertiesWithoutUndo())
            {
                component.UpdateTimestamp();
            }
        }
    }

    private static long GetObjectLocalIdInFile(UnityEngine.Object obj)
    {
        long idInFile = 0;
        SerializedObject serObj = new SerializedObject(obj);
        InspectorMode mode = InspectorMode.Normal;
        if (Set_InspectorMode != null)
        {
            mode = (InspectorMode)Get_InspectorMode(serObj);
            Set_InspectorMode(serObj, InspectorMode.Debug);
        }
        idInFile = serObj.FindProperty(k_LocalIdentifierField).longValue;
        Set_InspectorMode?.Invoke(serObj, mode);
        return idInFile;
    }

}