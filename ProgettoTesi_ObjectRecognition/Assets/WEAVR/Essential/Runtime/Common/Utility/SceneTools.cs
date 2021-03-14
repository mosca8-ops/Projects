using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TXT.WEAVR.EditorBridge;
using UnityEngine;
using UnityEngine.SceneManagement;

using Object = UnityEngine.Object;

namespace TXT.WEAVR
{
    public static class SceneTools
    {
        /// <summary>
        /// Gets a local snapshot of the transform
        /// </summary>
        /// <param name="transform">The transform to get the pose from</param>
        /// <returns>The local snaphot of the transform</returns>
        public static Pose GetLocalPose(this Transform transform)
        {
            return new Pose(transform, Pose.PoseType.Local);
        }

        /// <summary>
        /// Gets a world snapshot of the transform
        /// </summary>
        /// <param name="transform">The transform to get the pose from</param>
        /// <returns>The world snaphot of the transform</returns>
        public static Pose GetWorldPose(this Transform transform)
        {
            return new Pose(transform, Pose.PoseType.World);
        }

        /// <summary>
        /// Applies the pose to the transform
        /// </summary>
        /// <param name="transform">The transofrm to apply the pose to</param>
        /// <param name="pose">The pose to be applied</param>
        public static void ApplyPose(this Transform transform, Pose pose)
        {
            pose.ApplyTo(transform);
        }

        /// <summary>
        /// Applies an offset to the transform
        /// </summary>
        /// <param name="transform">The transofrm to apply the offset to</param>
        /// <param name="offset">The offset to be applied</param>
        public static void ApplyOffset(this Transform transform, Pose offset)
        {
            offset.ApplyAsOffsetTo(transform);
        }

        /// <summary>
        /// Gets the scene hierarchy path of the specified object
        /// </summary>
        /// <param name="obj">The object to retrieve the path for</param>
        /// <param name="separator">[Optional] The separator of path levels. Default is '/'</param>
        /// <returns>The scene hierarchy path of the object or null if obj is null</returns>
        public static string GetGameObjectPath(UnityEngine.Object obj, char separator = '/') {
            return GetGameObjectPath(obj is Component ? (obj as Component).gameObject : obj as GameObject, separator);
        }

        /// <summary>
        /// Gets the scene hierarchy path of the specified object
        /// </summary>
        /// <param name="obj">The object to retrieve the path for</param>
        /// <param name="separator">[Optional] The separator of path levels. Default is '/'</param>
        /// <returns>The scene hierarchy path of the object or null if obj is null</returns>
        public static string GetGameObjectPath(GameObject obj, char separator = '/') {
            if (obj == null) {
                return null;
            }
            string path = obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = obj.name + separator + path;
            }
            return path;
        }

        /// <summary>
        /// Tries to retrieve a gameobject in the hierarchy with the specified path
        /// </summary>
        /// <param name="path">The path to the object in the hierarchy</param>
        /// <param name="separator">[Optional] The level separator in the path. Default is '/'</param>
        /// <returns>Either the found object, or null if not found</returns>
        public static GameObject GetGameObjectAtScenePath(string path, char separator = '/') {
            var parts = path.Split(new char[] { separator }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) {
                return null;
            }
            Transform currentLevelTransform = null;
            var sceneRootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            // Start the search with the root level
            foreach (var rootObject in sceneRootObjects) {
                if (rootObject.name == parts[0]) {
                    currentLevelTransform = rootObject.transform;
                    if (parts.Length == 1) {
                        // The path is only one level so return the found object
                        return rootObject;
                    }
                    break;
                }
            }
            // If not found at root level return nothing
            if (currentLevelTransform == null) {
                return null;
            }
            // Else go follow the path until the object is found
            for (int i = 1; i < parts.Length; i++) {
                bool objectFound = false;
                for (int c = 0; c < currentLevelTransform.childCount; c++) {
                    Transform child = currentLevelTransform.GetChild(c);
                    if (child.gameObject.name == parts[i]) {
                        currentLevelTransform = child;
                        objectFound = true;
                        break;
                    }
                }
                if (!objectFound) {
                    // No object found at current level, so return null
                    return null;
                }
            }

            return currentLevelTransform.gameObject; // Object has been found
        }

        /// <summary>
        /// Gets all components in scene, including from hidden objects
        /// </summary>
        /// <typeparam name="T">The type of the component to search</typeparam>
        /// <returns>All found components</returns>
        public static T[] GetComponentsInScene<T>() {
            List<T> components = new List<T>();
            foreach(var obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
                components.AddRange(obj.GetComponentsInChildren<T>(true));
            }
            return components.ToArray();
        }

