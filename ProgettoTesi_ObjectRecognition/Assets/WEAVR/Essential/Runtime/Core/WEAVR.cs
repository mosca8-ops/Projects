using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TXT.WEAVR.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR
{
    public interface IWeavrSingleton { }
    
    public class Weavr
    {

        #region [  CONSTANTS PARTS  ]

        public const string VERSION = "1.2.4";

        private static string s_projectPath;
        /// <summary>
        /// Editor Only project path
        /// </summary>
        public static string ProjectPath
        {
            get
            {
                if(s_projectPath == null)
                {
                    s_projectPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets"), 6);
                }
                return s_projectPath;
            }
        }

        public static string ProceduresFullPath => Application.dataPath + "/Procedures/";
        public static string ProceduresDataFullPath => Application.dataPath + "/Procedures/Data/";

        public const string InteractionLayer = "WeavrInteraction";
        public const string Overlay3DLayer = "Overlay 3D";

        public static string ConfigurationsFolder => Path.Combine(
            Application.platform == RuntimePlatform.Android 
            || Application.platform == RuntimePlatform.IPhonePlayer
            || Application.platform == RuntimePlatform.WSAPlayerARM
            || Application.platform == RuntimePlatform.WSAPlayerX86
            || Application.platform == RuntimePlatform.WSAPlayerX64? 
            Application.persistentDataPath : 
            Application.streamingAssetsPath, "Configurations");

        private static ShadersCollection s_shaders;
        public static ShadersCollection Shaders
        {
            get
            {
                if (s_shaders == null)
                {
                    s_shaders = new ShadersCollection();
                }
                return s_shaders;
            }
        }

        #endregion

        public class ShadersCollection
        {
            public readonly Shader DefaultUI = Shader.Find("WEAVR/UI/Default");

            internal ShadersCollection()
            {

            }
        }

        private static SettingsHandler s_settings;
        public static SettingsHandler Settings
        {
            get
            {
                if(s_settings == null)
                {
                    try
                    {
                        string settingsPath = Path.Combine(Application.persistentDataPath, "WEAVR.settings");
                        s_settings = new SettingsHandler(settingsPath, true);
                    }
                    catch
                    {
                        s_settings = new SettingsHandler(null, true);
                    }

                    try
                    {
                        string tempSettingsPath = Path.Combine(Application.streamingAssetsPath, "Settings.txt");
                        if (File.Exists(tempSettingsPath))
                        {
                            var tempSettings = new SettingsHandler(tempSettingsPath, false);
                            s_settings.MergeFrom(tempSettings, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        WeavrDebug.LogException(nameof(Weavr), ex);
                    }
                }
                return s_settings;
            }
        }

        /// <summary>
        /// Returns the filepath of the specified config file
        /// </summary>
        /// <param name="filename">The config filename, with extension</param>
        /// <param name="path">The full path of the file if it exists</param>
        /// <returns>True if the config file exists</returns>
        public static bool TryGetConfigFilePath(string filename, out string path)
        {
            if (Application.isMobilePlatform || Application.isConsolePlatform) { path = default; return false; }
            if (!Directory.Exists(ConfigurationsFolder))
            {
                Directory.CreateDirectory(ConfigurationsFolder);
            }
            path = Path.Combine(ConfigurationsFolder, filename);
            if (!File.Exists(path))
            {
                path = null;
            }
            return path != null;
        }

        /// <summary>
        /// Returns the filepath of the specified config file
        /// </summary>
        /// <param name="filename">The config filename, with extension</param>
        /// <param name="path">The full path of the file if it exists</param>
        /// <returns>True if the config file exists</returns>
        public static bool TryGetConfig<T>(string filename, out T config)
        {
            //if(Application.platform == RuntimePlatform.Android) 
            //{
            //    return TryGetConfigFromMobilePlatform(filename, out config);
            //}

            try
            {
                if (!Directory.Exists(ConfigurationsFolder))
                {
                    Directory.CreateDirectory(ConfigurationsFolder);
                }
                string path = Path.Combine(ConfigurationsFolder, filename);
                if (File.Exists(path))
                {
                    config = JsonUtility.FromJson<T>(File.ReadAllText(path));
                }
                else
                {
                    config = default;
                }
                return !Equals(config, default);
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(nameof(TryGetConfig), ex);
                config = default;
                return false;
            }
        }

        public static bool TryGetConfig(string filename, out string config)
        {
            if (!Directory.Exists(ConfigurationsFolder))
            {
                Directory.CreateDirectory(ConfigurationsFolder);
            }
            string path = Path.Combine(ConfigurationsFolder, filename);
            if (File.Exists(path))
            {
                config = File.ReadAllText(path);
            }
            else
            {
                config = default;
            }
            return !Equals(config, default);
        }

        private static bool TryGetConfigFromMobilePlatform<T>(string filename, out T config)
        {
            config = default;
            try
            {
                var filePath = Path.Combine(Application.persistentDataPath + "/", filename);
                if (filePath.Contains("://") || filePath.Contains(":///"))
                {
                    UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
                    www.SendWebRequest();

                    config = JsonUtility.FromJson<T>(www.downloadHandler.text);
                }
                else
                {
                    config = JsonUtility.FromJson<T>(File.ReadAllText(filePath));
                }
            }
            catch(Exception e)
            {
                WeavrDebug.LogException("Configuration", e);
            }
            return !Equals(config, default);
        }

        /// <summary>
        /// Writes to config file text
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="text">The text to write</param>
        public static void WriteToConfigFile(string filename, string text)
        {
            //if (Application.isMobilePlatform || Application.isConsolePlatform) { return; }
            try
            {
                if (!Directory.Exists(ConfigurationsFolder))
                {
                    Directory.CreateDirectory(ConfigurationsFolder);
                }
                var path = Path.Combine(ConfigurationsFolder, filename);
                File.WriteAllText(path, text);
            }
            catch(Exception ex)
            {
                WeavrDebug.LogException(nameof(WriteToConfigFile), ex);
            }
        }

        private static Dictionary<int, Dictionary<Type, Component>> s_instances = new Dictionary<int, Dictionary<Type, Component>>();
        private static Dictionary<int, Transform> s_weavrs = new Dictionary<int, Transform>();

        private static bool s_initialized;

        public static T GetInCurrentScene<T>() where T : Component, IWeavrSingleton
        {
            return GetInScene<T>(SceneManager.GetActiveScene()); ;
        }
        
        public static T GetInScene<T>(Scene scene) where T : Component, IWeavrSingleton
        {
            if(scene.IsValid() && scene.isLoaded)
            {
                if(!s_instances.TryGetValue(scene.handle, out Dictionary<Type, Component> sceneInstances))
                {
                    if (!s_initialized)
                    {
                        s_initialized = true;
                        Initialize();
                    }
                    sceneInstances = new Dictionary<Type, Component>();
                    s_instances.Add(scene.handle, sceneInstances);

                    var newInstance = FindOrCreate<T>(null, scene);
                    if (!newInstance.transform.IsChildOf(GetWEAVRInScene(scene)))
                    {
                        newInstance.transform.SetParent(GetWEAVRInScene(scene), false);
                    }
                    sceneInstances[typeof(T)] = newInstance;
                    return newInstance;
                }
                if(!sceneInstances.TryGetValue(typeof(T), out Component behaviour) || !behaviour)
                {
                    behaviour = FindOrCreate<T>(null, scene);
                    if (!behaviour.transform.IsChildOf(GetWEAVRInScene(scene)))
                    {
                        behaviour.transform.SetParent(GetWEAVRInScene(scene), false);
                    }
                    sceneInstances[typeof(T)] = behaviour;
                }
                return behaviour as T;
            }
            return null;
        }

        public static T TryGetInAnyScene<T>() where T : Component, IWeavrSingleton
        {
            var currentScene = SceneManager.GetActiveScene();
            T component = TryGetInScene<T>(currentScene);
            if (component) { return component; }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if(scene != currentScene)
                {
                    component = TryGetInScene<T>(scene);
                    if (component) { return component; }
                }
            }

            return default;
        }

        public static T TryGetInCurrentScene<T>() where T : Component, IWeavrSingleton
        {
            return TryGetInScene<T>(SceneManager.GetActiveScene()); ;
        }

        public static T TryGetInScene<T>(Scene scene) where T : Component, IWeavrSingleton
        {
            if (scene.IsValid())
            {
                if (!s_instances.TryGetValue(scene.handle, out Dictionary<Type, Component> sceneInstances))
                {
                    if (!s_initialized)
                    {
                        s_initialized = true;
                        Initialize();
                    }
                    sceneInstances = new Dictionary<Type, Component>();
                    s_instances.Add(scene.handle, sceneInstances);

                    var newInstance = Find<T>(null, scene);
                    if (newInstance)
                    {
                        sceneInstances[typeof(T)] = newInstance;
                    }
                    return newInstance;
                }
                if (!sceneInstances.TryGetValue(typeof(T), out Component behaviour) || !behaviour)
                {
                    behaviour = Find<T>(null, scene);
                    if (behaviour)
                    {
                        sceneInstances[typeof(T)] = behaviour;
                    }
                }
                return behaviour as T;
            }
            return null;
        }

        public static Transform GetWEAVRInCurrentScene()
        {
            return GetWEAVRInScene(SceneManager.GetActiveScene());
        }

        public static Transform GetWEAVRInScene(Scene scene)
        {
            if(!s_weavrs.TryGetValue(scene.handle, out Transform weavr) || !weavr)
            {
                weavr = FindOrCreate<Transform>("WEAVR", scene);
                s_weavrs[scene.handle] = weavr;
            }
            return weavr;
        }

        public static bool TryGetWEAVRInScene(Scene scene, out Transform weavr) => s_weavrs.TryGetValue(scene.handle, out weavr) && weavr;
        public static bool TryGetWEAVRInCurrentScene(out Transform weavr) => TryGetWEAVRInScene(SceneManager.GetActiveScene(), out weavr);

        public static void MergeWith(GameObject weavrInstance)
        {
            var current = GetWEAVRInScene(weavrInstance.scene);
            if(current == weavrInstance.transform) { return; }

            if (!current)
            {
                s_weavrs[weavrInstance.scene.handle] = weavrInstance.transform;
                return;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);
                if (child && !weavrInstance.transform.Find(child.name))
                {
                    child.SetParent(weavrInstance.transform);
                    i--;
                }
            }

            s_weavrs[weavrInstance.scene.handle] = weavrInstance.transform;
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(current.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(current.gameObject);
            }
        }

        private static void Initialize()
        {
            SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
        }

        private static void SceneManager_SceneUnloaded(Scene scene)
        {
            s_instances.Remove(scene.handle);
            s_weavrs.Remove(scene.handle);
        }

        private static T FindOrCreate<T>(string name, Scene scene) where T : Component
        {
            T found = null;
            foreach(var root in scene.GetRootGameObjects())
            {
                found = root.GetComponentInChildren<T>(true);
                if(found && (name == null || found.name == name))
                {
                    return found;
                }
            }

            var lastActiveScene = SceneManager.GetActiveScene();
            var isSameScene = lastActiveScene == scene;

            if (!isSameScene)
                SceneManager.SetActiveScene(scene);

            if (!typeof(Transform).IsAssignableFrom(typeof(T)))
            {
                found = new GameObject(name ?? typeof(T).Name).AddComponent<T>();
            }
            else
            {
                found = new GameObject(name ?? typeof(T).Name).GetComponent<T>();
            }

            if (!isSameScene)
                SceneManager.SetActiveScene(lastActiveScene);

            return found;
        }

        private static T Find<T>(string name, Scene scene) where T : Component
        {
            T found = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                found = root.GetComponentInChildren<T>(true);
                if (found != null && (name == null || found.name == name))
                {
                    return found;
                }
            }

            return found;
        }

        public static void UnregisterSingleton(IWeavrSingleton singleton)
        {
            if(singleton is Component c && c && s_instances != null 
                && s_instances.TryGetValue(c.gameObject.scene.handle, out Dictionary<Type, Component> values) 
                && values != null
                && values.TryGetValue(c.GetType(), out Component cValue) && cValue == c)
            {
                values.Remove(c.GetType());
            }
        }
    }

    public static class WeavrSingletonExtensions
    {
        /// <summary>
        /// Tries to get the singleton of type <typeparamref name="T"/>. If the component is not present, IT WILL BE INSTANTIATED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T GetSingleton<T>(this Component component) where T : Component, IWeavrSingleton
        {
            return GetSingleton<T>(component.gameObject);
        }

        /// <summary>
        /// Tries to get the singleton of type <typeparamref name="T"/>. If the component is not present, IT WILL BE INSTANTIATED
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T GetSingleton<T>(this GameObject gameObject) where T : Component, IWeavrSingleton
        {
            if (gameObject.scene.name == "DontDestroyOnLoad")
            {
                return Weavr.GetInCurrentScene<T>();
            }
            return Weavr.GetInScene<T>(gameObject.scene);
        }

        /// <summary>
        /// Tries to get the singleton of type <typeparamref name="T"/>. If the component is not present, it will return null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T TryGetSingleton<T>(this Component component) where T : Component, IWeavrSingleton
        {
            return TryGetSingleton<T>(component.gameObject);
        }

        /// <summary>
        /// Tries to get the singleton of type <typeparamref name="T"/>. If the component is not present, it will return null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component"></param>
        /// <returns></returns>
        public static T TryGetSingleton<T>(this GameObject gameObject) where T : Component, IWeavrSingleton
        {
            if (gameObject.scene.name == "DontDestroyOnLoad")
            {
                return Weavr.TryGetInCurrentScene<T>();
            }
            return Weavr.TryGetInScene<T>(gameObject.scene);
        }
    }
}
