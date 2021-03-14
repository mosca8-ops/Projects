using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Procedure;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{

    public static class ProcedureInjectionResolver
    {
        [DidReloadScripts]
        [InitializeOnLoadMethod]
        static void Resolve() {
            
            #region [  WEAVR DATA  ]
            
            ProcedureObject.s_AddToProcedureAsset += Extensions.SaveToProcedure;
            //ProcedureObject.s_PreChange += UnityEditor.EditorUtility.SetDirty;
            //ProcedureObject.s_MarkDirty += UnityEditor.EditorUtility.SetDirty;
            ReferenceTable.s_PersistItem += (e, p) =>
            {
                if (AssetDatabase.Contains(p) && !AssetDatabase.Contains(e))
                {
                    e.hideFlags = p.hideFlags;
                    AssetDatabase.AddObjectToAsset(e, p);
                }
            };
            ReferenceTable.s_CreateRefTarget += Extensions.MakeTarget;
            ReferenceTable.s_RefreshSceneView += EditorApplication.RepaintHierarchyWindow;

            SceneData.GetSceneGuid = GetSceneNameAndId;
            SceneData.GetScenePathAndName = GetScenePathAndName;
            SceneData.SceneExists = SceneExists;
            //SceneData.ResolveSceneEditor = ResolveScene;

            Procedure.s_AssetProcedureOjects += GetProcedureObjects;
            Procedure.s_FindProcedureOject += FindProcedureObject;
            Procedure.s_SaveUpdateTime += Extensions.SaveUpdateTime;

            BaseCondition.s_Clone += CloneCondition;

            #endregion
        }

        private static BaseCondition CloneCondition(BaseCondition condition)
        {
            var clone = ScriptableObject.CreateInstance(condition.GetType()) as BaseCondition;
            EditorUtility.CopySerialized(condition, clone);
            return clone;
        }

        private static bool SceneExists(string path)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        private static (string path, string name) GetScenePathAndName(string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != null)
            {
                var assetObj = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (assetObj)
                {
                    return (path, assetObj.name);
                }
            }
            return (path, "");
        }

        private static Scene ResolveScene(string scenePath, string sceneGuid)
        {

            return default;
        }

        private static IEnumerable<ProcedureObject> GetProcedureObjects(Procedure procedure)
        {
            var path = AssetDatabase.GetAssetPath(procedure);
            return AssetDatabase.LoadAllAssetsAtPath(path).Where(o => o is ProcedureObject obj && obj.Procedure == procedure)
                                .Select(o => o as ProcedureObject);
        }

        private static ProcedureObject FindProcedureObject(Procedure procedure, string guid)
        {
            var path = AssetDatabase.GetAssetPath(procedure);
            return AssetDatabase.LoadAllAssetsAtPath(path).FirstOrDefault(o => o is ProcedureObject obj && obj.Guid == guid) as ProcedureObject;
        }

        private static void GetSceneNameAndId(string scenePath, out string name, out string guid)
        {
            var sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneObject)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sceneObject, out guid, out long fileId);
                name = sceneObject.name;
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
