
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Networking;
using UnityEngine;
using System;

using Photon.Pun;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Scene Network")]
    public class SceneNetwork : MonoBehaviour, IWeavrSingleton
    {
        [SerializeField]
        private bool m_instantiateOnStart = false;
        [SerializeField]
        private bool m_keepAvatarBetweenScenes = false;

        private GameObject m_scenePlayer;

        [NonSerialized]
        private GameObject m_avatar;

        public GameObject SceneAvatar => m_avatar;
        public GameObject ScenePlayer
        {
            get => m_scenePlayer;
            set
            {
                if(m_scenePlayer != value)
                {
                    m_scenePlayer = value;
                }
            }
        }

        private string m_lastRoomJoined;
        private int m_lastAvatarViewId;
        private Coroutine m_queryAvatarCoroutine;

        public void RoomJoined(string playerName)
        {

        }

        private void Start()
        {
            if (m_instantiateOnStart && PhotonNetwork.InRoom && !WeavrNetwork.Instance.Avatar)
            {
                WeavrNetwork.Instance.Avatar = InstantiateAvatar(PhotonNetwork.LocalPlayer.NickName);
            }
        }

        public GameObject InstantiateAvatar(string playerName)
        {
            var surrogate = FindObjectOfType<NetworkSurrogate>();
            if (!surrogate) { return null; }
            GameObject avatar = surrogate.SurrogateAvatar;
            m_scenePlayer = surrogate.RootObject ? surrogate.RootObject : surrogate.gameObject;

            if (m_queryAvatarCoroutine != null)
            {
                StopCoroutine(m_queryAvatarCoroutine);
                m_queryAvatarCoroutine = null;
            }

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == m_lastRoomJoined)
            {
                var avatarView = PhotonNetwork.GetPhotonView(m_lastAvatarViewId);
                m_avatar = avatarView ? avatarView.gameObject : null;

                if (!m_avatar)
                {
                    m_queryAvatarCoroutine = StartCoroutine(QueryAvatar(playerName));
                }
            }
            else
            {
                m_avatar = PhotonNetwork.Instantiate(avatar.name, Vector3.zero, Quaternion.identity, 0);
                m_lastAvatarViewId = m_avatar.GetComponent<PhotonView>().ViewID;
            }

            m_lastRoomJoined = PhotonNetwork.CurrentRoom?.Name;

            if (m_avatar)
            {
                if (m_keepAvatarBetweenScenes)
                {
                    DontDestroyOnLoad(m_avatar);
                }
                if (m_queryAvatarCoroutine != null)
                {
                    StopCoroutine(m_queryAvatarCoroutine);
                    m_queryAvatarCoroutine = null;
                }

                var networkAvatar = m_avatar.GetComponentInChildren<NetworkAvatar>(true);
                if (networkAvatar)
                {
                    networkAvatar.InitializeAvatar(playerName, m_scenePlayer);
                }
            }

            return m_avatar;
        }

        private IEnumerator QueryAvatar(string playerName)
        {
            while (!m_avatar)
            {
                var avatarView = PhotonNetwork.GetPhotonView(m_lastAvatarViewId);
                m_avatar = avatarView ? avatarView.gameObject : null;
                yield return null;
            }

            var networkAvatar = m_avatar.GetComponentInChildren<NetworkAvatar>(true);
            if (networkAvatar)
            {
                networkAvatar.InitializeAvatar(playerName, m_scenePlayer);
            }
            m_queryAvatarCoroutine = null;
        }

        [Serializable]
        private struct PlayerRemotePair
        {
            public GameObject sceneObject;
            public GameObject avatar;
        }
    }
}
