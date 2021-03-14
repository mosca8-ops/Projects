using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Assertions;

using Setter = System.Action<object, object>;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class ReferenceTarget : ScriptableObject
    {
        #region [  STATIC PART  ]
        private static Dictionary<Type, Dictionary<string, Setter>> s_setters;
        private static Dictionary<Type, Dictionary<string, Setter>> Setters
        {
            get
            {
                if(s_setters == null)
                {
                    s_setters = new Dictionary<Type, Dictionary<string, Setter>>();
                }
                return s_setters;
            }
        }

        #endregion


        [SerializeField]
        protected ProcedureObject m_target;
        [SerializeField]
        protected string m_fieldPath;
        //[SerializeField]
        protected string m_targetGuid;

        public ProcedureObject Target => m_target;
        public string FieldPath => m_fieldPath;

        protected virtual string PathKey => m_fieldPath;

        public static ReferenceTarget Create(ProcedureObject obj, string fieldPath)
        {
            var target = CreateInstance<ReferenceTarget>();
            target.m_target = obj;
            target.m_fieldPath = fieldPath;
            target.m_targetGuid = obj.Guid;
            return target;
        }

        public void Inject(object value)
        {
            if (Application.isEditor)
            {
                InjectEditor(value);
            }
            else
            {
                InjectRuntime(value);
            }
        }

        protected virtual void InjectEditor(object value)
        {
            if (!m_target) {
                WeavrDebug.LogError(this, "Target should be non null");
                return; 
            }

            Action<object, object> setter = null;
            if (!Setters.TryGetValue(m_target.GetType(), out Dictionary<string, Setter> setterDictionary))
            {
                try
                {
                    setter = GetFieldSetter();
                }
                catch(Exception e)
                {
                    WeavrDebug.LogException(this, e);
                    m_target = null;
                }

                if (setter != null)
                {
                    setterDictionary = new Dictionary<string, Setter>
                    {
                        [PathKey] = setter
                    };
                    Setters[m_target.GetType()] = setterDictionary;
                }
            }
            else if (!setterDictionary.TryGetValue(PathKey, out setter))
            {
                try
                {
                    setter = GetFieldSetter();
                }
                catch (Exception e)
                {
                    WeavrDebug.LogException(this, e);
                    m_target = null;
                }
                if (setter != null)
                {
                    setterDictionary[PathKey] = setter;
                }
            }


            try
            {
                setter?.Invoke(m_target, value);
                m_target.Modified();
            }
            catch(Exception ex)
            {
                WeavrDebug.LogError(this, ex.Message);
            }
        }

        protected virtual void InjectRuntime(object value)
        {
            Assert.IsNotNull(m_target, "Target should be non null");
            Action<object, object> setter = null;
            if (!Setters.TryGetValue(m_target.GetType(), out Dictionary<string, Setter> setterDictionary))
            {
                setter = GetFieldSetter();
                if (setter != null)
                {
                    setterDictionary = new Dictionary<string, Setter>
                    {
                        [PathKey] = setter
                    };
                    Setters[m_target.GetType()] = setterDictionary;
                }
            }
            else if (!setterDictionary.TryGetValue(PathKey, out setter))
            {
                setter = GetFieldSetter();
                if (setter != null)
                {
                    setterDictionary[PathKey] = setter;
                }
            }

            setter?.Invoke(m_target, value);
        }

        protected virtual Setter GetFieldSetter()
        {
            return m_target.GetType().FieldPathSet(FieldPath);
        }

        
        public string Serialize()
        {
            return JsonUtility.ToJson(new SerializationWrapper()
            {
                type = this is ReferenceIndexedTarget ? 1 : 0,
                data = ToJson()
            });
        }

        protected virtual string ToJson() => $"{m_target?.Guid}|{m_fieldPath}";

        protected virtual void FromJson(string json, Procedure targetProcedure)
        {
            var split = json.Split('|');
            if(split.Length > 1)
            {
                m_target = targetProcedure.Find(split[0]);
                m_targetGuid = split[0];
                m_fieldPath = split[1];
            }
        }

        public static ReferenceTarget Deserialize(string json, Procedure targetProcedure)
        {
            var wrapper = JsonUtility.FromJson<SerializationWrapper>(json);
            if(!string.IsNullOrEmpty(wrapper.data))
            {
                var refTarget = CreateInstance(wrapper.type == 1 ? typeof(ReferenceIndexedTarget) : typeof(ReferenceTarget)) as ReferenceTarget;
                refTarget.FromJson(wrapper.data, targetProcedure);
                return refTarget;
            }
            return Old_Deserialize(json, targetProcedure);
        }

        [Serializable]
        private struct SerializationWrapper
        {
            public int type;
            public string data;
        }

        #region [  OLD SERIALIZATION - Delete when unused  ]

        public string Old_Serialize()
        {
            return JsonUtility.ToJson(new SerializedTargetWrapper()
            {
                typename = this is ReferenceIndexedTarget ? "1" : this is ReferenceTarget ? "0" : GetType().FullName,
                refTargetJson = ToJson()
            });
        }

        protected virtual string Old_ToJson()
        {
            return JsonUtility.ToJson(new SerializedReferenceTarget()
            {
                guid = m_target?.Guid,
                fieldPath = m_fieldPath,
            });
        }

        protected virtual void Old_FromJson(string json, Procedure targetProcedure)
        {
            var serItem = JsonUtility.FromJson<SerializedReferenceTarget>(json);
            m_target = targetProcedure.Find(serItem.guid);
            m_targetGuid = serItem.guid;
            m_fieldPath = serItem.fieldPath;
        }

        public static ReferenceTarget Old_Deserialize(string json, Procedure targetProcedure)
        {
            var wrapper = JsonUtility.FromJson<SerializedTargetWrapper>(json);
            var refTarget = CreateInstance(int.TryParse(wrapper.typename, out int tId) ? (tId == 1 ? typeof(ReferenceIndexedTarget) : typeof(ReferenceTarget)) : Type.GetType(wrapper.typename)) as ReferenceTarget;
            refTarget.Old_FromJson(wrapper.refTargetJson, targetProcedure);
            return refTarget;
        }
       
        [Serializable]
        [Obsolete("This class generates too much characters", false)]
        private struct SerializedTargetWrapper
        {
            public string typename;
            public string refTargetJson;
        }

        [Serializable]
        [Obsolete("This class generates too much characters", false)]
        private struct SerializedReferenceTarget
        {
            public string guid;
            public string fieldPath;
        }

        #endregion
    }
}
