namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;

    [AddComponentMenu("WEAVR/Utilities/Scene Loader")]
    public class SceneLoader : MonoBehaviour, IWeavrSingleton
    {
        [Serializable]
        public class UnityEventString : UnityEvent<string> { }

        public float loadDelay = 0.1f;
        [HideInInspector]
        [DoNotExpose]
        public string sceneToLoad;

        public UnityEventString onSceneLoad;
        public UnityEventString onSceneLoadAdditive;

        public string LoadScene {
            get {
                return sceneToLoad;
            }
            set {
                LoadSceneWithName(value, LoadSceneMode.Single);
            }
        }

        public string LoadSceneAdditive
        {
            get => sceneToLoad;
            set => LoadSceneWithName(value, LoadSceneMode.Additive);
        }

        public void LoadDefaultScene()
        {
            LoadSceneWithName(sceneToLoad, LoadSceneMode.Single);
        }

        public void LoadDefaultSceneAdditive()
        {
            LoadSceneWithName(sceneToLoad, LoadSceneMode.Additive);
        }

        public void LoadSceneWithName(string scene, LoadSceneMode loadSceneMode)
        {
            if (!string.IsNullOrEmpty(scene))
            {
                StopAllCoroutines();
                StartCoroutine(LoadSceneDelayed(loadDelay, scene, loadSceneMode));
            }
        }

        private IEnumerator LoadSceneDelayed(float delay, string scene, LoadSceneMode loadSceneMode)
        {
            yield return new WaitForSeconds(delay);
            onSceneLoad.Invoke(scene);
            if (loadSceneMode == LoadSceneMode.Additive)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    if (SceneManager.GetSceneAt(i).name == scene || SceneManager.GetSceneAt(i).path == scene)
                    {
                        yield break;
                    }
                }
                onSceneLoadAdditive.Invoke(scene);
            }
            SceneManager.LoadSceneAsync(scene, loadSceneMode);
        }

        public void QuitApplication()
        {
            Application.Quit();
        }

        public void ReloadCurrentScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            onSceneLoad.Invoke(sceneName);
            LoadSceneWithName(sceneName, LoadSceneMode.Single);
        }
    }
}