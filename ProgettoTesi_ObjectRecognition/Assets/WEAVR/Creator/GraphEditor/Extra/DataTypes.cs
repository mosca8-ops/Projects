using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{
    struct TimeStateHandler
    {
        public float elapsed;
        private bool valid;

        public bool IsValid => valid;

        public float GetElapsed(TimerState state)
        {
            if (valid)
            {
                elapsed += state.deltaTime * 0.001f;
                return elapsed;
            }
            elapsed = state.deltaTime * 0.001f;
            valid = true;
            return elapsed;
        }

        public void Invalidate()
        {
            valid = false;
        }
    }

    #region [  SIMPLE SERIALIZATION PAIRS  ]

    [Serializable]
    class SerializationList : IEnumerable, IEnumerable<SerializationPair>
    {
        public SerializationPair[] pairs;

        private List<SerializationPair> m_pairsList;

        public SerializationList()
        {
            m_pairsList = new List<SerializationPair>();
        }

        public void Append(ScriptableObject obj)
        {
            m_pairsList.Add(new SerializationPair(obj));
        }

        public SerializationList Recover()
        {
            m_pairsList = new List<SerializationPair>(pairs);
            return this;
        }

        public SerializationList Seal()
        {
            pairs = m_pairsList.ToArray();
            return this;
        }

        public IEnumerator GetEnumerator()
        {
            return pairs.GetEnumerator();
        }

        IEnumerator<SerializationPair> IEnumerable<SerializationPair>.GetEnumerator()
        {
            return m_pairsList.GetEnumerator();
        }
    }

    [Serializable]
    struct SerializationPair
    {
        public string typename;
        public string objectJson;

        public SerializationPair(ScriptableObject obj)
        {
            typename = obj.GetType().AssemblyQualifiedName;
            objectJson = EditorJsonUtility.ToJson(obj);
        }

        public ScriptableObject GetObject()
        {
            Type type = Type.GetType(typename);
            if (typeof(ScriptableObject).IsAssignableFrom(type))
            {
                var obj = ScriptableObject.CreateInstance(type);
                EditorJsonUtility.FromJsonOverwrite(objectJson, obj);
                return obj;
            }
            return null;
        }
    }

    #endregion

}
