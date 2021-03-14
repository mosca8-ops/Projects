using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [SerializeField]
    public class IndexedList : IndexedList<UnityEngine.Object> { }

    [Serializable]
    public class IndexedList<T> : IDictionary<string, T> where T : UnityEngine.Object
    {
        [SerializeField]
        [HideInInspector]
        private bool m_isExpanded;
        [SerializeField]
        private List<IndexedPair<T>> m_pairs;
        private Dictionary<string, T> m_shadowDictionary;

        private Dictionary<string, T> ShadowDictionary
        {
            get
            {
                if(m_shadowDictionary == null)
                {
                    m_shadowDictionary = new Dictionary<string, T>();
                    foreach (var pair in m_pairs)
                    {
                        m_shadowDictionary[pair.key] = pair.value;
                    }
                }
                return m_shadowDictionary;
            }
        }

        public IndexedList()
        {
            m_pairs = new List<IndexedPair<T>>();
        }

        public T this[string key] {
            get { return ShadowDictionary[key]; }
            set {
                if (ShadowDictionary.ContainsKey(key))
                {
                    for (int i = 0; i < m_pairs.Count; i++)
                    {
                        if(m_pairs[i].key == key)
                        {
                            m_pairs[i].value = value;
                            break;
                        }
                    }
                }
                else
                {
                    m_pairs.Add(new IndexedPair<T>(key, value));
                }
                m_shadowDictionary[key] = value;
            }
        }

        public T this[int index]
        {
            get
            {
                if(index < 0 || index >= m_pairs.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                return m_pairs[index].value;
            }
            set
            {
                if (index < 0 || index >= m_pairs.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }
                m_pairs[index].value = value;
                ShadowDictionary[m_pairs[index].key] = value;
            }
        }

        public ICollection<string> Keys => ShadowDictionary.Keys;

        public ICollection<T> Values => ShadowDictionary.Values;

        public int Count => m_pairs.Count;

        public bool IsReadOnly => false;

        public void Add(string key, T value)
        {
            if (!ShadowDictionary.ContainsKey(key))
            {
                m_pairs.Add(new IndexedPair<T>(key, value));
                m_shadowDictionary.Add(key, value);
            }
        }

        public void Add(KeyValuePair<string, T> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            if(m_shadowDictionary != null)
            {
                m_shadowDictionary.Clear();
            }
            m_pairs.Clear();
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            return ShadowDictionary.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return ShadowDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (array.Length - arrayIndex < m_pairs.Count)
            {
                throw new ArgumentException("Not enough elements after arrayIndex in the destination array.");
            }
            for (int i = 0; i < m_pairs.Count; i++)
            {
                array[i + arrayIndex] = new KeyValuePair<string, T>(m_pairs[i].key, m_pairs[i].value);
            }
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return ShadowDictionary.GetEnumerator();
        }

        public bool Remove(string key)
        {
            if (ShadowDictionary.Remove(key))
            {
                for (int i = 0; i < m_pairs.Count; i++)
                {
                    if(m_pairs[i].key == key)
                    {
                        m_pairs.RemoveAt(i);
                        return true;
                    }
                }
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out T value)
        {
            return ShadowDictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ShadowDictionary.GetEnumerator();
        }
    }
    
    [Serializable]
    public abstract class AbstractIndexedPair
    {
        public Type type;

        public AbstractIndexedPair(Type type)
        {
            this.type = type;
        }
    }

    [Serializable]
    public class IndexedPair<T> : AbstractIndexedPair where T : UnityEngine.Object
    {
        public string key;
        public T value;

        public IndexedPair() : base(typeof(T)) { }

        public IndexedPair(string key, T value) : base(typeof(T))
        {
            this.key = key;
            this.value = value;
        }

        public override bool Equals(object obj)
        {
            return obj is IndexedPair<T> && ((IndexedPair<T>)obj).key == key;
        }

        public override int GetHashCode()
        {
            return key.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{{key}: {value}}}";
        }

    }

    [Serializable]
    public class IndexedPair : IndexedPair<UnityEngine.Object> { }
}
