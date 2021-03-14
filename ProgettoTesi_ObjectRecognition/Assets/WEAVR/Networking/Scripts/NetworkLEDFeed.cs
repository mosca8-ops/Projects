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
    [RequireComponent(typeof(LEDFeed))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Network LED Feed")]
    public class NetworkLEDFeed :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        [SerializeField]
        private bool m_immediate = true;

        protected override bool IsImmediate => m_immediate;

        private LEDFeed m_ledFeed;
        protected LEDFeed LEDFeed
        {
            get
            {
                if (m_ledFeed == null)
                {
                    m_ledFeed = GetComponent<LEDFeed>();
                }
                return m_ledFeed;
            }
        }

        private void OnEnable()
        {
            LEDFeed.onLEDColorIndexChanged.AddListener(RemoteLEDIndexChanged);
        }

        private void OnDisable()
        {
            LEDFeed.onLEDColorIndexChanged.RemoveListener(RemoteLEDIndexChanged);
        }

        protected void RemoteLEDIndexChanged(int index)
        {
            if (CanRPC())
            {
                SelfBufferedRPC(nameof(RemoteLEDIndexChangedRPC), LEDFeed.CurrentLEDColorIndex);
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteLEDIndexChangedRPC(int ledIndex)
        {
            m_setFrameCount = Time.frameCount;
            LEDFeed.CurrentLEDColorIndex = ledIndex;
        }
#else
        private void RemoteLEDIndexChangedRPC(int ledIndex)
        {
        }
#endif
    }
}
