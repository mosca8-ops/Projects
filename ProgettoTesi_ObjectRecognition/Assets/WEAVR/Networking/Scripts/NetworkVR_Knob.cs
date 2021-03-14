using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{

    [RequireComponent(typeof(VR_Knob))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network VR Knob")]
    public class NetworkVR_Knob :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        [SerializeField]
        private bool m_smoothValue = true;

        private float? m_targetValue;

        private VR_Knob m_knob;
        protected VR_Knob Knob
        {
            get
            {
                if (m_knob == null)
                {
                    m_knob = GetComponent<VR_Knob>();
                }
                return m_knob;
            }
        }

#if WEAVR_NETWORK

        protected override bool CanRPC()
        {
            return m_knob.IsCurrentBehaviour && base.CanRPC();
        }

        private void OnEnable()
        {
            Knob.onValueChanged.AddListener(RemoteChangeValue);
        }

        private void OnDisable()
        {
            Knob.onValueChanged.RemoveListener(RemoteChangeValue);
        }

        protected void RemoteChangeValue(float progress)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteChangeValueRPC), progress);
            }
        }


        //private void FixedUpdate()
        //{
        //    if (m_targetProgress.HasValue && !Door.IsLocked)
        //    {
        //        Door.SilentAnimateDoorMovement(Mathf.MoveTowards(Door.CurrentOpenProgress, m_targetProgress.Value, (1.0f / PhotonNetwork.SerializationRate)));
        //        if(Door.CurrentOpenProgress == m_targetProgress.Value)
        //        {
        //            m_targetProgress = null;
        //        }
        //    }
        //}

        private void Update()
        {
            if (m_targetValue.HasValue)
            {
                float distance = Mathf.Max(1, Mathf.Abs(m_targetValue.Value - Knob.Value));
                Knob.Value = Mathf.MoveTowards(Knob.Value, m_targetValue.Value, Time.deltaTime * distance);
                if(Knob.Value == m_targetValue.Value)
                {
                    m_targetValue = null;
                }
                else
                {
                    m_setFrameCount = Time.frameCount + 30;
                }
            }
        }

        [PunRPC]
        private void RemoteChangeValueRPC(float progress)
        {
            m_setFrameCount = Time.frameCount;
            if (m_smoothValue)
            {
                m_targetValue = progress;
            }
            else
            {
                Knob.Value = progress;
            }
        }
#else
        private void RemoteChangeValueRPC(float progress)
        {

        }
#endif
    }
}
