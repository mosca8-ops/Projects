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
    [RequireComponent(typeof(AbstractTwoWaySwitch))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network 2-Way Switch")]
    public class NetworkTwoWaySwitch :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractTwoWaySwitch m_switch;
        protected AbstractTwoWaySwitch Switch
        {
            get
            {
                if (m_switch == null)
                {
                    m_switch = GetComponent<AbstractTwoWaySwitch>();
                }
                return m_switch;
            }
        }

        private void OnEnable()
        {
            Switch.OnStateChanged.AddListener(RemoteStateChanged);
        }

        private void OnDisable()
        {
            Switch.OnStateChanged.RemoveListener(RemoteStateChanged);
        }

        protected void RemoteStateChanged()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteStateChangedRPC), Switch.CurrentState.ToString());
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteStateChangedRPC(string state)
        {
            m_setFrameCount = Time.frameCount + (int)(Switch.TransitionTime * 100);
            //Switch.SilentlySetState((Switch2WayState)Enum.Parse(typeof(Switch2WayState), state));
            Switch.CurrentState = (Switch2WayState)Enum.Parse(typeof(Switch2WayState), state);
        }
#else
        private void RemoteStateChangedRPC(string state)
        {
        }
#endif
    }
}
