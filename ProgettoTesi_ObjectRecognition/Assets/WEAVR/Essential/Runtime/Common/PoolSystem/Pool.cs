using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Common
{

    public interface IPool
    {
        void Reclaim(object pooledObject);
        void Clear(bool includeActive);
    }

    public interface IPool<T> : IPool
    {
        T Get();
        void Reclaim(T obj);
    }

    public class Pool<T> : IPool<T>
    {
        public T sample;
        public GameObject container;

        public Action<T> ResetObject;
        public Func<T, T> InstantiateObject;

        private List<T> m_stack = new List<T>();
        private List<T> m_allObjects = new List<T>();

        public Pool()
        {
            InstantiateObject = DefaultInstantiate;
        }

        private T DefaultInstantiate(T arg)
        {
            if(sample is Object objSample && Object.Instantiate(objSample) is T tResult)
            {
                return tResult;
            }
            return default;
        }

        public T Get()
        {
            T obj;
            if (m_stack.Count == 0)
            {
                obj = InstantiateObject(sample);
                if (obj is Component cObj)
                {
                    var pooledObj = cObj.gameObject.AddComponent<PooledObject>();
                    pooledObj.Pool = this;
                    if (container)
                    {
                        cObj.transform.SetParent(container.transform, false);
                    }
                }
                m_allObjects.Add(obj);
            }
            else
            {
                int lastIndex = m_stack.Count - 1;
                do
                {
                    obj = m_stack[lastIndex];
                    m_stack.RemoveAt(lastIndex);
                    lastIndex--;
                }
                while (obj == null);
            }

            ResetObject?.Invoke(obj);
            if (obj is Component c)
            {
                c.gameObject.SetActive((sample as Component).gameObject.activeSelf);
            }
            return obj;
        }

        public void Reclaim(object pooledObject)
        {
            T obj = default;
            if(pooledObject is T tObject)
            {
                obj = tObject;
            }
            else if(pooledObject is GameObject go)
            {
                obj = go.GetComponent<T>();
            }
            else if(pooledObject is Component c)
            {
                obj = c.GetComponent<T>();
            }

            Reclaim(obj);
        }

        public void Clear(bool includeActive)
        {
            foreach(var obj in (includeActive ? m_allObjects : m_stack))
            {
                if (obj is Component cObj && cObj)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(cObj.gameObject);
                    }
                    else
                    {
                        Object.DestroyImmediate(cObj.gameObject);
                    }
                }
            }

            m_allObjects.Clear();
            m_stack.Clear();
        }

        public void Reclaim(T obj)
        {
            if (obj is Component cObj && cObj)
            {
                m_stack.Add(obj);
                cObj.gameObject.SetActive(false);
                if (container)
                {
                    cObj.transform.SetParent(container.transform, false);
                }
            }
            else if (obj != null)
            {
                m_stack.Add(obj);
            }
        }
    }
}
