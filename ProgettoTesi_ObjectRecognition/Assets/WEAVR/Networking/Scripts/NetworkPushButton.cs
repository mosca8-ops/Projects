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
    [RequireComponent(typeof(AbstractPushButton))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Push Button")]
    public class NetworkPushButton :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        const int k_syncFramesBuffer = 5;

        public bool isImmediate = true;

        protected override bool IsImmediate => isImmediate;

        private AbstractPushButton m_pushButton;
        protected AbstractPushButton PushButton
        {
            get
            {
                if (m_pushButton == null)
                {
                    m_pushButton = GetComponent<AbstractPushButton>();
                }
                return m_pushButton;
            }
        }

        private void OnEnable()
        {
            PushButton.OnStateChanged.AddListener(RemoteStateChanged);
            PushButton.OnContinuouslyDown.AddListener(RemoteContinuouslyDown);
        }
        
        private void OnDisable()
        {
            PushButton.OnStateChanged.RemoveListener(RemoteStateChanged);
            PushButton.OnContinuouslyDown.RemoveListener(RemoteContinuouslyDown);
        }

        protected void RemoteStateChanged()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteStateChangedRPC), PushButton.CurrentState.ToString());
            }
        }

        private void RemoteContinuouslyDown(float time)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteContinuouslyDownRPC), time);
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteStateChangedRPC(string state)
        {
            m_setFrameCount = Time.frameCount + (int)(PushButton.TransitionTime / Time.deltaTime) + k_syncFramesBuffer;
            //PushButton.SilentlySetState((AbstractPushButton.State)Enum.Parse(typeof(AbstractPushButton.State), state));
            PushButton.CurrentState = (AbstractPushButton.State)Enum.Parse(typeof(AbstractPushButton.State), state);
        }

        [PunRPC]
        private void RemoteContinuouslyDownRPC(float time)
        {
            m_setFrameCount = Time.frameCount + (int)(PushButton.TransitionTime / Time.deltaTime) + k_syncFramesBuffer;
            PushButton.SilentlySetState(AbstractPushButton.State.Down, time);
        }
#else
        private void RemoteStateChangedRPC(string state)
        {
        }

        private void RemoteContinuouslyDownRPC(float time)
        {

        }
#endif
    }
}
