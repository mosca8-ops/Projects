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
    [RequireComponent(typeof(AbstractThreeWaySwitch))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network 3-Way Switch")]
    public class NetworkThreeWaySwitch :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractThreeWaySwitch m_switch;
        protected AbstractThreeWaySwitch Switch
        {
            get
            {
                if (m_switch == null)
                {
                    m_switch = GetComponent<AbstractThreeWaySwitch>();
                }
                return m_switch;
            }
        }
        
        const int k_syncFramesBuffer = 5;

        public bool isImmediate = true;

        protected override bool IsImmediate => isImmediate;

#if WEAVR_NETWORK

        private void OnEnable()
        {
            Switch.OnStateChanged.AddListener(RemoteStateChanged);
            Switch.OnContinuouslyUp.AddListener(RemoteContinusStateChanged);
            Switch.OnContinuouslyDown.AddListener(RemoteContinusStateChanged);
        }

        private void OnDisable()
        {
            Switch.OnStateChanged.RemoveListener(RemoteStateChanged);
            Switch.OnContinuouslyUp.RemoveListener(RemoteContinusStateChanged);
            Switch.OnContinuouslyDown.RemoveListener(RemoteContinusStateChanged);
        }

        protected void RemoteStateChanged()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteStateChangedRPC), (int)Switch.CurrentState);
            }
        }

        protected void RemoteContinusStateChanged(float time)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteContinuousStateChangedRPC), Switch.CurrentState, time);
            }
        }

        [PunRPC]
        private void RemoteStateChangedRPC(int state)
        {
            m_setFrameCount = Time.frameCount + (int)(Switch.TransitionTime / Time.deltaTime);
            Switch.CurrentState = (Switch3WayState)state;
        }

        [PunRPC]
        private void RemoteContinuousStateChangedRPC(int state, float time)
        {
            m_setFrameCount = Time.frameCount + (int)(Switch.TransitionTime / Time.deltaTime) + k_syncFramesBuffer;
            Switch.SilentlySetState((Switch3WayState)state, time);
        }
#endif
    }
}
