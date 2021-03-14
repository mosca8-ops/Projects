using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{

    public class GuidQueryUnit : IQueryUnit
    {
        public string UnitName => throw new NotImplementedException();

        public bool CanHandleSearchType(QuerySearchType searchType)
        {
            throw new NotImplementedException();
        }

        public IQuery<T> Query<T>(QuerySearchType searchType, string searchString)
        {
            throw new NotImplementedException();
        }

        public IQuery<T> Query<T>(QuerySearchType searchType, Func<T, bool> searchFunction)
        {
            throw new NotImplementedException();
        }

        public IQuery<T> Query<T>(QuerySearchType searchType, string searchString, CompareOptions options)
        {
            throw new NotImplementedException();
        }

        private class GuidQuery<T> : IQuery<T>
        {
            public IQueryUnit Creator { get; private set; }

            public T Element { get; private set; }
            public Guid Guid { get; private set; }

            private List<T> m_tempList;

            public bool IsStillValid { get; set; }

            public GuidQuery(IQueryUnit creator, Guid guid, T element)
            {
                Creator = creator;
                Element = element;
                Guid = guid;
                m_tempList = new List<T>() { Element };
            }

            public T First() => Element;

            public T First(Func<T, bool> predicate) => predicate(Element) ? Element : default;

            public T GetElementAt(int index) => Element;

            public IEnumerator<T> GetEnumerator() => m_tempList.GetEnumerator();

            public bool HasAny() => Equals(Element, default(T));

            public T Last() => Element;

            public T Last(Func<T, bool> predicate) => predicate(Element) ? Element : default;

            public IList<T> ToList() => m_tempList;
        }
    }
}
