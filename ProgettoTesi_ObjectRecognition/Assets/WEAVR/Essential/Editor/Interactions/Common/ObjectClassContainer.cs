using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine;
using System;
using UnityEditor.SceneManagement;

namespace TXT.WEAVR.Interaction
{
    [InitializeOnLoad]
    public static class ObjectClassContainer
    {
        private static Dictionary<string, int> _objectClassesCounters;
        private static HashSet<string> _classes;
        private static bool m_initialized;

        public static int Count { get { return _classes.Count; } }

        static ObjectClassContainer()
        {
            _objectClassesCounters = new Dictionary<string, int>();
            _classes = new HashSet<string>();
            SceneManager.activeSceneChanged += SceneManager_ActiveSceneChanged;
            //SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            //SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
            UpdateObjectClasses();
        }

        private static void SceneManager_SceneUnloaded(Scene scene)
        {
            RemoveFromScene(scene);
        }

        private static void SceneManager_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateFromScene(scene);
        }

        private static void SceneManager_ActiveSceneChanged(Scene current, Scene next)
        {
            UpdateObjectClasses();
        }

        private static void UpdateObjectClasses()
        {
            _objectClassesCounters.Clear();
            _classes.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                UpdateFromScene(SceneManager.GetSceneAt(i));
            }
        }

        private static void UpdateFromScene(Scene scene)
        {
            if (!scene.IsValid()) { return; }

            var rootObjects = scene.GetRootGameObjects();
            foreach(var rootObject in rootObjects) {
                foreach(var component in rootObject.GetComponentsInChildren<AbstractInteractiveBehaviour>(true)) {
                    m_initialized = true;
                    if(!component) {

                    }
                    else if(_objectClassesCounters.ContainsKey(component.ObjectClass.type)) {
                        _objectClassesCounters[component.ObjectClass.type]++;
                    }
                    else {
                        _objectClassesCounters[component.ObjectClass.type] = 1;
                        _classes.Add(component.ObjectClass.type);
                    }
                }
            }
        }

        private static void RemoveFromScene(Scene scene)
        {
            if (!scene.IsValid()) { return; }

            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                foreach (var component in rootObject.GetComponentsInChildren<AbstractInteractiveBehaviour>(true))
                {
                    if (component && _objectClassesCounters.TryGetValue(component.ObjectClass.type, out int counter))
                    {
                        if (counter > 1)
                        {
                            _objectClassesCounters[component.ObjectClass.type]++;
                        }
                        else
                        {
                            _objectClassesCounters.Remove(component.ObjectClass.type);
                        }
                    }
                }
            }
        }

        public static void Add(ObjectClass @class) {
            Add(@class.type);
        }

        public static void Add(string @class) {
            int counter = 0;
            if (_objectClassesCounters.TryGetValue(@class, out counter)) {
                _objectClassesCounters[@class]++;
            }
            else {
                _objectClassesCounters[@class] = 1;
                _classes.Add(@class);
            }
        }

        public static void Remove(ObjectClass @class) {
            Remove(@class.type);
        }

        public static void Remove(string @class) {
            int counter = 0;
            if (_objectClassesCounters.TryGetValue(@class, out counter)) {
                if (counter > 1) {
                    _objectClassesCounters[@class]--;
                }
                else {
                    _objectClassesCounters.Remove(@class);
                    _classes.Remove(@class);
                }
            }
        }

        public static bool Contains(ObjectClass @class) {
            return _classes.Contains(@class.type);
        }

        public static bool Contains(InputObjectClass @class) {
            return _classes.Contains(@class.validType);
        }

        public static bool Contains(string @class) {
            return _classes.Contains(@class);
        }

        public static IEnumerable<string> GetClasses() {
            if (!m_initialized)
            {
                UpdateObjectClasses();
            }
            return _classes;
        }
    }
}