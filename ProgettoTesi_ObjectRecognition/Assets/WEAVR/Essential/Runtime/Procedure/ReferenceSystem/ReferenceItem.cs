using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ReferenceItem : ScriptableObject
    {
        [SerializeField]
        private UnityEngine.Object m_reference;
        [SerializeField]
        private Type m_type;
        [SerializeField]
        private string m_scenePath;
        [SerializeField]
        private string m_typename;
        [SerializeField]
        private string m_uniqueId;
        [SerializeField]
        private List<ReferenceTarget> m_targets;

        public string Id => m_uniqueId;
        public IReadOnlyList<ReferenceTarget> Targets => m_targets;

        public string referenceScenePath => m_scenePath;
        
        public UnityEngine.Object reference
        {
            get { return m_reference; }
            set
            {
                if(m_reference != value)
                {
                    m_reference = value;
                    if(value)
                    {
                        m_type = value.GetType();
                        m_typename = m_type.AssemblyQualifiedName;
                        m_scenePath = SceneTools.GetGameObjectPath(value);
                        if(value is GameObject)
                        {
                            m_uniqueId = IDBookkeeper.Register(value as GameObject);
                        }
                        else if(value is Component)
                        {
                            m_uniqueId = IDBookkeeper.Register(value as Component);
                        }
                        else
                        {
                            m_uniqueId = string.Empty;
                        }
                        InjectAll();
                    }
                    else
                    {
                        m_type = null;
                        m_typename = m_uniqueId = string.Empty;
                    }
                }
            }
        }

        public bool IsValid => !string.IsNullOrEmpty(m_typename) && !string.IsNullOrEmpty(m_uniqueId);


        public Type type
        {
            get
            {
                if(m_type == null && !string.IsNullOrEmpty(m_typename))
                {
                    m_type = Type.GetType(m_typename);
                }
                return m_type;
            }
        }

        private void OnEnable()
        {
            if (m_targets == null)
            {
                m_targets = new List<ReferenceTarget>();
            }
            //CleanUp();
        }

        public void CleanUp()
        {
            if (m_targets != null)
            {
                for (int i = 0; i < m_targets.Count; i++)
                {
                    if (!m_targets[i])
                    {
                        m_targets.RemoveAt(i--);
                    }
                }
            }
        }

        public bool Resolve()
        {
            return Application.isEditor ? ResolveEditor() : ResolveRuntime();
        }

        private bool ResolveEditor()
        {
            if (m_reference || string.IsNullOrEmpty(m_typename))
            {
                return true;
            }
            GameObject key = null;
            if (!string.IsNullOrEmpty(m_uniqueId))
            {
                key = IDBookkeeper.Get(m_uniqueId);
            }
            if (!key && !string.IsNullOrEmpty(m_scenePath))
            {
                WeavrDebug.Log(this, $"Broken Reference Detected!! object has scene path [{m_scenePath}] but not unique id. <b>Automatic Fixing</b> will be applied!");
                key = SceneTools.GetGameObjectAtScenePath(m_scenePath);
                if (!key)
                {
                    WeavrDebug.Log(this, $"Looks like the object at path [{m_scenePath}] is missing, so the reference is broken completely");
                    return false;
                }
                m_uniqueId = IDBookkeeper.GetUniqueID(key.GetInstanceID());
                WeavrDebug.Log(this, $"Applied new unique id: {m_uniqueId}");
            }

            if (key)
            {
                m_type = Type.GetType(m_typename);
                if (m_type != null)
                {
                    if (m_type == typeof(GameObject))
                    {
                        m_reference = key;
                    }
                    else if (m_type.IsSubclassOf(typeof(Component)))
                    {
                        m_reference = key.GetComponent(m_type);
                    }
                }
                if (m_reference)
                {
                    m_type = m_reference.GetType();
                    m_typename = m_type.AssemblyQualifiedName;

                    return true;
                }
            }
            return false;
        }

        private bool ResolveRuntime()
        {
            if (m_reference || string.IsNullOrEmpty(m_typename) || string.IsNullOrEmpty(m_uniqueId))
            {
                return true;
            }
            var key = IDBookkeeper.Get(m_uniqueId);
            if (key)
            {
                m_type = Type.GetType(m_typename);
                if (m_type != null)
                {
                    if (m_type == typeof(GameObject))
                    {
                        m_reference = key;
                    }
                    else if (m_type.IsSubclassOf(typeof(Component)))
                    {
                        m_reference = key.GetComponent(m_type);
                    }
                }
                if (m_reference)
                {
                    m_type = m_reference.GetType();
                    m_typename = m_type.AssemblyQualifiedName;

                    return true;
                }
            }
            return false;
        }

        public bool Merge(ReferenceItem other)
        {
            if (!other || Id != other.Id || m_typename != other.m_typename) { return false; }
            foreach (var target in other.m_targets)
            {
                if (!ContainsTarget(target.Target))
                {
                    m_targets.Add(target);
                }
            }
            return true;
        }

        public bool ForceMerge(ReferenceItem other)
        {
            foreach (var target in other.m_targets)
            {
                if (!ContainsTarget(target.Target))
                {
                    m_targets.Add(target);
                }
            }
            return true;
        }

        public bool ContainsTarget(ProcedureObject target)
        {
            for (int i = 0; i < m_targets.Count; i++)
            {
                if(m_targets[i].Target == target)
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveTarget(ProcedureObject target, string fieldPath)
        {
            for (int i = 0; i < m_targets.Count; i++)
            {
                if (m_targets[i].Target == target && m_targets[i].FieldPath == fieldPath)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(m_targets[i]);
                    }
                    else
                    {
                        DestroyImmediate(m_targets[i], true);
                    }
                    m_targets.RemoveAt(i);
                    return;
                }
            }
        }

        public void RemoveTarget(ProcedureObject target)
        {
            for (int i = 0; i < m_targets.Count; i++)
            {
                if (m_targets[i].Target == target)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(m_targets[i]);
                    }
                    else
                    {
                        DestroyImmediate(m_targets[i], true);
                    }
                    m_targets.RemoveAt(i--);
                }
            }
        }

        private bool ContainsSameTargetAndField(ReferenceTarget target)
        {
            for (int i = 0; i < m_targets.Count; i++)
            {
                if (m_targets[i].Target == target.Target && m_targets[i].FieldPath == target.FieldPath)
                {
                    return true;
                }
            }
            return false;
        }

        public static ReferenceItem Create(UnityEngine.Object reference)
        {
            var refItem = CreateInstance<ReferenceItem>();
            refItem.reference = reference;
            return refItem;
        }


        public void ClearTargets()
        {
            if(m_targets != null)
            {
                m_targets.Clear();
            }
        }

        public bool AddTarget(ReferenceTarget target)
        {
            if (target && target.Target && !ContainsSameTargetAndField(target))
            {
                m_targets.Add(target);
                return true;
            }
            return false;
        }

        public static ReferenceItem Deserialize(string serializedVersion, Procedure targetProcedure)
        {
            return SerializedReferenceItem.Deserialize(serializedVersion, targetProcedure);
        }

        public string Serialize()
        {
            return SerializedReferenceItem.Serialize(this);
        }

        public void InjectAll()
        {
            //Resolve();
            if(!m_reference) { return; }
            for (int i = 0; i < m_targets.Count; i++)
            {
                if (m_targets[i])
                {
                    m_targets[i].Inject(m_reference);
                    if (Application.isEditor && !m_targets[i].Target)
                    {
                        WeavrDebug.LogError(this, $"Removed target {m_targets[i]} because it is corruptued");
                        m_targets.RemoveAt(i--);
                    }
                }
                else
                {
                    m_targets.RemoveAt(i--);
                }
            }
        }

        [Serializable]
        private struct SerializedReferenceItem
        {
            public string scenePath;
            public string typeName;
            public string uniqueId;

            public string[] targets;

            public static string Serialize(ReferenceItem item)
            {
                return JsonUtility.ToJson(new SerializedReferenceItem
                {
                    scenePath = item.m_scenePath,
                    typeName = item.m_typename,
                    uniqueId = item.m_uniqueId,
                    targets = item.m_targets.Select(t => t.Serialize()).ToArray(),
                });
            }

            public static ReferenceItem Deserialize(string json, Procedure targetProcedure)
            {
                var serItem = JsonUtility.FromJson<SerializedReferenceItem>(json);
                var item = CreateInstance<ReferenceItem>();
                item.m_scenePath = serItem.scenePath;
                item.m_typename = serItem.typeName;
                item.m_uniqueId = serItem.uniqueId;

                item.m_targets = serItem.targets.Select(s => ReferenceTarget.Deserialize(s, targetProcedure)).Where(t => t && t.Target).ToList();

                return item;
            }
        }
    }
}
