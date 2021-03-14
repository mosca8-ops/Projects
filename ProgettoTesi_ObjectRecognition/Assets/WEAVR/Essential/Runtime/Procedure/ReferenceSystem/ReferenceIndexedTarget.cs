using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.Assertions;

using Getter = System.Func<object, object>;

namespace TXT.WEAVR.Procedure
{

    public class ReferenceIndexedTarget : ReferenceTarget
    {
        #region [  STATIC PART  ]
        private static Dictionary<Type, Dictionary<string, Getter>> s_getters;
        private static Dictionary<Type, Dictionary<string, Getter>> Getters
        {
            get
            {
                if (s_getters == null)
                {
                    s_getters = new Dictionary<Type, Dictionary<string, Getter>>();
                }
                return s_getters;
            }
        }

        #endregion


        [SerializeField]
        private string m_shortFieldPath;
        [SerializeField]
        private int m_index;

        public string ShortFieldPath => m_shortFieldPath;
        public int Index => m_index;

        protected override string PathKey => m_shortFieldPath;

        public new static ReferenceIndexedTarget Create(ProcedureObject obj, string fieldPath)
        {
            var target = CreateInstance<ReferenceIndexedTarget>();
            target.m_target = obj;
            target.m_targetGuid = obj.Guid;
            target.m_fieldPath = fieldPath;
            target.m_shortFieldPath = fieldPath.Substring(0, fieldPath.IndexOf("Array.data") - 1);
            int bracketIndex = fieldPath.IndexOf('[') + 1;
            target.m_index = int.Parse(fieldPath.Substring(bracketIndex, fieldPath.IndexOf(']') - bracketIndex));
            return target;
        }

        protected override void InjectEditor(object value)
        {
            if (!m_target) {
                WeavrDebug.LogError(this, "Target should be non null");
                return; 
            }

            Func<object, object> getter = null;
            if (!Getters.TryGetValue(m_target.GetType(), out Dictionary<string, Getter> setterDictionary))
            {
                try
                {
                    getter = GetArrayGetter();
                }
                catch (Exception e)
                {
                    WeavrDebug.LogException(this, e);
                }

                if (getter != null)
                {
                    setterDictionary = new Dictionary<string, Getter>
                    {
                        [PathKey] = getter
                    };
                    Getters[m_target.GetType()] = setterDictionary;
                }
            }
            else if (!setterDictionary.TryGetValue(PathKey, out getter))
            {
                try
                {
                    getter = GetArrayGetter();
                }
                catch (Exception e)
                {
                    WeavrDebug.LogException(this, e);
                }
                if (getter != null)
                {
                    setterDictionary[PathKey] = getter;
                }
            }

            try
            {
                var array = getter?.Invoke(m_target);
                if (array is Array)
                {
                    (array as Array).SetValue(value, m_index);
                }
                else if (array is IList)
                {
                    (array as IList)[m_index] = value;
                }
                m_target.Modified();
            }
            catch (Exception ex)
            {
                WeavrDebug.LogError(this, ex.Message);
            }
        }

        protected override void InjectRuntime(object value)
        {
            Assert.IsNotNull(m_target, "Target should be non null");
            Func<object, object> getter = null;
            if (!Getters.TryGetValue(m_target.GetType(), out Dictionary<string, Getter> setterDictionary))
            {
                getter = GetArrayGetter();
                if (getter != null)
                {
                    setterDictionary = new Dictionary<string, Getter>
                    {
                        [PathKey] = getter
                    };
                    Getters[m_target.GetType()] = setterDictionary;
                }
            }
            else if (!setterDictionary.TryGetValue(PathKey, out getter))
            {
                getter = GetArrayGetter();
                if (getter != null)
                {
                    setterDictionary[PathKey] = getter;
                }
            }

            var array = getter?.Invoke(m_target);
            if (array is Array)
            {
                (array as Array).SetValue(value, m_index);
            }
            else if (array is IList)
            {
                (array as IList)[m_index] = value;
            }
        }

        protected virtual Func<object, object> GetArrayGetter()
        {
            return m_target.GetType().FieldPathGet(ShortFieldPath);
        }

        protected override string ToJson() => $"{m_target?.Guid}|{m_fieldPath}|{m_shortFieldPath}|{m_index}";

        protected override void FromJson(string json, Procedure targetProcedure)
        {
            var splits = json.Split('|');
            if(splits.Length > 2)
            {
                m_target = targetProcedure.Find(splits[0]);
                m_targetGuid = splits[0];
                m_fieldPath = splits[1];
                m_shortFieldPath = splits[2];
            }
            if(splits.Length > 3 && int.TryParse(splits[3], out int index))
            {
                m_index = index;
            }
        }

        #region [  OLD SERIALIZATION - Delete when unused  ]

        protected override string Old_ToJson()
        {
            return JsonUtility.ToJson(new SerializedReferenceTarget()
            {
                guid = m_target.Guid,
                fieldPath = m_fieldPath,
                shortFieldPath = m_shortFieldPath,
                index = m_index,
            });
        }

        protected override void Old_FromJson(string json, Procedure targetProcedure)
        {
            var serItem = JsonUtility.FromJson<SerializedReferenceTarget>(json);
            m_target = targetProcedure.Find(serItem.guid);
            m_targetGuid = serItem.guid;
            m_fieldPath = serItem.fieldPath;
            m_shortFieldPath = serItem.shortFieldPath;
            m_index = serItem.index;
        }
        
        [Serializable]
        private struct SerializedReferenceTarget
        {
            public string guid;
            public string fieldPath;
            public string shortFieldPath;
            public int index;
        }

        #endregion

    }
}
