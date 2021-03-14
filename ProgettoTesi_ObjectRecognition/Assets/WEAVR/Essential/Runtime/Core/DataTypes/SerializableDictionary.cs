using System;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {

        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            Clear();

            if (keys.Count != values.Count)
                throw new Exception($"There are {keys.Count} keys and {values.Count} values after deserialization. Make sure that both key and value types are serializable.");

            for (int i = 0; i < keys.Count; i++)
            {
                Add(keys[i], values[i]);
            }
        }

        public T CopyFrom<T>(T allText) where T : SerializableDictionary<TKey, TValue>, new()
        {
            var result = new T();
            foreach (var t in this)
            {
                result.Add(t.Key, t.Value);
            }
            return result;
        }
    }

    [Serializable] public class DictionaryOfStringAndUnityObject : SerializableDictionary<string, UnityEngine.Object> { }
    [Serializable] public class DictionaryOfStringAndString : SerializableDictionary<string, string> { }
    [Serializable] public class DictionaryOfStringAndInt : SerializableDictionary<string, int> { }
    [Serializable] public class DictionaryOfIntAndString : SerializableDictionary<int, string> { }
}