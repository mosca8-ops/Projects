using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Toggle On Scene Load")]
    public class ToggleOnSceneLoad : MonoBehaviour
    {
        [Tooltip("Whether to trigger the event when this gameobject's scene is loaded")]
        public bool includeThisScene = false;

        [System.Serializable]
        public class UnityEventString : UnityEvent<string> { }

        public UnityEventString onSceneLoaded;

        [System.NonSerialized]
        private bool m_movedToDontDestroyOnLoad = false;

        private void Awake()
        {
            if (includeThisScene)
            {
                onSceneLoaded.Invoke(gameObject.scene.name);
            }
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            SceneManager.sceneLoaded += SceneManager_SceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
        }

        private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(gameObject.scene.name == "DontDestroyOnLoad" && !m_movedToDontDestroyOnLoad)
            {
                m_movedToDontDestroyOnLoad = true;
                return;
            }
            if (scene != gameObject.scene && scene.name != "DontDestroyOnLoad")
            {
                onSceneLoaded.Invoke(scene.name);
            }
        }
    }
}
