using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(PhotonView))]
    [AddComponentMenu("WEAVR/Network/Network Avatar")]
    public class NetworkAvatar : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        private GameObject m_scenePlayer;
        [SerializeField]
        [Draggable]
        private GameObject m_avatarPrefab;
        [SerializeField]
        [Draggable]
        private NetworkPlayerText m_playerNameText;

        public GameObject ScenePlayer => m_scenePlayer;
        public GameObject AvatarPrefab => m_avatarPrefab;
        public bool IsMine => RootPhotonView.IsMine;

        [System.NonSerialized]
        private string m_scenePlayerPath;

        private PhotonView m_photonView;
        public PhotonView RootPhotonView
        {
            get
            {
                if (!m_photonView)
                {
                    m_photonView = GetComponent<PhotonView>();
                }
                return m_photonView;
            }
        }

        protected virtual void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void Update()
        {
            if (m_scenePlayer && IsMine)
            {
                SyncTrasforms(m_scenePlayer.transform, transform);
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
        }

        public virtual void InitializeAvatar(string playerName, GameObject scenePlayer)
        {
            m_scenePlayer = scenePlayer;
            m_scenePlayerPath = scenePlayer ? SceneTools.GetGameObjectPath(scenePlayer) : null;

            if (!string.IsNullOrEmpty(playerName))
            {
                var networkText = GetComponentInChildren<NetworkPlayerText>(true);
                if (networkText)
                {
                    networkText.SendAndHide(playerName);
                }
            }

            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            SceneManager.sceneLoaded += SceneManager_SceneLoaded;
        }

        private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (!m_scenePlayer && !string.IsNullOrEmpty(m_scenePlayerPath))
            {
                m_scenePlayer = SceneTools.GetGameObjectAtScenePath(m_scenePlayerPath);
                if (!m_scenePlayer)
                {
                    int lastIndex = m_scenePlayerPath.LastIndexOf('/');
                    m_scenePlayer = GameObject.Find(m_scenePlayerPath.Substring(lastIndex + 1, m_scenePlayerPath.Length - lastIndex - 1));
                }
                if (m_scenePlayer)
                {
                    InitializeAvatar(null, m_scenePlayer);
                }
            }
        }

        protected static void SyncTrasforms(Transform source, Transform destination)
        {
            if (source && destination)
            {
                destination.SetPositionAndRotation(source.position, source.rotation);
                destination.localScale = source.localScale;
            }
        }
    }
}
