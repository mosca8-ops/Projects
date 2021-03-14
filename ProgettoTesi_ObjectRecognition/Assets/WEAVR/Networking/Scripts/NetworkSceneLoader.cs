using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Setup/Network Scene Loader")]
    public class NetworkSceneLoader :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        [Serializable]
        public class UnityEventString : UnityEvent<string>{ }

        public UnityEventString onRemoteSceneLoaded;

        private Common.SceneLoader m_sceneLoader;
        protected Common.SceneLoader SceneLoader
        {
            get
            {
                if (m_sceneLoader == null)
                {
                    m_sceneLoader = GetComponent<Common.SceneLoader>();
                    if(m_sceneLoader == null)
                    {
                        m_sceneLoader = FindObjectOfType<Common.SceneLoader>();
                    }
                }
                return m_sceneLoader;
            }
        }

        private void OnEnable()
        {
            SceneLoader.onSceneLoad.AddListener(RemoteSceneLoad);
        }

        private void OnDisable()
        {
            SceneLoader.onSceneLoad.RemoveListener(RemoteSceneLoad);
        }

        protected void RemoteSceneLoad(string scene)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteSceneLoadRPC), scene);
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteSceneLoadRPC(string sceneName)
        {
            m_setFrameCount = Time.frameCount;
            onRemoteSceneLoaded.Invoke(sceneName);
            Debug.Log($"NetworkSceneLoader: Loading scene {sceneName}");
            SceneLoader.LoadSceneWithName(sceneName,UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
#else
        private void RemoteSceneLoadRPC(bool isLocked)
        {
            
        }
#endif
    }
}
