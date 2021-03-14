using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
#if WEAVR_EXTENSIONS_OBI
    [RequireComponent(typeof(ObiRopeHandler))]
#endif
#if WEAVR_NETWORK
    [AddComponentMenu("WEAVR/Multiplayer/Advanced/Network Rope Handler")]
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    public class NetworkRopeHandler :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {

        #if WEAVR_EXTENSIONS_OBI
        private ObiRopeHandler m_ropeHandler;
        protected ObiRopeHandler RopeHandler
        {
            get
            {
                if (m_ropeHandler == null)
                {
                    m_ropeHandler = GetComponent<ObiRopeHandler>();
                }
                return m_ropeHandler;
            }
        }

        private void OnEnable()
        {
            RopeHandler.OnStateChanged.AddListener(RemoteStateChanged);
        }

        private void OnDisable()
        {
            RopeHandler.OnStateChanged.RemoveListener(RemoteStateChanged);
        }

        protected void RemoteStateChanged(bool value)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteStateChangedRPC), value);
            }
        }
#endif

#if WEAVR_NETWORK && WEAVR_EXTENSIONS_OBI

        [PunRPC]
        private void RemoteStateChangedRPC(bool value)
        {
            m_setFrameCount = Time.frameCount;
            if(!value && RopeHandler.IsActive)
            {
                SelfRPC(nameof(RemoteStateChangedRPC), true);
            }
            value |= RopeHandler.IsActive;
            RopeHandler.IsActive = value;
        }
#else
        private void RemoteStateChangedRPC(bool value)
        {
        }
#endif
    }
}
