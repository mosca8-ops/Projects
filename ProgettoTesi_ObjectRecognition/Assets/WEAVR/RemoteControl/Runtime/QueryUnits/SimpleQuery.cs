using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    /// <summary>
    /// The very basic query
    /// </summary>
    /// <typeparam name="T">The type of the element</typeparam>
    public class SimpleQuery<T> : IQuery<T>
    {

        #region [  PRIVATE MEMBERS  ]
        private IEnumerator<T> m_enumerator;
        private Func<IEnumerator<T>> m_enumeratorGenerator;
        private T m_firstElement;
        private List<T> m_list;
        #endregion

        public IQueryUnit Creator { get; private set; }

        public bool IsStillValid { get; set; }

        public SimpleQuery(IQueryUnit creator, Func<IEnumerator<T>> enumeratorGenerator)
        {
            Creator = creator;
            m_enumeratorGenerator = enumeratorGenerator;
            m_list = new List<T>();
        }

        public SimpleQuery(IQueryUnit creator, IEnumerable<T> enumerable) : this(creator, enumerable.GetEnumerator){ }

        public T First()
        {
            InitializeIfNeeded();
            return m_firstElement;
        }

        public IEnumerator<T> GetEnumerator() => m_enumeratorGenerator();

        public bool HasAny()
        {
            InitializeIfNeeded();
            return m_list.Count > 0;
        }

        public IList<T> ToList()
        {
            InitializeIfNeeded();
            while (m_enumerator.MoveNext())
            {
                m_list.Add(m_enumerator.Current);
            }
            return new List<T>(m_list);
        }

        public T First(Func<T, bool> predicate)
        {
            InitializeIfNeeded();
            if (m_list.Count == 0)
            {
                return default;
            }
            if (predicate(m_firstElement))
            {
                return m_firstElement;
            }
            for (int i = 0; i < m_list.Count; i++)
            {
                if (predicate(m_list[i]))
                {
                    return m_list[i];
                }
            }
            while (m_enumerator.MoveNext())
            {
                m_list.Add(m_enumerator.Current);
                if (predicate(m_enumerator.Current))
                {
                    return m_enumerator.Current;
                }
            }
            return default;
        }

        public T Last()
        {
            InitializeIfNeeded();
            while (m_enumerator.MoveNext())
            {
                m_list.Add(m_enumerator.Current);
            }
            return m_list[m_list.Count - 1];
        }

        public T Last(Func<T, bool> predicate)
        {
            InitializeIfNeeded();
            while (m_enumerator.MoveNext())
            {
                m_list.Add(m_enumerator.Current);
            }
            for (int i = m_list.Count - 1; i >= 0; i--)
            {
                if (predicate(m_list[i]))
                {
                    return m_list[i];
                }
            }
            return default;
        }

        public T GetElementAt(int index)
        {
            InitializeIfNeeded();
            if(m_list.Count == 0) { return default; }

            if(index >= 0)
            {
                while(index >= m_list.Count && m_enumerator.MoveNext())
                {
                    m_list.Add(m_enumerator.Current);
                }
                return index < m_list.Count ? m_list[index] : default;
            }
            else
            {
                while (m_enumerator.MoveNext())
                {
                    m_list.Add(m_enumerator.Current);
                }
                return -index > m_list.Count ? default : m_list[m_list.Count + index];
            }
        }

        private void InitializeIfNeeded()
        {
            if (m_enumerator == null)
            {
                m_enumerator = m_enumeratorGenerator();
                if (m_enumerator.MoveNext())
                {
                    m_firstElement = m_enumerator.Current;
                    m_list.Add(m_firstElement);
                }
            }
        }
    }
}
