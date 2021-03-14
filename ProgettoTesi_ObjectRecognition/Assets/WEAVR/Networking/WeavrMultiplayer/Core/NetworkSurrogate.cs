using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Network Surrogate")]
    public class NetworkSurrogate : MonoBehaviour
    {
        [SerializeField]
        [Draggable]
        public GameObject m_rootObject;
        [SerializeField]
        [Draggable]
        public GameObject m_surrogateAvatar;
        [SerializeField]
        private bool m_instantiateOnStart = false;

        [Header("Platform Overrides")]
        [Draggable]
        public GameObject m_PcSurrogate;
        [Draggable]
        public GameObject m_LinuxSurrogate;
        [Draggable]
        public GameObject m_MacSurrogate;
        [Draggable]
        public GameObject m_AndroidSurrogate;
        [Draggable]
        public GameObject m_IosSurrogate;
        [Draggable]
        public GameObject m_HololensSurrogate;

        public GameObject RootObject => m_rootObject ?? gameObject;

        private GameObject m_activeSurrogate;
        public GameObject SurrogateAvatar
        {
            get
            {
                if (!m_activeSurrogate)
                {
                    m_activeSurrogate = m_surrogateAvatar;
                    switch (Application.platform)
                    {
                        case RuntimePlatform.WindowsPlayer:
                        case RuntimePlatform.WindowsEditor:
                            if (m_PcSurrogate) { m_activeSurrogate = m_PcSurrogate; }
                            break;
                        case RuntimePlatform.OSXPlayer:
                        case RuntimePlatform.OSXEditor:
                            if (m_LinuxSurrogate) { m_activeSurrogate = m_LinuxSurrogate; }
                            break;
                        case RuntimePlatform.LinuxPlayer:
                        case RuntimePlatform.LinuxEditor:
                            if (m_MacSurrogate) { m_activeSurrogate = m_MacSurrogate; }
                            break;
                        case RuntimePlatform.Android:
                            if (m_AndroidSurrogate) { m_activeSurrogate = m_AndroidSurrogate; }
                            break;
                        case RuntimePlatform.IPhonePlayer:
                            if (m_IosSurrogate) { m_activeSurrogate = m_IosSurrogate; }
                            break;
                        case RuntimePlatform.WSAPlayerARM:
                        case RuntimePlatform.WSAPlayerX64:
                        case RuntimePlatform.WSAPlayerX86:
                            if (m_HololensSurrogate) { m_activeSurrogate = m_HololensSurrogate; }
                            break;
                    }
                }
                return m_activeSurrogate;
            }
        }

        protected virtual void Start()
        {
            if (m_instantiateOnStart && PhotonNetwork.InRoom && !WeavrNetwork.Instance.Avatar)
            {
                var sceneNetwork = this.TryGetSingleton<SceneNetwork>();
                if (sceneNetwork)
                {
                    WeavrNetwork.Instance.Avatar = sceneNetwork.InstantiateAvatar(PhotonNetwork.LocalPlayer.NickName);
                }
            }
        }
    }
}
