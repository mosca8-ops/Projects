using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(AbstractDoorLock))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Door Lock")]
    public class NetworkDoorLock :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractDoorLock m_doorLock;
        protected AbstractDoorLock DoorLock
        {
            get
            {
                if (m_doorLock == null)
                {
                    m_doorLock = GetComponent<AbstractDoorLock>();
                }
                return m_doorLock;
            }
        }

        private void OnEnable()
        {
            DoorLock.OnUnlock.AddListener(NotifyDoorLockIsUnlocked);
            DoorLock.OnLock.AddListener(NotifyDoorLockIsLocked);
        }

        private void OnDisable()
        {
            DoorLock.OnUnlock.RemoveListener(NotifyDoorLockIsUnlocked);
            DoorLock.OnLock.RemoveListener(NotifyDoorLockIsLocked);
        }

        protected void NotifyDoorLockIsLocked()
        {
            if (CanRPC())
            {
                SelfBufferedRPC(nameof(SetLockStateNetwork), true);
            }
        }

        protected void NotifyDoorLockIsUnlocked()
        {
            if (CanRPC())
            {
                SelfBufferedRPC(nameof(SetLockStateNetwork), false);
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void SetLockStateNetwork(bool isLocked)
        {
            m_setFrameCount = Time.frameCount + 5;
            DoorLock.IsLocked = isLocked;
        }
#else
        private void SetLockStateNetwork(bool isLocked)
        {

        }
#endif
    }
}
