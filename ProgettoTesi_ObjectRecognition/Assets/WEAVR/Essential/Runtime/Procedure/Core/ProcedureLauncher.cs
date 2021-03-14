using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{

    [AddComponentMenu("WEAVR/Procedures/Procedure Launcher")]
    public class ProcedureLauncher : MonoBehaviour
    {
        public static Action<string> s_LoadSceneAsync;
        public static Action<Action<Scene, LoadSceneMode>> s_RegisterSceneOpenEventHandler;
        public static Action<Action<Scene, LoadSceneMode>> s_UnregisterSceneOpenEventHandler;

        [SerializeField]
        private Procedure m_procedure;
        [SerializeField]
        [ArrayElement(nameof(m_modes))]
        private ExecutionMode m_mode;
        [SerializeField]
        [HideInInspector]
        private ExecutionMode[] m_modes;

        [Space]
        [SerializeField]
        [Tooltip("Reload the scene if needed")]
        private bool m_reloadScene = false;
        [SerializeField]
        [Tooltip("Whether to destroy this gameobject once the procedure has been launched")]
        private bool m_destroyOnLaunch = true;
        [SerializeField]
        [Tooltip("Whether to load the scene beforehand or not")]
        private bool m_preloadScene;

        private bool m_canLaunchPreloadedScene;

        [Space]
        [SerializeField]
        [ShowAsReadOnly]
        private bool m_sceneIsReady;

        private AsyncOperation m_loadSceneOperation;
        private Language m_languageToSet;

        public Procedure CurrentProcedure
        {
            get { return m_procedure; }
            set { m_procedure = value; }
        }

        public ExecutionMode Mode
        {
            get { return m_mode; }
            set { m_mode = value; }
        }

        private void OnValidate()
        {
            m_modes = m_procedure ? m_procedure.ExecutionModes.ToArray() : null;
        }

        private void Start()
        {
            if (m_preloadScene)
            {
                PreloadScene();
            }
        }

        private void PreloadScene()
        {
            StartCoroutine(PreloadSceneCoroutine());
        }

        private IEnumerator PreloadSceneCoroutine()
        {
            m_loadSceneOperation = SceneManager.LoadSceneAsync(m_procedure.SceneName, LoadSceneMode.Single);
            m_loadSceneOperation.allowSceneActivation = false;
            m_loadSceneOperation.completed -= LoadSceneOperation_Completed;
            m_loadSceneOperation.completed += LoadSceneOperation_Completed;
            while (!m_loadSceneOperation.isDone)
            {
                yield return null;
            }
        }

        public void Launch()
        {
            StartCoroutine(LoadProcedureSceneCoroutine());
        }

        private IEnumerator LoadProcedureSceneCoroutine()
        {
            var scene = SceneManager.GetSceneByPath(m_procedure.ScenePath);

            if ((!scene.isLoaded || m_reloadScene) && m_loadSceneOperation == null)
            {
                DontDestroyOnLoad(this);

                if (!Application.isPlaying)
                {
                    s_UnregisterSceneOpenEventHandler?.Invoke(SceneManager_SceneLoaded);
                    s_RegisterSceneOpenEventHandler?.Invoke(SceneManager_SceneLoaded);
                    s_LoadSceneAsync?.Invoke(m_procedure.SceneName);
                }
                else if (m_reloadScene || m_procedure.ScenePath != SceneManager.GetActiveScene().path)
                {
                    SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
                    SceneManager.sceneLoaded += SceneManager_SceneLoaded;
                    var load = SceneManager.LoadSceneAsync(m_procedure.SceneName);
                    while (!load.isDone)
                    {
                        yield return null;
                    }
                }
                else
                {
                    StartProcedure(SceneManager.GetActiveScene());
                }
            }
            else if (m_loadSceneOperation != null && m_loadSceneOperation.isDone)
            {
                var currentScene = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.GetSceneByPath(m_procedure.ScenePath));
                yield return SceneManager.UnloadSceneAsync(currentScene);
                m_canLaunchPreloadedScene = false;
                m_loadSceneOperation.completed -= LoadSceneOperation_Completed;
                m_loadSceneOperation = null;
                m_sceneIsReady = true;
            }
            else
            {
                m_canLaunchPreloadedScene = true;
            }
        }

        private void LoadSceneOperation_Completed(AsyncOperation operation)
        {
            m_sceneIsReady = true;
            if (m_canLaunchPreloadedScene)
            {
                var currentScene = SceneManager.GetActiveScene();
                operation.allowSceneActivation = true;
                SceneManager.SetActiveScene(SceneManager.GetSceneByPath(m_procedure.ScenePath));
                SceneManager.UnloadSceneAsync(currentScene);
                m_canLaunchPreloadedScene = false;
                m_loadSceneOperation.completed -= LoadSceneOperation_Completed;
                m_loadSceneOperation = null;
            }
        }

        private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(scene.path == m_procedure.ScenePath)
            {
                StartProcedure(scene);
            }
        }

        private void StartProcedure(Scene scene)
        {
            var runner = Weavr.GetInScene<ProcedureRunner>(scene);
            runner.StartProcedure(m_procedure, m_mode);
            if (m_languageToSet)
            {
                LocalizationManager.Current.CurrentLanguage = m_languageToSet;
                m_languageToSet = null;
            }
            //runner.StartWhenReady = false;
            //runner.CurrentProcedure = m_procedure;
            //runner.ExecutionMode = m_mode;
            //runner.StartWhenReady = true;
            if (!Application.isPlaying)
            {
                s_UnregisterSceneOpenEventHandler?.Invoke(SceneManager_SceneLoaded);
            }
            else
            {
                SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            }
            if (m_destroyOnLaunch)
            {
                Destroy(gameObject);
            }
        }

        public static void LaunchProcedure(Procedure procedure, ExecutionMode mode, bool reloadScene)
        {
            var launcher = new GameObject("Launcher").AddComponent<ProcedureLauncher>();
            launcher.m_procedure = procedure;
            launcher.m_mode = mode;
            launcher.m_destroyOnLaunch = true;
            launcher.m_reloadScene = reloadScene;
            launcher.PreloadScene();
            launcher.Launch();
        }

        public static void LaunchProcedure(Procedure procedure, ExecutionMode mode, Language language, bool reloadScene)
        {
            var launcher = new GameObject("Launcher").AddComponent<ProcedureLauncher>();
            launcher.m_procedure = procedure;
            launcher.m_mode = mode;
            launcher.m_destroyOnLaunch = true;
            launcher.m_reloadScene = reloadScene;
            launcher.m_languageToSet = language;
            launcher.PreloadScene();
            launcher.Launch();
        }

        public static void LaunchProcedure(Procedure procedure, ExecutionMode mode) => LaunchProcedure(procedure, mode, true);
    }
}
