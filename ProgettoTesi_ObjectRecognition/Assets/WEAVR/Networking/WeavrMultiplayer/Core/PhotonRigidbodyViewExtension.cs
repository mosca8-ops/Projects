
using UnityEngine;

using Photon.Pun;

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonRigidbodyView))]
    [RequireComponent(typeof(Rigidbody))]
    //[AddComponentMenu("Photon Networking/Photon Rigidbody View")]
    [AddComponentMenu("")]
    public class PhotonRigidbodyViewExtension : MonoBehaviour, IPunObservable
    {
        private enum SyncOptions { GravityAndKinematic, Everything }

        [SerializeField]
        private SyncOptions m_syncOptions = SyncOptions.Everything;

        private PhotonRigidbodyView m_photonRbodyView;
        private Rigidbody m_rigidBody;

        public void Awake()
        {
            m_photonRbodyView = GetComponent<PhotonRigidbodyView>();
            m_rigidBody = GetComponent<Rigidbody>();
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                if (m_syncOptions == SyncOptions.Everything)
                {
                    stream.SendNext(m_rigidBody.mass);
                    stream.SendNext(m_rigidBody.drag);
                    stream.SendNext(m_rigidBody.angularDrag);
                    stream.SendNext(m_rigidBody.useGravity);
                    stream.SendNext(m_rigidBody.isKinematic);
                    stream.SendNext(m_rigidBody.interpolation);
                    stream.SendNext(m_rigidBody.collisionDetectionMode);
                }
                else if (m_syncOptions == SyncOptions.GravityAndKinematic)
                {
                    stream.SendNext(m_rigidBody.useGravity);
                    stream.SendNext(m_rigidBody.isKinematic);
                }
            }
            else
            {
                if (m_syncOptions == SyncOptions.Everything)
                {
                    m_rigidBody.mass = (float)stream.ReceiveNext();
                    m_rigidBody.drag = (float)stream.ReceiveNext();
                    m_rigidBody.angularDrag = (float)stream.ReceiveNext();
                    m_rigidBody.useGravity = (bool)stream.ReceiveNext();
                    m_rigidBody.isKinematic = (bool)stream.ReceiveNext();
                    m_rigidBody.interpolation = (RigidbodyInterpolation)stream.ReceiveNext();
                    m_rigidBody.collisionDetectionMode = (CollisionDetectionMode)stream.ReceiveNext();
                }
                else if (m_syncOptions == SyncOptions.GravityAndKinematic)
                {
                    m_rigidBody.useGravity = (bool)stream.ReceiveNext();
                    m_rigidBody.isKinematic = (bool)stream.ReceiveNext();
                }
            }
        }

    }
}
