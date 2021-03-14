namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using TXT.WEAVR.Utility;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public static class EditorTools
    {
        //static Dictionary<Type, PropertyValueDrawer> s_propertyDrawers;
        static readonly Dictionary<Type, string> s_modulesPath = new Dictionary<Type, string>();

        static List<UnityEngine.Object> s_tempList = new List<UnityEngine.Object>();

        //static EditorTools()
        //{
        //    //s_propertyDrawers = new Dictionary<Type, PropertyValueDrawer>();
        //    ReloadPropertyDrawerTypes();
        //}

        //[UnityEditor.Callbacks.DidReloadScripts]
        //static void OnEditorReload()
        //{
        //    ReloadPropertyDrawerTypes();
        //}

        //static void ReloadPropertyDrawerTypes()
        //{
        //    s_propertyDrawers.Clear();

        //    // Look for all the valid attribute decorators
        //    var types = GetAllAssemblyTypes()
        //                    .Where(
        //                        t => t.IsSubclassOf(typeof(PropertyValueDrawer))
        //                          && t.IsDefined(typeof(ValueDrawerAttribute), false)
        //                          && !t.IsAbstract
        //                    );

        //    // Store them
        //    foreach (var type in types)
        //    {
        //        var attr = type.GetAttribute<ValueDrawerAttribute>();
        //        var decorator = (PropertyValueDrawer)Activator.CreateInstance(type);
        //        s_propertyDrawers.Add(attr.AttributeType, decorator);
        //    }
        //}

        //public static PropertyValueDrawer GetPropertyValueDrawer(Type attributeType)
        //{
        //    PropertyValueDrawer decorator;
        //    return !s_propertyDrawers.TryGetValue(attributeType, out decorator)
        //        ? null
        //        : decorator;
        //}

        private static string _essentialFolderPath;
        public static string EssentialFolderPath {
            get {
                if (_essentialFolderPath == null)
                {
                    _essentialFolderPath = GetModulePath("TXT.WEAVR.WeavrEssential");
                }
                return _essentialFolderPath;
            }
        }

        /// <summary>
        /// Gets all specified attributes with its types
        /// </summary>
        /// <typeparam name="T">The attribute type</typeparam>
        /// <returns>The dictionary with attribute as key and type as value</returns>
        public static Dictionary<T, Type> GetAttributesWithTypes<T>() where T : Attribute
        {
            //return GetAllAssemblyTypes().Where(t => t.GetCustomAttribute<T>() != null).ToDictionary(t => t.GetCustomAttribute<T>());
            return ToDictionary(TypeCache.GetTypesWithAttribute<T>(), t => t.GetCustomAttribute<T>());
        }

        private static Dictionary<T, S> ToDictionary<T, S>(IEnumerable<S> list, Func<S, T> keyGetter)
        {
            Dictionary<T, S> dic = new Dictionary<T, S>();
            foreach(S item in list)
            {
                dic[keyGetter(item)] = item;
            }
            return dic;
        }

        /// <summary>
        /// Gets all types having the specified attribute type
        /// </summary>
        /// <typeparam name="T">The attribute type</typeparam>
        /// <returns>The list of types having the specified attribute</returns>
        public static IEnumerable<Type> GetTypesWithAttribute<T>() where T : Attribute
        {
            //return GetAllAssemblyTypes().Where(t => t.GetCustomAttribute<T>() != null).ToDictionary(t => t.GetCustomAttribute<T>());
            return TypeCache.GetTypesWithAttribute<T>();
        }

        /// <summary>
        /// Gets the relative ("Assets/...") project path of the <typeparamref name="TModule"/> in the project
        /// </summary>
        /// <typeparam name="TModule">The <see cref="WeavrModule"/></typeparam>
        /// <returns>The project path to the Weavr Module</returns>
        public static string GetModulePath<TModule>() where TModule : WeavrModule
        {
            string path = null;
            if (!s_modulesPath.TryGetValue(typeof(TModule), out path))
            {
                var module = ScriptableObject.CreateInstance<TModule>();
                module.hideFlags = HideFlags.HideAndDontSave;
                path = GetScriptDirectory(module);
                UnityEngine.Object.DestroyImmediate(module);
                s_modulesPath.Add(typeof(TModule), path);
            }
            return path;
        }

        /// <summary>
        /// Gets the relative ("Assets/...") project path of the <paramref name="moduleType"/> in the project
        /// </summary>
        /// <param name="moduleType">The type of the module</param>
        /// <returns>The project path to the Weavr Module</returns>
        public static string GetModulePath(Type moduleType)
        {
            string path = null;
            if (!s_modulesPath.TryGetValue(moduleType, out path))
            {
                var module = ScriptableObject.CreateInstance(moduleType);
                module.hideFlags = HideFlags.HideAndDontSave;
                module.name = moduleType.Name;
                path = GetScriptDirectory(module);
                UnityEngine.Object.DestroyImmediate(module);
                s_modulesPath.Add(moduleType, path);
            }
            return path;
        }

        /// <summary>
        /// Gets the relative ("Assets/...") project path of the <paramref name="moduleTypename"/> in the project
        /// </summary>
        /// <param name="moduleTypename">The typename of the module</param>
        /// <returns>The project path to the Weavr Module</returns>
        public static string GetModulePath(string moduleTypename)
        {
            if (string.IsNullOrEmpty(moduleTypename)) { return null; }
            Type moduleType = Type.GetType(moduleTypename);
            return moduleType != null ? GetModulePath(moduleType) : null;
        }

        public static float GetTextWidth(string text)
        {
            return GUI.skin.label.CalcSize(new GUIContent(text)).x;
        }

        public static float GetTextHeight(string text)
        {
            return GUI.skin.label.CalcSize(new GUIContent(text)).y;
        }

        public static float GetTextHeight(string text, float width)
        {
            return GUI.skin.label.CalcHeight(new GUIContent(text), width);
        }

        public static float GetTextWidth(string text, GUIStyle style)
        {
            return style != null ? style.CalcSize(new GUIContent(text)).x : 0;
        }

        public static float GetTextHeight(string text, GUIStyle style)
        {
            return style != null ? style.CalcSize(new GUIContent(text)).y : 0;
        }

        public static float GetTextHeight(string text, GUIStyle style, float width)
        {
            return style != null ? style.CalcHeight(new GUIContent(text), width) : 0;
        }

        public static float GetButtonHeight()
        {
            return GUI.skin.button.CalcSize(new GUIContent("Bu")).y;
        }

        public static float GetButtonHeight(GUIStyle style)
        {
            return style != null ? style.CalcSize(new GUIContent("Bu")).y : 0;
        }

        public static void DrawCircle(Rect rect, Color color)
        {
            var lastColor = Handles.color;
            Handles.color = color;
            Handles.DrawSolidDisc(rect.center, Vector3.forward, rect.width);
            Handles.color = lastColor;
        }

        public static void DrawRect(Rect rect, Color color, float? outline = null)
        {
            if (outline.HasValue)
            {
                Handles.DrawSolidRectangleWithOutline(rect, new Color(0, 0, 0, 0), color);
            }
            else
            {
                EditorGUI.DrawRect(rect, color);
            }
        }

        public static void DrawRect(Vector2 center, Color color, float thickness)
        {
            float halfThickness = thickness * 0.5f;
            Rect rect = new Rect(new Vector2(center.x - halfThickness, center.y - halfThickness),
                                 new Vector2(thickness, thickness));
            EditorGUI.DrawRect(rect, color);
        }

        /// <summary>
        /// Creates and tries to persist the object
        /// </summary>
        /// <typeparam name="T">Extends from <see cref="Object"/></typeparam>
        /// <param name="persistedCaller">The caller which should be persisted</param>
        /// <returns>The newly created object</returns>
        public static T CreatePersistent<T>(UnityEngine.Object persistedCaller) where T : UnityEngine.Object, new()
        {
            T newObject = new T();
            if (AssetDatabase.Contains(persistedCaller))
            {
                AssetDatabase.AddObjectToAsset(newObject, persistedCaller);
            }
            return newObject;
        }

        /// <summary>
        /// Creates and tries to persist a scriptable object
        /// </summary>
        /// <typeparam name="T">Extends from <see cref="ScriptableObject"/></typeparam>
        /// <param name="persistedCaller">The caller which should be persisted</param>
        /// <returns>The newly created scriptable object</returns>
        public static T CreateScriptableObject<T>(UnityEngine.Object persistedCaller) where T : ScriptableObject, new()
        {
            T newObject = new T();
            if (AssetDatabase.Contains(persistedCaller))
            {
                AssetDatabase.AddObjectToAsset(newObject, persistedCaller);
            }
            return newObject;
        }

        /// <summary>
        /// Creates a copy of the asset into the Resources folder to be accessible at runtime
        /// </summary>
        /// <param name="element">The object to copy</param>
        /// <param name="resourceRelativeFolder">The relative folder path under Resource folder</param>
        /// <param name="overwrite">[Optional] Whether to overwrite or not the object in the Resource folder</param>
        /// <returns>The copy Resources relative path or null if the copy didn't succeed</returns>
        public static string CreateResourceCopy(UnityEngine.Object element, string resourceRelativeFolder, bool overwrite = false)
        {
            string assetPath = AssetDatabase.GetAssetPath(element);
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }
            string filename = Path.GetFileName(assetPath);
            string resFolderFullPath = Path.Combine(Application.dataPath, Path.Combine("Resources", resourceRelativeFolder));
            string resRelativePath = Path.Combine(resourceRelativeFolder, filename);
            string fullPath = Path.Combine(resFolderFullPath, filename);
            if (!Directory.Exists(resFolderFullPath))
            {
                Directory.CreateDirectory(resFolderFullPath);
            }
            if (Path.GetFullPath(assetPath) == Path.GetFullPath(fullPath)
                || (File.Exists(fullPath) && !overwrite)
                || AssetDatabase.CopyAsset(assetPath, fullPath))
            {
                return resRelativePath.Replace('\\', '/');
            }
            return null;
        }

        /// <summary>
        /// Creates a copy of the asset into the Resources folder to be accessible at runtime
        /// </summary>
        /// <param name="element">The object to copy</param>
        /// <param name="overwrite">[Optional] Whether to overwrite or not the object in the Resource folder</param>
        /// <returns>The copy Resources relative path or null if the copy didn't succeed</returns>
        public static string CreateResourceCopy(UnityEngine.Object element, bool overwrite = false)
        {
            string assetPath = AssetDatabase.GetAssetPath(element);
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }
            string filename = Path.GetFileName(assetPath);
            string resFolderFullPath = Path.Combine(Application.dataPath, "Resources");
            string resRelativePath = filename;
            string fullPath = Path.Combine(resFolderFullPath, filename);
            if (!Directory.Exists(resFolderFullPath))
            {
                Directory.CreateDirectory(resFolderFullPath);
            }
            if (Path.GetFullPath(assetPath) == Path.GetFullPath(fullPath) || (File.Exists(fullPath) && !overwrite) || AssetDatabase.CopyAsset(assetPath, fullPath))
            {
                return resRelativePath.Replace('\\', '/');
            }
            return null;
        }

        /// <summary>
        /// Tries to return the directory of the script
        /// </summary>
        /// <remarks>This method will return null if the script is in a managed DLL or not in Assets folder/subfolders</remarks>
        /// <param name="script">The script in the Assets folder/subfolder</param>
        /// <returns>The path of the script or null if it cannot be retrieved</returns>
        public static string GetScriptDirectory(MonoBehaviour script)
        {
            MonoScript monoScript = MonoScript.FromMonoBehaviour(script);
            return monoScript == null ? null : GetScriptDirectory(monoScript);
        }

        /// <summary>
        /// Tries to return the directory of the script
        /// </summary>
        /// <remarks>This method will return null if the script is in a managed DLL or not in Assets folder/subfolders</remarks>
        /// <param name="script">The script in the Assets folder/subfolder</param>
        /// <returns>The path of the script or null if it cannot be retrieved</returns>
        public static string GetScriptDirectory(ScriptableObject script)
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(script);
            if (monoScript == null)
            {
                var path = Directory.GetFiles(Application.dataPath + "/WEAVR", $"{script.name}.dll", SearchOption.AllDirectories).FirstOrDefault();
                return path?.Replace(Application.dataPath, "Assets/").Replace(Path.GetFileName(path), "");
            }
            return monoScript == null ? null : GetScriptDirectory(monoScript);
        }

        private static string GetScriptDirectory(MonoScript script)
        {
            var path = AssetDatabase.GetAssetPath(script);
            return string.IsNullOrEmpty(path) ? null : path.Replace(Application.dataPath, "Assets/").Replace(Path.GetFileName(path), "");
        }

        //public static UnityEngine.Object ObjectField(Rect rect, string label, UnityEngine.Object obj, Type type, bool allowSceneObjects)
        //{
        //    var result = !string.IsNullOrEmpty(label) ?
        //                              EditorGUI.ObjectField(rect, label, obj, type, allowSceneObjects) :
        //                              EditorGUI.ObjectField(rect, obj, type, allowSceneObjects);
        //    result = PerformPrefabSwap(obj, type, result);
        //    return result;
        //}

        //public static UnityEngine.Object ObjectField(Rect rect, UnityEngine.Object obj, Type type, bool allowSceneObjects)
        //{
        //    var result = EditorGUI.ObjectField(rect, obj, type, allowSceneObjects);
        //    result = PerformPrefabSwap(obj, type, result);
        //    return result;
        //}

        //private static UnityEngine.Object PerformPrefabSwap(UnityEngine.Object obj, Type type, UnityEngine.Object result)
        //{
        //    if (result != obj && result != null)
        //    {
        //        // Do everything here
        //        GameObject gameObject = result is GameObject ? result as GameObject
        //                              : result is Component ? (result as Component).gameObject : null;
        //        if (gameObject != null && !IsPrefab(gameObject))
        //        {
        //            string prefabName = SceneTools.GetGameObjectPath(gameObject, '.') + ".prefab";
        //            string prefabPath = ProcedureEditorSettings.Current.PrefabCreationFolder + prefabName;
        //            string absPrefabPath = Application.dataPath.Replace("Assets", "") + prefabPath;
        //            if (!File.Exists(absPrefabPath))
        //            {
        //                gameObject = PrefabUtility.CreatePrefab(prefabPath, gameObject, ReplacePrefabOptions.Default);
        //            }
        //            else
        //            {
        //                gameObject = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;
        //            }
        //            Debug.Log(prefabPath);
        //            if (result is Component && gameObject != null)
        //            {
        //                result = gameObject.GetComponent(type);
        //            }
        //            else
        //            {
        //                result = gameObject;
        //            }
        //        }
        //        if (gameObject != null && gameObject.name.Contains("."))
        //        {
        //            gameObject.name = gameObject.name.Substring(gameObject.name.LastIndexOf('.') + 1);
        //        }
        //    }

        //    return result;
        //}

        public static bool IsChildOf(UnityEngine.Object obj, Transform transform)
        {
            if (obj is GameObject)
            {
                return (obj as GameObject).transform.IsChildOf(transform);
            }
            else if (obj is Component)
            {
                return (obj as Component).transform.IsChildOf(transform);
            }
            return false;
        }

        public static Transform GetTransform(UnityEngine.Object obj)
        {
            if (obj is GameObject)
            {
                return (obj as GameObject).transform;
            }
            else if (obj is Component)
            {
                return (obj as Component).transform;
            }
            return null;
        }

        public static string NicifyName(string name)
        {
            if(string.IsNullOrEmpty(name)) { return string.Empty; }
            if (name.StartsWith("m_"))
            {
                name = name.Substring(2);
            }
            else if (name.StartsWith("_"))
            {
                name = name.TrimStart('_');
            }
            return name.Substring(0, 1).ToUpperInvariant() + SplitUpperLetters(name.Substring(1));
        }

        static readonly char[] s_upperLetters = { 'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P', 'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L', 'Z', 'X', 'C', 'V', 'B', 'N', 'M' };

        private static string SplitUpperLetters(string name)
        {
            if(name.Length == 0) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            sb.Append(name[0]);
            bool wasLower = !((name[0] >= 'A' && name[0] <= 'Z') || (name[0] >= '0' && name[0] <= '9'));
            for (int i = 1; i < name.Length; i++)
            {
                bool isLower = true;
                if((name[i] >= 'A' && name[i] <= 'Z') || (name[i] >= '0' && name[i] <= '9'))
                {
                    isLower = false;
                    if (wasLower)
                    {
                        sb.Append(' ');
                    }
                }
                wasLower = isLower;
                sb.Append(name[i]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets whether an object has a prefab or not
        /// </summary>
        /// <param name="objectToTest">Object to test</param>
        /// <returns>True if it is a prefab/prefab instance, false otherwise</returns>
        public static bool IsPrefab(UnityEngine.Object objectToTest)
        {
            var prefabType = PrefabUtility.GetPrefabAssetType(objectToTest);
            return prefabType == PrefabAssetType.Model
                || prefabType == PrefabAssetType.Regular
                || prefabType == PrefabAssetType.Variant
                /*|| (prefabType == PrefabType.PrefabInstance && PrefabUtility.GetPrefabParent(objectToTest) == null)*/;
        }

        public static UnityEngine.Object TryFindInScene(UnityEngine.Object prefabObject)
        {
            var transform = GetTransform(prefabObject);
            if (transform == null) { return null; }

            s_tempList.Clear();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (prefabObject is Component)
                {
                    s_tempList.AddRange(root.GetComponentsInChildren(prefabObject.GetType(), true));
                }
                else if (prefabObject is GameObject)
                {
                    var children = root.GetComponentsInChildren<Transform>(true);
                    foreach (var child in children)
                    {
                        s_tempList.Add(child.gameObject);
                    }
                }
            }

            if (s_tempList.Count == 0)
            {
                return null;
            }

            var path = SceneTools.GetGameObjectPath(prefabObject);
            foreach (var obj in s_tempList)
            {
                var objPath = SceneTools.GetGameObjectPath(obj);
                if (objPath.Contains(path))
                {
                    return obj;
                }
            }

            //Debug.Log($"TryFindInScene: Unable to find object with partial path: {path}");

            foreach (var obj in s_tempList)
            {
                if (obj.name == prefabObject.name)
                {
                    Debug.Log($"TryFindInScene: Found by name instead");
                    return obj;
                }
            }
            //Debug.Log($"TryFindInScene: Failed to find: {prefabObject.name} with Path: {path}");
            return null;
        }

        /// <summary>
        /// Tries to resolve the references between prefab and scene objects
        /// </summary>
        /// <remarks>The <paramref name="sceneObject"/> should be an instance of <paramref name="prefab"/></remarks>
        /// <param name="prefab"></param>
        /// <param name="sceneObject"></param>
        public static void ResolveReferences(GameObject prefab, GameObject sceneObject)
        {
            if (prefab == null || sceneObject == null) { return; }
            Dictionary<Behaviour, Behaviour> correspondecies = new Dictionary<Behaviour, Behaviour>();
            var prefabBehaviours = prefab.GetComponentsInChildren<Behaviour>(true);
            var sceneBehaviours = sceneObject.GetComponentsInChildren<Behaviour>(true);
            int i = 0;
            foreach (var behaviour in sceneBehaviours)
            {
                if (i >= prefabBehaviours.Length)
                {
                    Debug.Log($"ReferenceResolver: Wrong number of references between scene object [{sceneBehaviours.Length}] and prefab [{prefabBehaviours.Length}]");
                    break;
                }
                if (behaviour == null || prefabBehaviours[i] == null)
                {
                    i++;
                    //Debug.Log($"MonoBehaviour {i}: Prefab: {prefabBehaviours[i]} | GameObject: {behaviour}");
                    continue;
                }
                if (prefabBehaviours[i].GetType() != behaviour.GetType())
                {
                    //Debug.Log($"MonoBehaviour {i}: {prefabBehaviours[i].GetType().Name} != {behaviour.GetType().Name}");
                    i++;
                    continue;
                }
                correspondecies[prefabBehaviours[i++]] = behaviour;
            }

            foreach (var pair in correspondecies)
            {
                var serPrefab = new SerializedObject(pair.Key);
                var serGO = new SerializedObject(pair.Value);

                var property = serPrefab.GetIterator();
                while (property.NextVisible(property.propertyType == SerializedPropertyType.Generic))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null) { continue; }
                    if (!IsPrefab(property.objectReferenceValue) || IsChildOf(property.objectReferenceValue, prefab.transform)) { continue; }
                    //Debug.Log($"{SceneTools.GetGameObjectPath(serPrefab.targetObject)} -> Found Reference To fix: [{serPrefab.targetObject.GetType().Name}].{property.propertyPath} = {property.objectReferenceValue.name}");
                    var inScene = TryFindInScene(property.objectReferenceValue);
                    if (inScene)
                    {
                        serGO.FindProperty(property.propertyPath).objectReferenceValue = inScene;
                    }
                }
                serGO.ApplyModifiedProperties();
            }
        }

        #region Reflection

        private static List<Type> s_cachedAssemblyTypes;
        private static List<Type> s_cachedNonUnityAssemblyTypes;
        private static HashSet<Assembly> s_cachedAssemblies;
        private static Dictionary<Type, List<Type>> s_cachedSubclasses = new Dictionary<Type, List<Type>>();
        private static Dictionary<Type, List<Type>> s_cachedNonUnitySubclasses = new Dictionary<Type, List<Type>>();
        private static Dictionary<Type, List<Type>> s_cachedLeafClasses = new Dictionary<Type, List<Type>>();
        private static Dictionary<Type, int> s_typesDepths = new Dictionary<Type, int>();

        private static HashSet<Assembly> NonUnityAssemblies
        {
            get
            {
                if(s_cachedAssemblies == null)
                {
                    s_cachedAssemblies = new HashSet<Assembly>(AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.FullName.ToLower().Contains("unity")));
                }
                return s_cachedAssemblies;
            }
        }

        /// <summary>
        /// Get all available and valid types present in all loaded assemblies
        /// </summary>
        /// <remarks>It involves a caching mechanism, thus initial loading will take much longer than subsequent ones</remarks>
        /// <returns>The list of all available types</returns>
        public static IEnumerable<Type> GetAllAssemblyTypes()
        {
            if (s_cachedAssemblyTypes == null)
            {
                s_cachedAssemblyTypes = new List<Type>(AppDomain.CurrentDomain.GetAssemblies().SelectMany(t =>
                {
                    try
                    {
                        return t.GetTypes();
                    }
                    catch
                    {
                        Debug.Log($"Exception when loading assemblies of {t.FullName}");
                        return new Type[0];
                    }
                }));
            }
            return s_cachedAssemblyTypes;
        }

        /// <summary>
        /// Get all available and valid types present in all loaded assemblies
        /// </summary>
        /// <remarks>It involves a caching mechanism, thus initial loading will take much longer than subsequent ones</remarks>
        /// <returns>The list of all available types</returns>
        public static IEnumerable<Type> GetAllNonUnityAssemblyTypes()
        {
            if (s_cachedNonUnityAssemblyTypes == null)
            {
                s_cachedNonUnityAssemblyTypes = new List<Type>(NonUnityAssemblies.SelectMany(t => t.GetTypes()));
            }
            return s_cachedNonUnityAssemblyTypes;
        }

        /// <summary>
        /// Gets all subclasses (if <paramref name="baseClass"/> is a class type) 
        /// or all implementations (if <paramref name="baseClass"/> is interface type) 
        /// of specified <paramref name="baseClass"/>
        /// </summary>
        /// <param name="baseClass">The base class to get subclasses from</param>
        /// <returns>The list of all subclasses or implementations</returns>
        public static IReadOnlyList<Type> GetAllSubclassesOf(this Type baseClass)
        {
            return baseClass.IsValueType ? new List<Type>()
                        : baseClass.IsInterface ? GetAllImplementations(baseClass)
                                                : GetAllSubclasses(baseClass);
        }

        /// <summary>
        /// Gets all implementations of specified <paramref name="interfaceType"/>
        /// </summary>
        /// <typeparam name="TInterface">The interface type to get implementations from</typeparam>
        /// <returns>The list of all implementations</returns>
        public static IReadOnlyList<Type> GetAllImplementations<TInterface>()
        {
            return GetAllImplementations(typeof(TInterface));
        }

        /// <summary>
        /// Gets all implementations of specified <paramref name="interfaceType"/>
        /// </summary>
        /// <param name="interfaceType">The interface type to get implementations from</param>
        /// <returns>The list of all implementations</returns>
        public static IReadOnlyList<Type> GetAllImplementations(this Type interfaceType)
        {
            List<Type> implementations = null;
            if (!s_cachedSubclasses.TryGetValue(interfaceType, out implementations))
            {
                implementations = new List<Type>();
                foreach (var type in GetAllAssemblyTypes())
                {
                    if (!implementations.Contains(type) && type.GetInterfaces().Contains(interfaceType))
                    {
                        implementations.Add(type);
                    }
                }
                s_cachedSubclasses[interfaceType] = implementations;
            }
            return implementations;
        }

        /// <summary>
        /// Gets all subclasses of specified <paramref name="baseClass"/>
        /// </summary>
        /// <param name="baseClass">The base class to get subclasses from</param>
        /// <returns>The list of all subclasses</returns>
        public static IReadOnlyList<Type> GetAllSubclasses(this Type baseClass)
        {
            //return TypeCache.GetTypesDerivedFrom(baseClass).ToList();
            List<Type> subclasses = null;
            if (!s_cachedSubclasses.TryGetValue(baseClass, out subclasses))
            {
                subclasses = new List<Type>();
                foreach (var type in GetAllAssemblyTypes())
                {
                    if (type.IsSubclassOf(baseClass) && !subclasses.Contains(type))
                    {
                        subclasses.Add(type);
                    }
                }
                s_cachedSubclasses[baseClass] = subclasses;
            }
            return subclasses;
        }

        /// <summary>
        /// Gets all leaf classes of specified <paramref name="baseClass"/>
        /// </summary>
        /// <param name="baseClass">The base class to get leaf classes from</param>
        /// <returns>The list of all leaf classes</returns>
        public static IReadOnlyList<Type> GetAllLeafClasses(this Type baseClass)
        {
            List<Type> LeafClasses = null;
            if (!s_cachedLeafClasses.TryGetValue(baseClass, out LeafClasses))
            {
                LeafClasses = new List<Type>();
                foreach (var type in baseClass.GetAllSubclasses())
                {
                    if (type.GetAllSubclasses().Count == 0)
                    {
                        LeafClasses.Add(type);
                    }
                    else
                    {
                        s_cachedLeafClasses[type] = LeafClasses;
                    }
                }
                s_cachedLeafClasses[baseClass] = LeafClasses;
            }
            return LeafClasses;
        }

        /// <summary>
        /// Gets the depth of the type in its inheritance tree
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetDepth(this Type type)
        {
            if(type.IsValueType || type == typeof(object)) { return 0; }
            if(s_typesDepths.TryGetValue(type, out int depth))
            {
                return depth;
            }
            depth = GetDepth(type.BaseType) + 1;
            s_typesDepths[type] = depth;
            return depth;
        }

        // Quick extension method to get the first attribute of type T on a given Type
        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            //Assert.IsTrue(type.IsDefined(typeof(T), false), "Attribute not found");
            var attributes = type.GetCustomAttributes(typeof(T), false);
            return attributes.Length > 0 ? attributes[0] as T : null;
        }

        public static T GetAttribute<T>(this MemberInfo member) where T : Attribute
        {
            if(member == null) { return null; }
            var attributes = member.GetCustomAttributes(typeof(T), false);
            return attributes.Length > 0 ? attributes[0] as T : null;
        }

        // Returns all attributes set on a specific member
        // Note: doesn't include inherited attributes, only explicit ones
        public static Attribute[] GetMemberAttributes<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            Expression body = expr;

            if (body is LambdaExpression)
                body = ((LambdaExpression)body).Body;

            switch (body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    var fi = (FieldInfo)((MemberExpression)body).Member;
                    return fi.GetCustomAttributes(false).Cast<Attribute>().ToArray();
                default:
                    throw new InvalidOperationException();
            }
        }

        // Returns a string path from an expression - mostly used to retrieve serialized properties
        // without hardcoding the field path. Safer, and allows for proper refactoring.
        public static string GetFieldPath<TType, TValue>(Expression<Func<TType, TValue>> expr)
        {
            MemberExpression me;
            switch (expr.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                    me = expr.Body as MemberExpression;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            var members = new List<string>();
            while (me != null)
            {
                members.Add(me.Member.Name);
                me = me.Expression as MemberExpression;
            }

            var sb = new StringBuilder();
            for (int i = members.Count - 1; i >= 0; i--)
            {
                sb.Append(members[i]);
                if (i > 0) sb.Append('.');
            }

            return sb.ToString();
        }

        public static object GetParentObject(string path, object obj)
        {
            var fields = path.Split('.');

            if (fields.Length == 1)
                return obj;

            var info = obj.GetType().GetField(fields[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            obj = info.GetValue(obj);

            return GetParentObject(string.Join(".", fields, 1, fields.Length - 1), obj);
        }

        #endregion
    }

}
