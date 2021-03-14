using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using System;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(AbstractDoor))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Door")]
    public class NetworkDoor : MonoBehaviour
#if WEAVR_NETWORK
        , IPunObservable
#endif
    {

#if WEAVR_NETWORK
        private PhotonView m_photonView;
#endif
        private float m_nextOpenProgress;
        private float m_distance;
        private bool m_firstTake = false;

        private AbstractDoor m_door;
        protected AbstractDoor Door
        {
            get
            {
                if(m_door == null)
                {
                    m_door = GetComponent<AbstractDoor>();
                }
                return m_door;
            }
        }

#if WEAVR_NETWORK

        private void Reset()
        {
            PreparePhotonView();
            if (!m_photonView.ObservedComponents.Contains(this))
            {
                m_photonView.ObservedComponents.Add(this);
            }
            if(m_photonView.Synchronization == ViewSynchronization.Off)
            {
                m_photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            }
            m_photonView.OwnershipTransfer = OwnershipOption.Takeover;
        }

        private void PreparePhotonView()
        {
            if (!m_photonView) { m_photonView = GetComponent<PhotonView>(); }
            if (!m_photonView) { m_photonView = gameObject.AddComponent<PhotonView>(); }
            if (!m_photonView.ObservedComponents.Contains(this))
            {
                m_photonView.ObservedComponents.Add(this);
            }
        }

        private void OnValidate()
        {
            PreparePhotonView();
        }

        private void Awake()
        {
            if (!m_door)
            {
                m_door = GetComponent<AbstractDoor>();
            }
            PreparePhotonView();
        }

        void OnEnable()
        {
            Door.OnDoorAction -= Door_OnDoorAction;
            Door.OnDoorAction += Door_OnDoorAction;
            m_firstTake = true;
        }

        private void Door_OnDoorAction(AbstractDoor door, AbstractDoor.DoorAction action)
        {
            if(action == AbstractDoor.DoorAction.StartInteraction && !m_photonView.IsMine)
            {
                m_photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            }
        }

        void OnDisable()
        {
            Door.OnDoorAction -= Door_OnDoorAction;
        }

        private void Update()
        {
            if (!m_photonView.IsMine && Door.CurrentOpenProgress != m_nextOpenProgress)
            {
                Door.AnimatedOpenProgress = Mathf.MoveTowards(Door.CurrentOpenProgress, m_nextOpenProgress, m_distance * (1.0f / PhotonNetwork.SerializationRate));
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(Door.CurrentOpenProgress);
            }
            else
            {
                if (m_firstTake)
                {
                    Door.AnimatedOpenProgress = m_nextOpenProgress = (float)stream.ReceiveNext();
                    m_distance = 0f;
                }
                else
                {
                    m_nextOpenProgress = (float)stream.ReceiveNext();
                    m_distance = Mathf.Abs(m_nextOpenProgress - Door.CurrentOpenProgress);
                }


                m_firstTake = false;
            }
        }
#endif
    }
}
