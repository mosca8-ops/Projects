using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using UnityEngine.UI;
using UnityEngine.Events;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network UI Button")]
    public class NetworkButton :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {

        public UnityEvent onRemoteClick;

        private Button m_button;
        protected Button Button
        {
            get
            {
                if (m_button == null)
                {
                    m_button = GetComponent<Button>();
                }
                return m_button;
            }
        }

        private void OnEnable()
        {
            Button.onClick.RemoveListener(NotifyOnClick);
            Button.onClick.AddListener(NotifyOnClick);
        }

        private void OnDisable()
        {
            Button.onClick.RemoveListener(NotifyOnClick);
        }

        protected void NotifyOnClick()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteClickRPC));
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteClickRPC()
        {
            m_setFrameCount = Time.frameCount;
            Button.onClick.Invoke();
            onRemoteClick.Invoke();
        }

        public override void Register()
        {
            base.Register();
            Button.onClick.RemoveListener(NotifyOnClick);
            Button.onClick.AddListener(NotifyOnClick);
        }
#else
        private void RemoteClickRPC()
        {

        }
#endif
    }
}
