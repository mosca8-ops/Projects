using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    public class WeavrQuery
    {
        private List<IQueryUnit> m_queryUnits;

        public WeavrQuery()
        {
            m_queryUnits = new List<IQueryUnit>();
        }

        public T GetComponentByGuid<T>(Guid guid) where T : Component
        {
            var go = GuidManager.GetGameObject(guid);
            return go ? go.GetComponent<T>() : null;
        }

        public GameObject GetGameObjectByGuid(Guid guid)
        {
            return GuidManager.GetGameObject(guid);
        }

        public T GetByGuid<T>(QuerySearchType searchType, Guid guid)
        {
            if (searchType.HasFlag(QuerySearchType.Scene))
            {
                // Try to find in scene

            }
            else if (searchType.HasFlag(QuerySearchType.Procedure))
            {
                // Try to find in procedure
            }
            else if (searchType.HasFlag(QuerySearchType.Resource))
            {
                // Try to find in resources
            }

            return GuidManager.GetObject(guid) is T tResult ? tResult : default;
        }

        public IQuery<T> Find<T>(QuerySearchType searchType, string searchValue)
        {
            searchValue = searchValue.TrimStart();

            foreach (var unit in m_queryUnits)
            {
                if (unit.CanHandleSearchType(searchType))
                {
                    return unit.Query<T>(searchType, searchValue);
                }
            }
            return EmptyQuery<T>.Shared;
        }

        internal void Register(IQueryUnit queryUnit)
        {
            if (!m_queryUnits.Contains(queryUnit))
            {
                m_queryUnits.Add(queryUnit);
            }
        }

        internal void Unregister(IQueryUnit queryUnit)
        {
            m_queryUnits.Remove(queryUnit);
        }

        private class CompoundQuery<T> : IQuery<T>
        {
            #region [  PRIVATE MEMBERS  ]
            private IEnumerator<T> m_enumerator;
            private List<Func<IEnumerator<T>>> m_enumeratorGenerators;
            private T m_firstElement;
            private List<T> m_list;
            #endregion

            public IQueryUnit Creator { get; private set; }

            public bool IsStillValid { get; set; }

            public CompoundQuery(IQueryUnit creator, List<Func<IEnumerator<T>>> enumeratorGenerators)
            {
                Creator = creator;
                m_enumeratorGenerators = enumeratorGenerators;
                m_list = new List<T>();
            }

            public CompoundQuery(IQueryUnit creator, List<IEnumerable<T>> enumerables)
            {
                Creator = creator;
                List<Func<IEnumerator<T>>> generators = new List<Func<IEnumerator<T>>>();
                foreach (var enumerable in enumerables)
                    generators.Add(enumerable.GetEnumerator);
                m_enumeratorGenerators = generators;
                m_list = new List<T>();
            }

            public T First()
            {
                InitializeIfNeeded();
                return m_firstElement;
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

            public T GetElementAt(int index)
            {
                InitializeIfNeeded();
                if (m_list.Count == 0) { return default; }

                if (index >= 0)
                {
                    while (index >= m_list.Count && m_enumerator.MoveNext())
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

            public IEnumerator<T> GetEnumerator()
            {
                List<T> enumerator = new List<T>();
                foreach (var generator in m_enumeratorGenerators)
                    enumerator.Add((T)generator());

                return enumerator as IEnumerator<T>;
            }

            public bool HasAny()
            {
                InitializeIfNeeded();
                return m_list.Count > 0;
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

            public IList<T> ToList()
            {
                InitializeIfNeeded();
                while (m_enumerator.MoveNext())
                {
                    m_list.Add(m_enumerator.Current);
                }
                return new List<T>(m_list);
            }

            private void InitializeIfNeeded()
            {
                if (m_enumerator == null)
                {
                    m_enumerator = GetEnumerator();

                    if (m_enumerator.MoveNext())
                    {
                        m_firstElement = m_enumerator.Current;
                        m_list.Add(m_firstElement);
                    }
                }
            }
        }
    }
}