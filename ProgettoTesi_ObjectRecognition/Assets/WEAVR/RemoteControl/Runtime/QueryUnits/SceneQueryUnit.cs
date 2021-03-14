using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.RemoteControl
{

    public class SceneQueryUnit : IQueryUnit
    {
        public Scene Scene { get; set; }

        public string UnitName => throw new NotImplementedException();

        public SceneQueryUnit(Scene scene)
        {
            Scene = scene;
        }

        public bool CanHandleSearchType(QuerySearchType searchType)
        {
            return searchType == QuerySearchType.Scene;
        }

        public IQuery<T> Query<T>(QuerySearchType searchType, string searchString)
        {
            if (typeof(GameObject) == typeof(T))
            {
                return new SimpleQuery<T>(this, () => CreateGameObjectEnumerator<T>(Scene.GetRootGameObjects(), searchString));
            }
            else if (typeof(Transform) == typeof(T))
            {
                return new SimpleQuery<T>(this, () => CreateTransformEnumerator<T>(Scene.GetRootGameObjects(), searchString));
            }
            else if (typeof(T).IsInterface || typeof(Component).IsAssignableFrom(typeof(T)))
            {
                return new SimpleQuery<T>(this, () => CreateEnumerator<T>(Scene.GetRootGameObjects(), searchString));
            }

            return new EmptyQuery<T>() { Creator = this };
        }

        public IQuery<T> Query<T>(QuerySearchType searchType, string searchString, CompareOptions options)
        {
            if (typeof(GameObject) == typeof(T))
            {
                return new SimpleQuery<T>(this, () => CreateGameObjectEnumerator<T>(Scene.GetRootGameObjects(), searchString, options));
            }
            else
                return new EmptyQuery<T>() { Creator = this };
        }

        public IQuery<T> Query<T>(QuerySearchType searchType, Func<T, bool> searchFunction)
        {
            if (typeof(GameObject) != typeof(T) && !typeof(T).IsInterface && !typeof(Component).IsAssignableFrom(typeof(T)))
            {
                return new EmptyQuery<T>() { Creator = this };
            }

            return new SimpleQuery<T>(this, () => CreateEnumerator(Scene.GetRootGameObjects(), searchFunction));
        }

        private IEnumerator<T> CreateEnumerator<T>(GameObject[] roots, string searchPath)
        {
            foreach (var root in roots)
            {
                T currentElem;
                if (searchPath == root.name)
                {
                    currentElem = root.GetComponent<T>();
                    if (currentElem != null)
                    {
                        yield return currentElem;
                    }
                }
                else if (searchPath.StartsWith(root.name))
                {
                    var enumerator = FindAtPath(root.transform, searchPath.Substring(root.name.Length + 1));
                    while (enumerator.MoveNext())
                    {
                        if ((currentElem = enumerator.Current.GetComponent<T>()) as UnityEngine.Object)
                        {
                            yield return currentElem;
                        }
                    }
                }
            }
        }

        private IEnumerator<T> CreateGameObjectEnumerator<T>(GameObject[] roots, string searchPath)
        {
            foreach (var root in roots)
            {
                if (searchPath == root.name)
                {
                    if (root is T go)
                    {
                        yield return go;
                    }
                }
                else if (searchPath.StartsWith(root.name))
                {
                    var enumerator = FindAtPath(root.transform, searchPath.Substring(root.name.Length + 1));
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.gameObject is T go)
                        {
                            yield return go;
                        }
                    }
                }
            }
        }

        private IEnumerator<T> CreateGameObjectEnumerator<T>(GameObject[] roots, string searchPath, CompareOptions options)
        {
            foreach (var root in roots)
            {
                var rootName = options.Apply(root.name);
                searchPath = options.Apply(searchPath);

                if (searchPath == rootName)
                {
                    if (root is T go)
                    {
                        yield return go;
                    }
                }
                else if (searchPath.StartsWith(rootName))
                {
                    var enumerator = FindAtPath(root.transform, searchPath.Substring(rootName.Length + 1), options);
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.gameObject is T go)
                        {
                            yield return go;
                        }
                    }
                }
            }
        }

        private IEnumerator<T> CreateTransformEnumerator<T>(GameObject[] roots, string searchPath)
        {
            foreach (var root in roots)
            {
                if (searchPath == root.name)
                {
                    if (root.transform is T transform)
                    {
                        yield return transform;
                    }
                }
                else if (searchPath.StartsWith(root.name))
                {
                    var enumerator = FindAtPath(root.transform, searchPath.Substring(root.name.Length + 1));
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current is T transform)
                        {
                            yield return transform;
                        }
                    }
                }
            }
        }

        private IEnumerator<T> CreateEnumerator<T>(GameObject[] roots, Func<T, bool> searchPattern)
        {
            List<T> components = new List<T>();
            for (int r = 0; r < roots.Length; r++)
            {
                components.Clear();
                roots[r].GetComponentsInChildren(true, components);
                for (int i = 0; i < components.Count; i++)
                {
                    if (searchPattern(components[i]))
                    {
                        yield return components[i];
                    }
                }
            }
        }

        private IEnumerator<Transform> FindAtPath(Transform parent, string remainingPath)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (remainingPath.StartsWith(child.name))
                {
                    if (child.name.Length == remainingPath.Length)
                    {
                        yield return child;
                    }
                    else
                    {
                        var enumerator = FindAtPath(child, remainingPath.Substring(child.name.Length + 1));
                        while (enumerator.MoveNext())
                        {
                            yield return enumerator.Current;
                        }
                    }
                }
            }
        }

        private IEnumerator<Transform> FindAtPath(Transform parent, string remainingPath, CompareOptions options)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var childName = options.Apply(child.name);
                remainingPath = options.Apply(remainingPath);

                if (remainingPath.StartsWith(childName))
                {
                    if (childName.Length == remainingPath.Length)
                    {
                        yield return child;
                    }
                    else
                    {
                        var enumerator = FindAtPath(child, remainingPath.Substring(childName.Length + 1), options);
                        while (enumerator.MoveNext())
                        {
                            yield return enumerator.Current;
                        }
                    }
                }
            }
        }
    }
}