        /// <summary>
        /// Gets all components in scene, including from hidden objects
        /// </summary>
        /// <typeparam name="T">The type of the component to search</typeparam>
        /// <returns>All found components</returns>
        public static T[] GetComponentsInAllScenes<T>()
        {
            List<T> components = new List<T>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (var obj in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    components.AddRange(obj.GetComponentsInChildren<T>(true));
                }
            }
            return components.ToArray();
        }

        /// <summary>
        /// Gets all components in scene, including from hidden objects
        /// </summary>
        /// <typeparam name="T">The type of the component to search</typeparam>
        /// <param name="whereClause">The filtering function</param>
        /// <returns>All found components</returns>
        public static T[] GetComponentsInScene<T>(System.Func<T, bool> whereClause) {
            List<T> components = new List<T>();
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach(var component in obj.GetComponentsInChildren<T>(true)) {
                    if (whereClause(component)) {
                        components.Add(component);
                    }
                }
            }
            return components.ToArray();
        }

        /// <summary>
        /// Get a component in scene, including from hidden objects
        /// </summary>
        /// <typeparam name="T">The type of the component to search</typeparam>
        /// <returns>The first suitable component</returns>
        public static T GetComponentInScene<T>(bool searchAlsoInactive = true) {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var obj in scene.GetRootGameObjects())
                {
                    var component = obj.GetComponentInChildren<T>(searchAlsoInactive);
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
            return default;
        }

        /// <summary>
        /// Get a component in scene, including from hidden objects
        /// </summary>
        /// <typeparam name="T">The type of the component to search</typeparam>
        /// <param name="whereClause">The filtering function</param>
        /// <returns>The first suitable component</returns>
        public static T GetComponentInScene<T>(System.Func<T, bool> whereClause) {
            foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
                foreach (var component in obj.GetComponentsInChildren<T>(true)) {
                    if (whereClause(component)) {
                        return component;
                    }
                }
            }
            return default;
        }

        public static bool ExistsInScene(Scene scene, GameObject gameObject, bool instanceCheck) {
            var effectiveScene = scene != null ? scene : SceneManager.GetActiveScene();
            foreach (var root in effectiveScene.GetRootGameObjects()) {
                if(ExistsInChildren(root.transform, gameObject, instanceCheck)) {
                    return true;
                }
            }
            return false;
        }

        public static bool ExistsInChildren(Transform currentRoot, GameObject target, bool instanceCheck = true) {
            if(SimilarGameObject(currentRoot.gameObject, target, instanceCheck)) { return true; }
            for (int i = 0; i < currentRoot.childCount; i++) {
                if(ExistsInChildren(currentRoot.GetChild(i), target, instanceCheck)) {
                    return true;
                }
            }
            return false;
        }

        private static bool SimilarGameObject(GameObject a, GameObject b, bool instanceCheck) {
            if (instanceCheck) { return a == b; }
            var componentsB = new List<Component>(b.GetComponents<Component>());
            var componentsA = a.GetComponents<Component>();
            foreach(var componentA in componentsA) {
                if (!componentsB.Remove(componentA)) { return false; }
            }
            return componentsB.Count == 0;
        }
        
        /// <summary>
        /// Whether the specified object exists in specified scene or not
        /// </summary>
        /// <param name="scene">The scene to search in, or if null, the current active <see cref="Scene"/></param>
        /// <param name="gameObject">The <see cref="GameObject"/> to search for</param>
        /// <param name="sceneGameObject">The found <see cref="GameObject"/>, useful when searching is based on a prefab</param>
        /// <returns></returns>
        private static bool ExistsInSceneEditor(Scene scene, GameObject gameObject, out GameObject sceneGameObject) {
            if(gameObject == null) {
                sceneGameObject = null;
                return false;
            }
            if(!PrefabUtility.IsPrefabAsset(gameObject)) {
                var foundGameObject = GameObject.Find(gameObject.name);
                sceneGameObject = foundGameObject ?? gameObject;
                return foundGameObject || ExistsInScene(scene, gameObject, true);
            }
            sceneGameObject = GetPrefabInstance(scene, gameObject);
            return sceneGameObject;
        }

        private static GameObject GetPrefabInstance(Scene scene, GameObject prefab) {
            var effectiveScene = scene != null ? scene : SceneManager.GetActiveScene();
            //var prefabParent = PrefabUtility.GetPrefabParent(prefab);
            foreach (var root in effectiveScene.GetRootGameObjects()) {
                var foundInstance = GetPrefabInstanceInChildren(root.transform, prefab);
                if (foundInstance != null) {
                    return foundInstance;
                }
            }
            return null;
        }

        public static GameObject GetPrefabInstanceInChildren(Transform currentRoot, Object prefabParent) {
            if (PrefabUtility.GetPrefabParent(currentRoot.gameObject) == prefabParent) { return currentRoot.gameObject; }
            for (int i = 0; i < currentRoot.childCount; i++) {
                var nextChild = GetPrefabInstanceInChildren(currentRoot.GetChild(i), prefabParent);
                if (nextChild != null) {
                    return nextChild;
                }
            }
            return null;
        }

        /// <summary>
        /// Whether the specified object exists in specified scene or not
        /// </summary>
        /// <param name="scene">The scene to search in, or if null, the current active <see cref="Scene"/></param>
        /// <param name="gameObject">The <see cref="GameObject"/> to search for</param>
        /// <param name="sceneGameObject">The found <see cref="GameObject"/>, useful when searching is based on a prefab</param>
        /// <returns></returns>
        public static bool ExistsInScene(Scene scene, GameObject gameObject, out GameObject sceneGameObject) {
            if (Application.isEditor)
            {
                return ExistsInSceneEditor(scene, gameObject, out sceneGameObject);
            }
            else
            {
                sceneGameObject = ExistsInScene(scene, gameObject, false) ? gameObject : null;
                return sceneGameObject != null;
            }
        }

        public static bool ExistsSimilarInScene(Scene scene, GameObject gameObject, out GameObject sceneGameObject)
        {
            sceneGameObject = null;
            if (!gameObject)
            {
                return false;
            }
            var effectiveScene = scene != null ? scene : SceneManager.GetActiveScene();
            foreach (var root in effectiveScene.GetRootGameObjects())
            {
                if(root.name == gameObject.name) {
                    sceneGameObject = root;
                    return true;
                }
                sceneGameObject = root.transform.Find(gameObject.name)?.gameObject;
                if (sceneGameObject)
                {
                    return true;
                }
            }
            return false;
        }

        public static void RepairTree(GameObject targetRoot, GameObject sourceRoot, bool copyExisting)
        {
            try
            {
                var transformA = sourceRoot.transform;
                var transformB = targetRoot.transform;
                Repair(targetRoot, sourceRoot, copyExisting);
                for (int i = 0; i < transformA.childCount; i++)
                {
                    var childA = transformA.GetChild(i);
                    var childB = transformB.childCount > i && transformB.GetChild(i).name == childA.name ?
                                    transformB.GetChild(i) : transformB.Find(childA.name);
                    if (!childB)
                    {
                        childB = Object.Instantiate(childA.gameObject).transform;
                        childB.name = childA.name;
                        childB.SetParent(transformB, false);
                        childB.localPosition = childA.localPosition;
                        childB.localRotation = childA.localRotation;
                        childB.localScale = childA.localScale;
                        childB.gameObject.SetActive(childA.gameObject.activeSelf);
                    }
                    RepairTree(childB.gameObject, childA.gameObject, copyExisting);
                }
            }
            catch(Exception e)
            {
                Debug.Log($"[WEAVR SETUP]: Failed to repair {targetRoot.name} from {sourceRoot.name} with error: {e.Message} -> {e.StackTrace}");
            }
        }

        public static void Repair(GameObject target, GameObject source, bool copyExisting)
        {
            foreach(var compA in source.GetComponents<Component>())
            {
                var compB = target.GetComponent(compA.GetType());
                if (!compB)
                {
                    compB = target.AddComponent(compA.GetType());
                    Copy(compA, compB);
                }
                else if (copyExisting)
                {
                    Copy(compA, compB);
                }
            }
        }

        public static void Copy(Component from, Component to)
        {
            if (!from || !to) { return; }
            Type type = from.GetType();
            if (type != to.GetType()) return; // type mis-match
            if(from is Transform tFrom && to is Transform tTo)
            {
                tTo.localPosition = tFrom.localPosition;
                tTo.localRotation = tFrom.localRotation;
                tTo.localScale = tFrom.localScale;
                return;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default/* | BindingFlags.DeclaredOnly*/;
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                try
                {
                    finfo.SetValue(to, finfo.GetValue(from));
                }
                catch { }
            }
        }

        public static void CopyComponents(GameObject from, GameObject to)
        {
            var fromComponents = from.GetComponents<Component>();
            var toComponents = to.GetComponents<Component>();
            for (int i = 0; i < fromComponents.Length; i++)
            {
                Copy(fromComponents[i], toComponents[i]);
            }
        }

    }
}