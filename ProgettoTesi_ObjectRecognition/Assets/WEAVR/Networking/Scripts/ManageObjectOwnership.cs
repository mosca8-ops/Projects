
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Maintenance;
using UnityEngine;
using TXT.WEAVR.Networking;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking {

    /// <summary>
    /// This class manages ownership of a scene object in order to control movement with specific conditions.
    /// </summary>
    [RequireComponent(typeof(AbstractGrabbable))]
#if WEAVR_NETWORK
[RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Manage Object Ownership")]
    public class ManageObjectOwnership :
#if WEAVR_NETWORK
    MonoBehaviourPun
#else
    MonoBehaviour
#endif
    {
        private Grabbable m_grabbable;
        
        public Grabbable Grabbable
        {
            get
            {
                if (!m_grabbable)
                {
                    m_grabbable = GetComponent<Grabbable>();
                }
                return m_grabbable;
            }
        }

#if WEAVR_NETWORK
        private void Reset()
        {
            var photonView = GetComponent<PhotonView>();
            if (!photonView)
            {
                photonView = gameObject.AddComponent<PhotonView>();
            }

            photonView.OwnershipTransfer = OwnershipOption.Takeover;
            photonView.Synchronization = ViewSynchronization.UnreliableOnChange;

            var rigidBody = GetComponent<Rigidbody>();

            if (!rigidBody && !GetComponent<PhotonTransformView>())
            {
                gameObject.AddComponent<PhotonTransformView>();
            }
            else if (rigidBody && !GetComponent<PhotonRigidbodyView>())
            {
                var view = gameObject.AddComponent<PhotonRigidbodyView>();
                view.m_TeleportEnabled = true;
                view.m_TeleportIfDistanceGreaterThan = 3;
                view.m_SynchronizeAngularVelocity = true;
            }

            if (GetComponent<Rigidbody>() != null && GetComponent<PhotonRigidbodyViewExtension>() == null)
            {
                gameObject.AddComponent<PhotonRigidbodyViewExtension>();
            }

            if (photonView && photonView.ObservedComponents == null)
            {
                photonView.ObservedComponents = new List<Component>();
            }

            if (photonView && photonView.ObservedComponents != null)
            {
                AddIfNotPresent<PhotonTransformView>(photonView);
                AddIfNotPresent<PhotonRigidbodyView>(photonView);
                AddIfNotPresent<PhotonRigidbodyViewExtension>(photonView);
            }
        }

        private void AddIfNotPresent<T>(PhotonView photonView) where T : Component
        {
            var comp = GetComponent<T>();
            if (comp && !photonView.ObservedComponents.Contains(comp))
            {
                photonView.ObservedComponents.Add(comp);
            }
        }

        private void OnValidate()
        {
            for (int i = 0; i < photonView.ObservedComponents.Count; i++)
            {
                if (photonView.ObservedComponents[i] == null)
                {
                    photonView.ObservedComponents.RemoveAt(i--);
                }
            }
            AddIfNotPresent<PhotonTransformView>(photonView);
            AddIfNotPresent<PhotonRigidbodyView>(photonView);
            AddIfNotPresent<PhotonRigidbodyViewExtension>(photonView);
        }

        // Use this for initialization
        void Start()
        {
            if (!m_grabbable)
            {
                m_grabbable = GetComponent<Grabbable>();
            }
        }

        private void OnEnable()
        {
            Grabbable?.onGrab.AddListener(ReleaseOthers);
        }

        private void OnDisable()
        {
            Grabbable?.onGrab.RemoveListener(ReleaseOthers);
        }

        public void ReleaseOthers()
        {
            photonView.RPC(nameof(Release), RpcTarget.Others, photonView.ViewID);
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }

        [PunRPC]
        private void Release(int viewID)
        {
            if (photonView.ViewID == viewID && m_grabbable)
            {
                m_grabbable.Release();
            }
        }
#endif

    }
}
