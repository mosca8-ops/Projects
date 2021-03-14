using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.Maintenance;
using System;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(AbstractGrabbable))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Grabbable")]
    public class NetworkGrabbable :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractGrabbable m_grabbable;
        protected AbstractGrabbable Grabbable
        {
            get
            {
                if (m_grabbable == null)
                {
                    m_grabbable = GetComponent<AbstractGrabbable>();
                }
                return m_grabbable;
            }
        }

        private void OnEnable()
        {
            Grabbable.onGrab.AddListener(RemoteGrab);
            Grabbable.onUngrab.AddListener(RemoteRelease);
        }

        private void OnDisable()
        {
            Grabbable.onGrab.AddListener(RemoteGrab);
            Grabbable.onUngrab.AddListener(RemoteRelease);
        }

        protected void RemoteGrab()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteGrabUngrabRPC), true);
            }
        }

        protected void RemoteRelease()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteGrabUngrabRPC), false);
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteGrabUngrabRPC(bool grabbed)
        {
            m_setFrameCount = Time.frameCount;
            Grabbable.IsGrabbedGlobally = grabbed;
        }
#else
        private void RemoteGrabUngrabRPC(bool grabbed)
        {
        }
#endif
    }
}
