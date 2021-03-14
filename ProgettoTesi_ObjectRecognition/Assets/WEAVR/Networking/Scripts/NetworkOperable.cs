using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.Maintenance;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{

    [RequireComponent(typeof(AbstractOperable))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Operable")]
    public class NetworkOperable :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractOperable m_operable;
        protected AbstractOperable Operable
        {
            get
            {
                if (!m_operable)
                {
                    m_operable = GetComponent<AbstractOperable>();
                }
                return m_operable;
            }
        }

#if WEAVR_NETWORK

        private void OnEnable()
        {
            Operable.onValueChange.AddListener(UpdateNetworkOperableProgress);
        }
        private void OnDisable()
        {
            Operable.onValueChange.RemoveListener(UpdateNetworkOperableProgress);
        }

        protected void UpdateNetworkOperableProgress(float progress)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(SetProgressNetwork), progress);
            }
        }

        [PunRPC]
        private void SetProgressNetwork(float progress)
        {
            m_setFrameCount = Time.frameCount;
            Operable.Value = progress;
        }
#else
        private void SetProgressNetwork(float progress)
        {

        }
#endif
    }
}
