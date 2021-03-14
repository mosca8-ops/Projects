using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR
{
    [Serializable]
    public abstract class PlainList { }

    [Serializable]
    public class PlainList<T> : PlainList, IList<T>
    {
        [SerializeField]
        private bool m_circular;
        [SerializeField]
        private List<T> m_values;

        [NonSerialized]
        private int m_currentIndex = 0;

        public bool Invalidate { get => false; set => m_currentIndex = -1; }

        public bool IsCircular { get => m_circular; set => m_circular = value; }

        public int CurrentIndex 
        { 
            get => m_currentIndex;
            set
            {
                if (m_circular)
                {
                    if (value < 0)
                    {
                        m_currentIndex = Mathf.Min(m_values.Count - Mathf.Abs(value % m_values.Count), m_values.Count - 1);
                    }
                    else if (value >= m_values.Count)
                    {
                        m_currentIndex = value % m_values.Count;
                    }
                    else
                    {
                        m_currentIndex = value;
                    }
                }
                else
                {
                    m_currentIndex = Mathf.Clamp(value, 0, m_values.Count - 1);
                }
            }
        }

        public int IncrementIndex { get => m_currentIndex; set => CurrentIndex += value; }
        public int DecrementIndex { get => m_currentIndex; set => CurrentIndex -= value; }

        public T Current
        {
            get => 0 <= m_currentIndex && m_currentIndex < m_values.Count ? m_values[m_currentIndex] : default;
            set
            {
                if (m_currentIndex < m_values.Count)
                {
                    m_values[m_currentIndex] = value;
                }
            }
        }

        public T PeekNext => m_currentIndex < m_values.Count - 1 ? m_values[m_currentIndex + 1] : m_circular && m_values.Count > 0 ? m_values[0] : default;

        public T PeekPrevious => 0 < m_currentIndex && m_currentIndex < m_values.Count ? m_values[m_currentIndex - 1] : m_values.Count > 0 ? m_values[m_values.Count - 1] : default;

        public void MoveNext() => CurrentIndex++;

        public void MovePrevious() => CurrentIndex--;

        #region [  ILIST IMPLEMENTATION  ]

        public T this[int index] { get => m_values[index]; set => m_values[index] = value; }

        public int Count => m_values.Count;

        public bool IsReadOnly => false;

        public void Add(T item) => m_values.Add(item);

        public void Clear() => m_values.Clear();

        public bool Contains(T item) => m_values.Contains(item);

        public bool Contains(object value) => value is T tvalue && m_values.Contains(tvalue);

        public void CopyTo(T[] array, int arrayIndex) => m_values.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => m_values.GetEnumerator();

        public int IndexOf(T item) => m_values.IndexOf(item);

        public void Insert(int index, T item) => m_values.Insert(index, item);

        public bool Remove(T item) => m_values.Remove(item);

        public void RemoveAt(int index) => m_values.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => m_values.GetEnumerator();

        #endregion
    }

    [Serializable] public class PlainListBool : PlainList<bool> { }
    [Serializable] public class PlainListInt : PlainList<int> { }
    [Serializable] public class PlainListFloat : PlainList<float> { }
    [Serializable] public class PlainListString : PlainList<string> { }
    [Serializable] public class PlainListGameObject : PlainList<GameObject> { }
}