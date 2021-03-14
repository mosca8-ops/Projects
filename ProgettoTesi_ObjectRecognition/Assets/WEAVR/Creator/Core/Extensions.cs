using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public static class Extensions
    {
        
        public static ReferenceTarget MakeTarget(this ProcedureObject obj, string fieldPath)
        {
            if(fieldPath.Contains("Array.data[") && fieldPath.EndsWith("]"))
            {
                return ReferenceIndexedTarget.Create(obj, fieldPath);
            }
            return ReferenceTarget.Create(obj, fieldPath);
        }

        public static void RegisterProcedureObject(this ProcedureObject parent, ProcedureObject pObj)
        {
            if (!pObj) { return; }

            if (pObj && !AssetDatabase.Contains(pObj) && parent)
            {
                pObj.Procedure = parent.Procedure;
                pObj.hideFlags = parent.hideFlags;
                if (AssetDatabase.Contains(parent))
                {
                    AssetDatabase.AddObjectToAsset(pObj, parent);
                }
                else if (parent.Procedure)
                {
                    AssetDatabase.AddObjectToAsset(pObj, pObj.Procedure);
                }
            }

            var procedure = parent ? parent.Procedure : pObj ? pObj.Procedure : null;
            if (procedure && procedure.Graph)
            {
                RegisterSceneReferences(pObj, procedure.Graph.ReferencesTable, new HashSet<object>() { procedure });
            }

            if(pObj && pObj.Procedure)
            {
                pObj.Procedure.SaveUpdateTime();
            }
        }

        public static void RegisterSceneReferences(this ProcedureObject obj, ReferenceTable table, HashSet<object> visited)
        {
            if(!obj || !table || visited.Contains(obj)) { return; }
            visited.Add(obj);

            var serObj = new SerializedObject(obj);
            serObj.Update();
            var iterator = serObj.FindProperty("m_Script");
            while(iterator.Next(iterator.propertyType == SerializedPropertyType.Generic))
            {
                if(iterator.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if(iterator.objectReferenceValue is Component c && c.gameObject.scene.IsValid())
                    {
                        table.Register(obj, c, iterator.propertyPath);
                    }
                    else if(iterator.objectReferenceValue is GameObject go && go.scene.IsValid())
                    {
                        table.Register(obj, go, iterator.propertyPath);
                    }
                    else if(iterator.objectReferenceValue is ProcedureObject po)
                    {
                        RegisterSceneReferences(po, table, visited);
                    }
                }
            }
        }

        public static GraphObject TryGetContainer(this BaseAction action)
        {
            if (!action.Procedure) { return null; }
            return action.Procedure.Graph.Nodes.FirstOrDefault(n => n is GenericNode gn && gn.FlowElements.Contains(action)) as GraphObject
                ?? action.Procedure.Graph.Transitions.FirstOrDefault(n => n.Actions.Contains(action));
        }

        public static GraphObject TryGetStep(this BaseAnimationBlock block)
        {
            return TryGetContainer(block.Composer);
        }

        public static GraphObject TryGetStep(this BaseCondition condition)
        {
            if (!condition.Procedure) { return null; }
            return condition.Procedure
                .Graph.Nodes.FirstOrDefault(n => n is GenericNode gn 
                            && gn.FlowElements.Any(f => f is FlowConditionsContainer fc 
                                        && fc.Conditions.Any(c => c.Contains(condition))));
        }

        public static GraphObject TryGetStep(this ProcedureObject obj)
        {
            switch (obj)
            {
                case BaseAction elem: return TryGetContainer(elem);
                case BaseCondition elem: return TryGetStep(elem);
                case BaseAnimationBlock elem: return TryGetStep(elem);
                case BaseTransition elem: return elem.From ?? elem.To;
                case BaseNode elem: return elem;
                case BaseStep elem: return elem.Nodes.FirstOrDefault();
                default: return null;
            }
        }

        public static bool IsCurrentlyOpen(this SceneData data)
        {
            return data.ResolveScene().isLoaded;
        }

        public static void Save(this Procedure procedure, bool updateReferences, HideFlags hideFlags = HideFlags.HideInHierarchy)
        {
            if (!AssetDatabase.Contains(procedure)) { return; }

            //var now = DateTime.Now;
            //var start = now;
            //Debug.Log($"Starting whole saving process at {now}");

            EditorApplication.LockReloadAssemblies();

            try
            {

                var assetPath = AssetDatabase.GetAssetPath(procedure);
                HashSet<Object> allAssets = new HashSet<Object>(AssetDatabase.LoadAllAssetsAtPath(assetPath));
                HashSet<Object> newlyAdded = new HashSet<Object>() { procedure };

                //Debug.Log($"Loaded all assets at {DateTime.Now} with elapsed: {(DateTime.Now - now).TotalMilliseconds} ms");
                //now = DateTime.Now;

                // Auto Assign procedure --> since sometimes it references another procedure
                procedure.Procedure = procedure;

                ReferenceTable referenceTable = null;
                var procedureScene = GetScene(procedure);
                bool sceneIsReady = false;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene == procedureScene && scene.IsValid() && scene.isLoaded)
                    {
                        sceneIsReady = true;
                        break;
                    }
                }

                if (!sceneIsReady)
                {
                    WeavrDebug.Log(procedure, "The references will not be updated: Scene is not ready (not loaded, invalid or broken)");
                    updateReferences = false;
                }
                else if (EditorApplication.isCompiling)
                {
                    WeavrDebug.Log(procedure, "The references will not be updated: The Editor application is compiling");
                    updateReferences = false;
                }
                else if (EditorApplication.isUpdating)
                {
                    WeavrDebug.Log(procedure, "The references will not be updated: The Editor application is updating the Assets Database");
                    updateReferences = false;
                }
                else if (BuildPipeline.isBuildingPlayer)
                {
                    WeavrDebug.Log(procedure, "The references will not be updated: The Editor application is building");
                    updateReferences = false;
                }

                if (updateReferences)
                {
                    referenceTable = procedure.Graph.ReferencesTable;
                    procedure.Graph.MuteEvents = true;
                    procedure.Graph.ReferencesTable = null;
                    procedure.Graph.MuteEvents = false;
                }

                FullSaveRecursive(procedure, procedure.Graph, allAssets, newlyAdded, hideFlags);
                FullSaveRecursive(procedure, procedure.Configuration, allAssets, newlyAdded, hideFlags);
                FullSaveRecursive(procedure, procedure.LocalizationTable, allAssets, newlyAdded, hideFlags);

                //Debug.Log($"Saved all assets at {DateTime.Now} with elapsed: {(DateTime.Now - now).TotalMilliseconds} ms");
                //now = DateTime.Now;

                // Assign the correct procedure to all procedure objects
                foreach (var asset in newlyAdded)
                {
                    if (asset is ProcedureObject pObj)
                    {
                        pObj.Procedure = procedure;
                        //var sObj = new SerializedObject(pObj);
                        //sObj.Update();
                        //sObj.FindProperty("m_procedure").objectReferenceValue = procedure;
                        //sObj.ApplyModifiedProperties();
                    }
                }

                if (updateReferences && referenceTable)
                {
                    procedure.Graph.MuteEvents = true;
                    procedure.Graph.ReferencesTable = referenceTable;
                    procedure.Graph.MuteEvents = false;

                    int referencesCount = referenceTable.References.Count;
                    procedureScene = GetScene(referenceTable.SceneData);
                    if (procedureScene.IsValid() && procedureScene.isLoaded)
                    {
                        procedure.Graph.ReferencesTable.Procedure = procedure;
                        procedure.Graph.ReferencesTable.Save(newlyAdded.Where(e => e is ProcedureObject).Cast<ProcedureObject>());
                    }
                    FullSaveRecursive(procedure.Graph, procedure.Graph.ReferencesTable, allAssets, newlyAdded, hideFlags);

                    // Either less the 20% of references changed (not a full ref loss) or no references were previously present then backup the references
                    if(referencesCount == 0 || Mathf.Abs(referenceTable.References.Count - referencesCount) / (float)referencesCount < 0.2f)
                    {
                        referenceTable.BackupReferences();
                        WeavrDebug.Log(procedure, "All references have been backed up successfully");
                    }
                }

                //Debug.Log($"Saved all references at {DateTime.Now} with elapsed: {(DateTime.Now - now).TotalMilliseconds} ms");
                //now = DateTime.Now;

                foreach (var asset in allAssets.Except(newlyAdded))
                {
                    Object.DestroyImmediate(asset, true);
                }

                procedure.SaveUpdateTime();
            }
            catch(Exception e)
            {
                Debug.LogError($"Unable to complete saving the procedure '{procedure}' due to {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }

            //Debug.Log($"Ended entire saving process at {DateTime.Now} with elapsed: {(DateTime.Now - start).TotalMilliseconds} ms");
        }

        public static void SaveUpdateTime(this Procedure procedure)
        {
            try
            {
                var serializedObject = new SerializedObject(procedure);
                serializedObject.FindProperty("m_lastUpdate").longValue = DateTime.Now.Ticks;
                serializedObject.ApplyModifiedProperties();
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(procedure, ex);
            }
        }

        public static void SaveToProcedure(this ProcedureObject pObj, Procedure procedure)
        {
            if (!procedure || !AssetDatabase.Contains(procedure)) { return; }
            
            FullSaveRecursive(procedure, pObj, new HashSet<Object>(), new HashSet<Object>() { procedure }, procedure.hideFlags);
        }

        private static void FullSaveRecursive(ScriptableObject parent, ScriptableObject obj, 
            HashSet<Object> alreadyAdded, HashSet<Object> alreadyProcessed, HideFlags hideFlags)
        {
            if (!obj || alreadyProcessed.Contains(obj) || obj is Procedure) { return; }
            if (!alreadyAdded.Contains(obj) && !AssetDatabase.Contains(obj))
            {
                obj.hideFlags = hideFlags;
                try
                {
                    AssetDatabase.AddObjectToAsset(obj, parent);
                }
                catch (Exception e)
                {
                    WeavrDebug.LogError(obj, $"Full Save: Problem with {parent} -> {obj}   ---> {e.Message}");
                }
            }
            alreadyProcessed.Add(obj);

            var serObj = new SerializedObject(obj);
            serObj.Update();
            var property = serObj.FindProperty("m_Script");
            while (property.Next(property.propertyType == SerializedPropertyType.Generic))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue is ScriptableObject sObj)
                {
                    FullSaveRecursive(obj, sObj, alreadyAdded, alreadyProcessed, hideFlags);
                }
            }
        }

        public static void Save(this ReferenceTable table, params ProcedureObject[] objects)
        {
            int currentReferences = table.References.Count;
            table.Clear(false);
            // Update also the scene data
            table.SceneData.ValidateSceneData();
            foreach (var child in objects)
            {
                child.SaveRecursive(table);
            }

            if(table.References.Count < currentReferences / 2)
            {
                table.TryRestorePreviousReferences();
            }
        }
        
        public static void Save(this ReferenceTable table, IEnumerable<ProcedureObject> objects)
        {
            int currentReferences = table.References.Count;
            table.Clear(table.SceneData.ResolveScene().IsValid());
            // Update also the scene data
            table.SceneData.ValidateSceneData();
            foreach (var obj in objects)
            {
                obj.SaveRecursive(table);
            }

            if (table.References.Count < currentReferences / 2)
            {
                table.TryRestorePreviousReferences();
            }
        }

        private static void SaveRecursive(this ProcedureObject obj, ReferenceTable table)
        {
            if (!obj)
            {
                Debug.Log($"[ReferenceTable]: Unable to save reference because specified ProcedureObject is null");
                return;
            }
            using (SerializedObject serObj = new SerializedObject(obj))
            {
                if (serObj != null)
                {
                    serObj.Update();
                    var property = serObj.FindProperty("m_Script");
                    while (property.Next(property.propertyType == SerializedPropertyType.Generic))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference 
                            && (property.objectReferenceValue is Component || property.objectReferenceValue is GameObject)
                            && !PrefabUtility.IsPartOfPrefabAsset(property.objectReferenceValue))
                        {
                            var item = table.GetItem(property.objectReferenceValue);
                            item.AddTarget(obj.MakeTarget(property.propertyPath));
                        }
                    }
                }
            }
        }

        public static ReferenceTable CreateChunk(this ReferenceTable table, IEnumerable<ProcedureObject> objects)
        {
            var chunk = ReferenceTable.Create();
            chunk.Save(objects);
            return chunk;
        }

        public static void CloneInternals(this ProcedureObject obj)
        {
            if (!obj) { return; }

            var clones = new Dictionary<ProcedureObject, ProcedureObject>();
            var serObj = new SerializedObject(obj);
            serObj.Update();
            var property = serObj.FindProperty("m_guid");
            bool enterChildren = false;
            while(property.Next(enterChildren 
                && property.propertyType != SerializedPropertyType.String 
                && property.propertyType != SerializedPropertyType.ObjectReference))
            {

                enterChildren = true;
                if (property.propertyType == SerializedPropertyType.ObjectReference 
                    && property.objectReferenceValue is ProcedureObject 
                    && property.GetAttribute<DoNotCloneAttribute>() == null)
                {
                    property.objectReferenceValue = (property.objectReferenceValue as ProcedureObject).CloneInternal(clones);
                }
                else if(property.isArray 
                    && property.arraySize > 0 
                    && property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference
                    && property.GetAttribute<DoNotCloneAttribute>() == null)
                {
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var nextObj = property.GetArrayElementAtIndex(i).objectReferenceValue as ProcedureObject;
                        if(nextObj != null)
                        {
                            property.GetArrayElementAtIndex(i).objectReferenceValue = nextObj.CloneInternal(clones);
                        }
                    }
                    enterChildren = false;
                }
            }
            serObj.ApplyModifiedProperties();
        }

        private static T CloneInternal<T>(this T source, Dictionary<ProcedureObject, ProcedureObject> clones) where T : ProcedureObject
        {
            if(clones.TryGetValue(source, out ProcedureObject previousClone) && previousClone)
            {
                return previousClone as T;
            }

            T clone = Object.Instantiate(source);
            clones[source] = clone;
            var serObj = new SerializedObject(clone);
            serObj.Update();
            var property = serObj.FindProperty("m_guid");
            bool enterChildren = true;
            while (property.Next(enterChildren
                && property.propertyType != SerializedPropertyType.String
                && property.propertyType != SerializedPropertyType.ObjectReference))
            {

                enterChildren = true;
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue is ProcedureObject
                    && property.GetAttribute<DoNotCloneAttribute>() == null)
                {
                    property.objectReferenceValue = (property.objectReferenceValue as ProcedureObject).CloneInternal(clones);
                }
                else if (property.isArray
                    && property.arraySize > 0
                    && property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference
                    && property.GetAttribute<DoNotCloneAttribute>() == null)
                {
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        var nextObj = property.GetArrayElementAtIndex(i).objectReferenceValue as ProcedureObject;
                        if (nextObj != null)
                        {
                            property.GetArrayElementAtIndex(i).objectReferenceValue = nextObj.CloneInternal(clones);
                        }
                    }
                    enterChildren = false;
                }
            }

            if (serObj.ApplyModifiedProperties())
            {
                clone.Refresh();
            }
            return clone;
        }


        //----------------------------------- GRAPH RELATED ----------------------------------------------

        

        //----------------------------------- PROCEDURE RELATED ----------------------------------------------

        public static void FixInternalProcedureReferences(this Procedure procedure, bool silently = true)
        {
            if (!procedure) { return; }

            AssignProcedureRecursive(procedure, procedure, new HashSet<Object>(), silently);
        }

        public static void AssignProcedureToTree(this ProcedureObject procedureObject, Procedure procedure, bool silently = false, bool addToAssets = false)
        {
            if (!procedureObject) { return; }

            AssignProcedureRecursive(procedure, procedureObject, new HashSet<Object>() { procedure }, silently);
            if (addToAssets)
            {
                procedureObject.SaveToProcedure(procedure);
            }
        }

        public static void AssignProcedureToTree(this IEnumerable<ProcedureObject> procedureObjects, Procedure procedure, bool silently = false, bool addToAssets = false)
        {
            foreach (var procedureObject in procedureObjects)
            {
                if (procedureObject)
                {
                    AssignProcedureRecursive(procedure, procedureObject, new HashSet<Object>() { procedure }, silently);
                    if (addToAssets)
                    {
                        procedureObject.SaveToProcedure(procedure);
                    }
                }
            }
        }

        private static void AssignProcedureRecursive(Procedure procedure, ProcedureObject target,  HashSet<Object> alreadyProcessed, bool silently)
        {
            if (!target || alreadyProcessed.Contains(target) || (target is Procedure otherProcedure && otherProcedure != procedure)) { return; }
            alreadyProcessed.Add(target);

            var serObj = new SerializedObject(target);
            serObj.Update();
            if (silently)
            {
                serObj.FindProperty("m_procedure").objectReferenceValue = procedure;
                serObj.ApplyModifiedProperties();
            }
            else
            {
                var oldProcedure = target.Procedure;
                serObj.FindProperty("m_procedure").objectReferenceValue = procedure;
                serObj.ApplyModifiedProperties();
                if(oldProcedure != procedure)
                {
                    target.PropagateProcedure();
                }
            }
            var property = serObj.FindProperty("m_Script");
            while (property.Next(property.propertyType == SerializedPropertyType.Generic))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue != procedure && property.objectReferenceValue is ProcedureObject pObj)
                {
                    AssignProcedureRecursive(procedure, pObj, alreadyProcessed, silently);
                }
            }
        }

        public static string GetProcedureDataPath(this ProcedureObject pObj)
        {
            string path = null;
            if(!pObj || !pObj.Procedure || !AssetDatabase.Contains(pObj.Procedure))
            {
                return Weavr.ProceduresDataFullPath;
            }
            else
            {
                path = System.IO.Path.Combine(pObj.Procedure.GetFullAssetDirectory(), "Data");
            }
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            return path;
        }

        public static Scene GetScene(this Procedure procedure)
        {
            return SceneManager.GetSceneByPath(procedure.ScenePath);
        }

        public static Scene GetScene(SceneData sceneData)
        {
            var scene = SceneManager.GetSceneByPath(sceneData.Path);
            return scene;// SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(""));
        }

        public static bool TryFixSceneReference(this Procedure procedure)
        {
            if(!string.IsNullOrEmpty(procedure.ScenePath) 
                && !string.IsNullOrEmpty(procedure.SceneName))
            {
                return true;
            }

            Scene scene  = default;
            if (!string.IsNullOrEmpty(procedure.ScenePath))
            {
                scene = SceneManager.GetSceneByPath(procedure.ScenePath);
            }
            else if(!string.IsNullOrEmpty(procedure.SceneName))
            {
                scene = SceneManager.GetSceneByName(procedure.SceneName);
            }

            if (scene.IsValid())
            {
                procedure.Graph.ReferencesTable.SceneData.ForceUpdate(scene, GetSceneGuid(scene.path));
                return true;
            }
            return false;
        }

        private static string GetSceneGuid(string scenePath)
        {
            var sceneObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            return sceneObject && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sceneObject, out string guid, out long fileId) ? guid : null;
        }

        public static string GetFullPath(this Procedure procedure)
        {
            return procedure ? Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets"), 6) + AssetDatabase.GetAssetPath(procedure) : Weavr.ProceduresFullPath;
        }

        //----------------------------------- PROCEDURE OBJECT TARGETTING ----------------------------------------------

        public static void TryAssignSceneReferences(this ProcedureObject obj, IEnumerable<ProcedureObject> objects, bool startFromEnd = true)
        {
            TryAssignReferences(obj, objects, startFromEnd, typeof(Component), typeof(GameObject));
        }

        public static void TryAssignReferences(this ProcedureObject obj, IEnumerable<ProcedureObject> objects, bool startFromEnd = true, params Type[] allowedTypes)
        {
            HashSet<object> checkedObjects = new HashSet<object>()
            {
                obj,
            };
            if (startFromEnd)
            {
                objects = objects.Reverse();
            }

            bool filterType = allowedTypes != null && allowedTypes.Length > 0;
            SerializedObject serObj = new SerializedObject(obj);
            serObj.Update();
            var property = serObj.FindProperty("m_guid");
            while (property.Next(property.propertyType == SerializedPropertyType.Generic))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && !property.objectReferenceValue)
                {
                    Type type = property.GetPropertyType();
                    if (!filterType || allowedTypes.Any(t => t.IsAssignableFrom(type)))
                    {
                        if(type == typeof(GameObject))
                        {
                            foreach (var elem in objects)
                            {
                                if (TryAssignReferences(typeof(Component), elem, checkedObjects, out Object result))
                                {
                                    property.objectReferenceValue = (result as Component).gameObject;
                                    continue;
                                }
                            }
                        }
                        else if (type.IsSubclassOf(typeof(Component)))
                        {
                            foreach (var elem in objects)
                            {
                                if (TryAssignReferences(typeof(GameObject), elem, checkedObjects, out Object result))
                                {
                                    property.objectReferenceValue = (result as GameObject).GetComponent(type);
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            foreach (var elem in objects)
                            {
                                if (TryAssignReferences(type, elem, checkedObjects, out Object result))
                                {
                                    property.objectReferenceValue = result;
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            serObj.ApplyModifiedProperties();
        }

        private static bool TryAssignReferences(Type refType, Object sourceObject, HashSet<object> checkedObjects, out Object result)
        {
            result = null;
            if (checkedObjects.Contains(sourceObject))
            {
                return false;
            }
            else if (refType.IsAssignableFrom(sourceObject.GetType()))
            {
                result = sourceObject;
                return true;
            }
            checkedObjects.Add(sourceObject);
            SerializedObject serObj = new SerializedObject(sourceObject);
            serObj.Update();
            var property = serObj.GetIterator();
            property.Next(true);
            while (property.Next(property.propertyType == SerializedPropertyType.Generic))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference
                    && property.objectReferenceValue != null)
                {
                    if (refType.IsAssignableFrom(property.objectReferenceValue.GetType())){
                        result = property.objectReferenceValue;
                        return true;
                    }
                    else if(TryAssignReferences(refType, property.objectReferenceValue, checkedObjects, out result))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
