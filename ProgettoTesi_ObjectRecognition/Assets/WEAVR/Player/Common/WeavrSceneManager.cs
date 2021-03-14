using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Player
{
    public class WeavrSceneManager : SceneManager
    {
        private const string AnonymousSceneName = "Anonymous";

        #region [  STATIC OVERRIDES  ]

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <param name="parameters">Struct that collects the various parameters into a single place except for the name and index.</param>
        /// <returns></returns>
        public static async Task LoadSceneAsync(string sceneName, LoadSceneParameters parameters, Action<float> progressUpdate = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, parameters);
            while (!operation.isDone)
            {
                await Task.Yield();
                progressUpdate?.Invoke(operation.progress);
            }
        }

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <returns></returns>
        public new static async Task LoadSceneAsync(string sceneName) => await LoadSceneAsync(sceneName, LoadSceneMode.Single, null);

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <param name="mode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        /// <returns></returns>
        public new static async Task LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
            => await LoadSceneAsync(sceneName, mode, null);

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to load.</param>
        /// <param name="mode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        /// <param name="progressUpdate">[Optional] The progress update callback.</param>
        /// <returns></returns>
        public static async Task LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, Action<float> progressUpdate = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneName, mode);
            while (operation?.isDone == false)
            {
                await Task.Yield();
                progressUpdate?.Invoke(operation.progress);
            }
        }

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneBuildIndex">Index of the Scene in the Build Settings to load.</param>
        /// <param name="parameters">Struct that collects the various parameters into a single place except for the name and index.</param>
        /// <returns></returns>
        public new static async Task LoadSceneAsync(int sceneBuildIndex, LoadSceneParameters parameters)
         => await LoadSceneAsync(sceneBuildIndex, parameters, null);

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneBuildIndex">Index of the Scene in the Build Settings to load.</param>
        /// <param name="parameters">Struct that collects the various parameters into a single place except for the name and index.</param>
        /// <param name="progressUpdate">[Optional] The progress update callback.</param>
        /// <returns></returns>
        public static async Task LoadSceneAsync(int sceneBuildIndex, LoadSceneParameters parameters, Action<float> progressUpdate = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneBuildIndex, parameters);
            while (!operation.isDone)
            {
                await Task.Yield();
                progressUpdate?.Invoke(operation.progress);
            }
        }

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneBuildIndex">Index of the Scene in the Build Settings to load.</param>
        /// <param name="mode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        /// <returns></returns>
        public new static async Task LoadSceneAsync(int sceneBuildIndex, LoadSceneMode mode = LoadSceneMode.Single)
         => await LoadSceneAsync(sceneBuildIndex, mode, null);

        /// <summary>
        /// Loads the Scene asynchronously in the background.
        /// </summary>
        /// <param name="sceneBuildIndex">Index of the Scene in the Build Settings to load.</param>
        /// <param name="mode">If LoadSceneMode.Single then all current Scenes will be unloaded before loading.</param>
        /// <param name="progressUpdate">[Optional] The progress update callback.</param>
        /// <returns></returns>
        public static async Task LoadSceneAsync(int sceneBuildIndex, LoadSceneMode mode = LoadSceneMode.Single, Action<float> progressUpdate = null)
        {
            var operation = SceneManager.LoadSceneAsync(sceneBuildIndex, mode);
            while (!operation.isDone)
            {
                await Task.Yield();
                progressUpdate?.Invoke(operation.progress);
            }
        }

        /// <summary>
        /// Destroys all <see cref="UnityEngine.GameObject"/>s associated with the given <see cref="Scene"/> 
        /// and removes the Scene from <see cref="SceneManager"/>
        /// </summary>
        /// <param name="sceneBuildIndex">Index of the Scene in BuildSettings.</param>
        /// <returns></returns>
        public new static async Task UnloadSceneAsync(int sceneBuildIndex)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneBuildIndex);
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Destroys all <see cref="UnityEngine.GameObject"/>s associated with the given <see cref="Scene"/> 
        /// and removes the Scene from <see cref="SceneManager"/>
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to unload.</param>
        /// <returns></returns>
        public new static async Task UnloadSceneAsync(string sceneName)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName);
            while (operation?.isDone == false)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Destroys all <see cref="UnityEngine.GameObject"/>s associated with the given <see cref="Scene"/> 
        /// and removes the Scene from <see cref="SceneManager"/>
        /// </summary>
        /// <param name="scene">Scene to unload.</param>
        /// <returns></returns>
        public new static async Task UnloadSceneAsync(Scene scene) { if (scene.IsValid() && scene.isLoaded) { await UnloadSceneAsync(scene.name); } }

        /// <summary>
        /// Destroys all <see cref="UnityEngine.GameObject"/>s associated with the given <see cref="Scene"/> 
        /// and removes the Scene from <see cref="SceneManager"/>
        /// </summary>
        /// <param name="sceneBuildIndex">Index of the Scene in BuildSettings.</param>
        /// <param name="options">Scene unloading options.</param>
        /// <returns></returns>
        public new static async Task UnloadSceneAsync(int sceneBuildIndex, UnloadSceneOptions options)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneBuildIndex, options);
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Destroys all <see cref="UnityEngine.GameObject"/>s associated with the given <see cref="Scene"/> 
        /// and removes the Scene from <see cref="SceneManager"/>
        /// </summary>
        /// <param name="sceneName">Name or path of the Scene to unload.</param>
        /// <param name="options">Scene unloading options.</param>
        /// <returns></returns>
        public new static async Task UnloadSceneAsync(string sceneName, UnloadSceneOptions options)
        {
            var operation = SceneManager.UnloadSceneAsync(sceneName, options);
            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Destroys all <see cref="UnityEngine.GameObject"/>s associated with the given <see cref="Scene"/> 
        /// and removes the Scene from <see cref="SceneManager"/>
        /// </summary>
        /// <param name="scene">Scene to unload.</param>
        /// <param name="options">Scene unloading options.</param>
        /// <returns></returns>
        public new static async Task UnloadSceneAsync(Scene scene, UnloadSceneOptions options) => await UnloadSceneAsync(scene.name, options);

        #endregion

        public static string LoadingSceneName { get; set; }
        public static Scene LoadingScene { get; private set; }
        public static ISceneLoadingListener SceneLoadingListener { get; set; }

        public static async Task RestartCurrentScenesAsync(Action<float> progressUpdater = null)
        {
            var activeSceneName = GetActiveScene().name;
            HashSet<string> additionalSceneNames = new HashSet<string>();
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = GetSceneAt(i);
                if (scene.name != activeSceneName && scene.name.ToLower() != "DontDestroyOnLoad".ToLower())
                {
                    additionalSceneNames.Add(scene.name);
                }
            }

            await UnloadAllScenesAsync();

            //await Task.Delay(100);

            await LoadNextScene(activeSceneName, false, progressUpdater, additionalSceneNames.ToArray());
        }

        public static async Task<Scene> UnloadAllScenesAsync()
        {
            var activeScene = GetActiveScene();
            List<Task> unloadTasks = new List<Task>();
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = GetSceneAt(i);
                if (scene != activeScene && scene.name.ToLower() != "DontDestroyOnLoad".ToLower())
                {
                    unloadTasks.Add(UnloadSceneAsync(scene));
                }
            }

            // TODO: Add loader when unloading scenes
            await Task.WhenAll(unloadTasks);

            Scene emptyScene = await CreateAnonymousScene();

            await UnloadSceneAsync(activeScene);

            return emptyScene;
        }

        private static async Task<Scene> CreateAnonymousScene()
        {
            var emptyScene = GetSceneByName(AnonymousSceneName);
            if(emptyScene.IsValid() && emptyScene.isLoaded) { return emptyScene; }

            emptyScene = CreateScene(AnonymousSceneName, new CreateSceneParameters() { localPhysicsMode = LocalPhysicsMode.None });
            SetActiveScene(emptyScene);
            var resourceOP = Resources.LoadAsync("Prefabs/AnonymousScene");
            while(resourceOP?.isDone == false)
            {
                await Task.Yield();
            }
            var anonymousScenePrefab = resourceOP?.asset;
            if (anonymousScenePrefab)
            {
                UnityEngine.Object.Instantiate(anonymousScenePrefab);
            }
            else
            {
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.gameObject.tag = "Main Camera";
                camera.clearFlags = CameraClearFlags.Color;
                camera.backgroundColor = Color.black;
            }
            return emptyScene;
        }

        public static async Task<Scene> LoadNextScene(string sceneName, bool restartIfLoaded, Action<float> progressUpdate = null, params string[] additionalScenes)
        {
            var activeScene = GetActiveScene();
            if (activeScene.name == sceneName && !restartIfLoaded)
            {
                return activeScene;
            }

            // Load the loading Scene
            if (!LoadingScene.isLoaded)
            {
                if (LoadingScene.IsValid())
                {
                    await LoadSceneAsync(LoadingScene.name, LoadSceneMode.Additive);
                }
                else if (!string.IsNullOrEmpty(LoadingSceneName))
                {
                    await LoadSceneAsync(LoadingSceneName, LoadSceneMode.Additive);
                    LoadingScene = GetSceneByName(LoadingSceneName);
                }
            }

            var loadingScene = LoadingScene;

            if (!loadingScene.IsValid())
            {
                loadingScene = activeScene.name == AnonymousSceneName ? activeScene : await CreateAnonymousScene();
            }

            var sceneLoadingListener = SceneLoadingListener;

            if (loadingScene.isLoaded)
            {
                SetActiveScene(loadingScene);
                if (sceneLoadingListener == null) {
                    foreach (var root in loadingScene.GetRootGameObjects())
                    {
                        sceneLoadingListener = root.GetComponentInChildren<ISceneLoadingListener>();
                        if(sceneLoadingListener != null) { break; }
                    }
                }
            }

            // First unload all
            List<Task> tasks = new List<Task>(Mathf.Max(sceneCount, additionalScenes.Length));
            List<Scene> currentlyLoadedScenes = new List<Scene>();
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = GetSceneAt(i);
                if(scene.name != loadingScene.name && scene != loadingScene)
                {
                    currentlyLoadedScenes.Add(scene);
                    if (restartIfLoaded || (sceneName != scene.name && additionalScenes.Any(s => s == scene.name)))
                    {
                        tasks.Add(UnloadSceneAsync(scene));
                    }
                }
            }

            await Task.WhenAll(tasks);

            if (sceneLoadingListener != null)
            {
                await sceneLoadingListener.OnScenesUnload();
            }

            // Then Load All in parallel
            tasks.Clear();

            Action<float> progressCallback = default;
            if(progressUpdate != null && sceneLoadingListener != null)
            {
                progressCallback = v =>
                {
                    progressUpdate(v);
                    sceneLoadingListener.ProgressUpdate(v);
                };
            }
            else if(progressUpdate != null)
            {
                progressCallback = progressUpdate;
            }
            else if(sceneLoadingListener != null)
            {
                progressCallback = sceneLoadingListener.ProgressUpdate;
            }

            ProgressAggregator aggregator = null;
            if(progressCallback != null)
            {
                aggregator = new ProgressAggregator(progressCallback, additionalScenes.Length + 1);
            }

            int count = 1;
            foreach(var s in additionalScenes)
            {
                if (restartIfLoaded || !currentlyLoadedScenes.Any(scene => scene.name != s))
                {
                    int sceneIndex = count; // This is to avoid side effects
                    tasks.Add(LoadSceneAsync(s, LoadSceneMode.Additive, progressCallback == null ? null : additionalScenes.Length <= 0 ? progressCallback : v => aggregator.UpdateProgress(sceneIndex, v)));
                    count++;
                }
            }

            if (restartIfLoaded || GetActiveScene().name != sceneName)
            {
                tasks.Add(LoadSceneAsync(sceneName, LoadSceneMode.Additive, progressCallback == null ? null : additionalScenes.Length <= 0 ? progressCallback : v => aggregator.UpdateProgress(0, v)));
            }
            
            await Task.WhenAll(tasks);

            if (sceneLoadingListener != null)
            {
                await sceneLoadingListener.OnScenesLoaded();
            }

            if (loadingScene.IsValid() && loadingScene.isLoaded)
            {
                await UnloadSceneAsync(loadingScene);
            }

            // Unload remaining currently loaded scenes
            tasks.Clear();
            foreach(var s in currentlyLoadedScenes)
            {
                if (s.IsValid() && s.isLoaded)
                {
                    tasks.Add(UnloadSceneAsync(s));
                }
            }

            await Task.WhenAll(tasks);

            // Disable cameras in additive scenes
            foreach(var additionalScene in additionalScenes)
            {
                EnableCamerasInScene(additionalScene, false);
            }

            var loadedScene = GetSceneByName(sceneName);
            if (!loadedScene.IsValid())
            {
                loadedScene = GetSceneByPath(sceneName);
            }
            if (!loadedScene.IsValid())
            {
                for (int i = 0; i < sceneCount; i++)
                {
                    var s = GetSceneAt(i);
                    if(s.name == sceneName || s.path == sceneName)
                    {
                        loadedScene = GetSceneAt(i);
                        break;
                    }
                }
            }
            if (!loadedScene.IsValid() && (GetActiveScene().name == sceneName || GetActiveScene().path == sceneName))
            {
                loadedScene = GetActiveScene();
            }
            else
            {
                SetActiveScene(loadedScene);
            }

            return loadedScene;
        }

        private static void EnableCamerasInScene(string sceneName, bool enable)
        {
            var scene = GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                scene = GetSceneByPath(sceneName);
            }
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var cameras = root.GetComponentsInChildren<Camera>();
                    foreach (var cam in cameras)
                    {
                        var audioListener = cam.GetComponent<AudioListener>();
                        if (audioListener)
                        {
                            audioListener.enabled = enable;
                        }
                        cam.enabled = enable;
                    }
                }
            }
        }

        private class ProgressAggregator
        {
            public float[] progresses;
            public Action<float> callback;

            public ProgressAggregator(Action<float> progressUpdate, int tasksCount)
            {
                callback = progressUpdate;
                progresses = new float[tasksCount];
            }

            public void UpdateProgress(int index, float progress)
            {
                progresses[index] = UnityEngine.Mathf.Clamp01(progress);
                AverageAndNotify();
            }

            private void AverageAndNotify()
            {
                float total = 0;
                for (int i = 0; i < progresses.Length; i++)
                {
                    total += progresses[i];
                }

                callback(total / progresses.Length);
            }
        }
    }
}
