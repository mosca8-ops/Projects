using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    #region [  SERIALIZATION NODES ]

    [Serializable]
    public class SerializationNodesList : IEnumerable, IEnumerable<SerializationNode>
    {
        const string k_serializedDataMimeType = "application/txt.weavr.procedure.elements";

        public SerializationNode[] nodes;

        private List<SerializationNode> m_nodesList;

        public static SerializationNodesList Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data)) { return null; }
            if (data.StartsWith(k_serializedDataMimeType))
            {
                return JsonUtility.FromJson<SerializationNodesList>(data.Substring(k_serializedDataMimeType.Length + 1));
            }
            else
            {
                return JsonUtility.FromJson<SerializationNodesList>(data);
            }
        }

        public static bool CanDeserialize(string data)
        {
            return data != null && data.StartsWith(k_serializedDataMimeType);
        }

        public string Serialize()
        {
            string data = JsonUtility.ToJson(this);
            if (string.IsNullOrEmpty(data)) { return null; }
            return k_serializedDataMimeType + " " + data;
        }

        public SerializationNodesList()
        {
            m_nodesList = new List<SerializationNode>();
        }

        public void Append(ScriptableObject obj)
        {
            m_nodesList.Add(new SerializationNode(obj));
        }

        public SerializationNodesList Recover()
        {
            m_nodesList = new List<SerializationNode>(nodes);
            return this;
        }

        public SerializationNodesList Seal()
        {
            nodes = m_nodesList.ToArray();
            return this;
        }

        public IEnumerator GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        IEnumerator<SerializationNode> IEnumerable<SerializationNode>.GetEnumerator()
        {
            return m_nodesList.GetEnumerator();
        }

        public List<ScriptableObject> DeserializeAll()
        {
            List<ScriptableObject> deserializedObjects = new List<ScriptableObject>();
            Dictionary<int, ScriptableObject> ids = new Dictionary<int, ScriptableObject>();
            foreach (var node in nodes)
            {
                deserializedObjects.Add(node.DeserializeObject(ids));
            }
            foreach (var node in nodes)
            {
                node.UpdateMissingLinks(ids);
            }
            return deserializedObjects;
        }
    }

    [Serializable]
    public class SerializationNode
    {
        public int id;
        public string typename;
        public string objectJson;
        public string[] childrenPaths;
        public SerializationNode[] children;

        public SerializationSceneObject[] sceneObjects;
        public SerializationPathId[] pathIds;
        //public SerializationNodePair[] children;

        public SerializationNode(ScriptableObject obj) : this(obj, new Dictionary<int, SerializationNode>(), new HashSet<Object>())
        {

        }

        private SerializationNode(ScriptableObject obj, Dictionary<int, SerializationNode> ids, HashSet<Object> alreadyVisited)
        {
            id = obj.GetInstanceID();
            typename = obj.GetType().AssemblyQualifiedName;
            objectJson = EditorJsonUtility.ToJson(obj);

            ids[id] = this;

            List<string> paths = new List<string>();
            List<SerializationNode> nodes = new List<SerializationNode>();

            List<SerializationSceneObject> sObjects = new List<SerializationSceneObject>();
            List<SerializationPathId> pIds = new List<SerializationPathId>();

            var serObj = new SerializedObject(obj);
            serObj.Update();
            var property = serObj.GetIterator();
            property.Next(true);
            bool enterChildren = false;
            while (property.Next(enterChildren))
            {
                enterChildren = property.propertyType == SerializedPropertyType.Generic;
                if (property.propertyType == SerializedPropertyType.ObjectReference || (property.isArray && property.arraySize > 0 && property.propertyType != SerializedPropertyType.String))
                {
                    var doNotClone = property.GetAttribute<DoNotCloneAttribute>();
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        //enterChildren = false;
                        var refObj = property.objectReferenceValue;
                        if (refObj is ScriptableObject)
                        {
                            if (refObj.GetType().GetCustomAttribute<DoNotCloneAttribute>() == null)
                            {
                                if (!alreadyVisited.Contains(refObj) && doNotClone == null)
                                {
                                    var pair = CreatePair(property, ids, alreadyVisited);
                                    paths.Add(pair.propertyPath);
                                    nodes.Add(pair.node);

                                    alreadyVisited.Add(refObj);
                                }
                                else
                                {
                                    pIds.Add(new SerializationPathId(refObj.GetInstanceID(), property.propertyPath, doNotClone != null && doNotClone.CutReference));
                                }
                            }
                        }
                        else if ((refObj is Component || refObj is GameObject)
                             && !PrefabUtility.IsPartOfPrefabAsset(refObj))
                        {
                            sObjects.Add(CreateSceneObject(property));
                        }
                    }
                    else if (property.GetArrayElementAtIndex(0).propertyType == SerializedPropertyType.ObjectReference
                        && doNotClone == null)
                    {
                        for (int i = 0; i < property.arraySize; i++)
                        {
                            var nextObj = property.GetArrayElementAtIndex(i).objectReferenceValue;
                            if (nextObj is ScriptableObject)
                            {
                                if (nextObj.GetType().GetCustomAttribute<DoNotCloneAttribute>() == null)
                                {
                                    if (!alreadyVisited.Contains(nextObj))
                                    {
                                        var pair = CreatePair(property.GetArrayElementAtIndex(i), ids, alreadyVisited);
                                        paths.Add(pair.propertyPath);
                                        nodes.Add(pair.node);

                                        alreadyVisited.Add(nextObj);
                                    }
                                    else
                                    {
                                        pIds.Add(new SerializationPathId(nextObj.GetInstanceID(), property.GetArrayElementAtIndex(i).propertyPath));
                                    }
                                }
                            }
                            else if ((nextObj is Component || nextObj is GameObject)
                             && !PrefabUtility.IsPartOfPrefabAsset(nextObj))
                            {
                                sObjects.Add(CreateSceneObject(property.GetArrayElementAtIndex(i)));
                            }
                        }
                        //enterChildren = false;
                    }
                    enterChildren = property.propertyType == SerializedPropertyType.Generic && doNotClone == null;
                }
            }

            //if (shouldApplyChanges)
            //{
            //    serObj.ApplyModifiedProperties();
            //}

            children = nodes.ToArray();
            childrenPaths = paths.ToArray();
            sceneObjects = sObjects.ToArray();
            pathIds = pIds.ToArray();
        }

        private static SerializationNodePair CreatePair(SerializedProperty property, Dictionary<int, SerializationNode> ids, HashSet<Object> alreadyVisited)
        {
            if (!ids.TryGetValue(property.objectReferenceInstanceIDValue, out SerializationNode node))
            {
                node = new SerializationNode(property.objectReferenceValue as ScriptableObject, ids, alreadyVisited);
            }
            return new SerializationNodePair(property.propertyPath, node);
        }

        private static SerializationSceneObject CreateSceneObject(SerializedProperty property)
        {
            if (property.objectReferenceValue is Component)
            {
                return new SerializationSceneObject(property.propertyPath, property.objectReferenceValue as Component);
            }
            return new SerializationSceneObject(property.propertyPath, property.objectReferenceValue as GameObject);
        }

        public ScriptableObject DeserializeObject()
        {
            return DeserializeInternal(new Dictionary<int, ScriptableObject>());
        }

        internal ScriptableObject DeserializeObject(Dictionary<int, ScriptableObject> ids)
        {
            return DeserializeInternal(ids ?? new Dictionary<int, ScriptableObject>());
        }

        private ScriptableObject DeserializeInternal(Dictionary<int, ScriptableObject> ids)
        {
            if (!ids.TryGetValue(id, out ScriptableObject obj) || !obj)
            {
                Type type = Type.GetType(typename);
                if(type == null)
                {
                    WeavrDebug.LogError(this, $"Unable to get deserialization type: {typename}");
                    return obj;
                }
                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    obj = ScriptableObject.CreateInstance(type);
                    if (!obj)
                    {
                        WeavrDebug.LogError(this, $"Unable to create instance of {type.Name}");
                        return obj;
                    }
                    EditorJsonUtility.FromJsonOverwrite(objectJson, obj);
                    ids[id] = obj;
                    var serObj = new SerializedObject(obj);
                    serObj.Update();
                    if (obj is ProcedureObject)
                    {
                        //serObj.FindProperty("m_procedure").objectReferenceValue = null;
                        serObj.FindProperty("m_guid").stringValue = string.Empty;
                    }
                    if (sceneObjects != null)
                    {
                        foreach (var sceneObject in sceneObjects)
                        {
                            sceneObject.AssignObject(serObj);
                        }
                    }
                    if (childrenPaths != null)
                    {
                        for (int i = 0; i < childrenPaths.Length; i++)
                        {
                            var childObject = children[i]?.DeserializeInternal(ids);
                            serObj.FindProperty(childrenPaths[i]).objectReferenceValue = childObject;
                        }
                    }
                    UpdateFromPathIds(ids, serObj);
                    if (serObj.ApplyModifiedProperties() && obj is ProcedureObject pObj && pObj)
                    {
                        pObj.Refresh();
                    }
                }
            }
            return obj;
        }

        public void UpdateMissingLinks(Dictionary<int, ScriptableObject> ids)
        {
            if (ids.TryGetValue(id, out ScriptableObject obj))
            {
                var serObj = new SerializedObject(obj);
                serObj.Update();
                UpdateFromPathIds(ids, serObj);
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        child.UpdateMissingLinks(ids);
                    }
                }
                serObj.ApplyModifiedProperties();
            }
        }

        private void UpdateFromPathIds(Dictionary<int, ScriptableObject> ids, SerializedObject serObj)
        {
            if (pathIds != null)
            {
                foreach (var pathId in pathIds)
                {
                    if (ids.TryGetValue(pathId.id, out ScriptableObject value))
                    {
                        serObj.FindProperty(pathId.propertyPath).objectReferenceValue = value;
                    }
                    else if (pathId.onlyInternal)
                    {
                        serObj.FindProperty(pathId.propertyPath).objectReferenceValue = null;
                    }
                }
            }
        }
    }

    [Serializable]
    struct SerializationNodePair
    {
        public string propertyPath;
        public SerializationNode node;

        public SerializationNodePair(string path, SerializationNode node)
        {
            propertyPath = path;
            this.node = node;
        }
    }

    [Serializable]
    public struct SerializationPathId
    {
        public int id;
        public string propertyPath;
        public bool onlyInternal;

        public SerializationPathId(int id, string propertyPath)
        {
            this.id = id;
            this.propertyPath = propertyPath;
            onlyInternal = false;
        }
        public SerializationPathId(int id, string propertyPath, bool onlyInternal)
        {
            this.id = id;
            this.propertyPath = propertyPath;
            this.onlyInternal = onlyInternal;
        }
    }

    [Serializable]
    public struct SerializationSceneObject
    {
        public string propertyPath;
        public string uniqueId;
        public string componentType;

        public SerializationSceneObject(string propertyPath, GameObject gameObject)
        {
            this.propertyPath = propertyPath;
            uniqueId = IDBookkeeper.Register(gameObject);
            componentType = string.Empty;
        }

        public SerializationSceneObject(string propertyPath, Component component)
        {
            this.propertyPath = propertyPath;
            uniqueId = IDBookkeeper.Register(component);
            componentType = component.GetType().AssemblyQualifiedName;
        }

        public void AssignObject(SerializedObject obj)
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                return;
            }

            var gameObject = IDBookkeeper.Get(uniqueId);
            if (!gameObject)
            {
                return;
            }
            if (string.IsNullOrEmpty(componentType))
            {
                obj.FindProperty(propertyPath).objectReferenceValue = gameObject;
            }
            else
            {
                Type type = Type.GetType(componentType);
                obj.FindProperty(propertyPath).objectReferenceValue = gameObject.GetComponent(type);
            }
        }
    }

    #endregion

}
