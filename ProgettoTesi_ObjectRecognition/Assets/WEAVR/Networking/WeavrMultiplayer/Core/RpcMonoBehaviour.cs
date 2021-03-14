using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;

namespace TXT.WEAVR.Networking
{
    //[RequireComponent(typeof(PhotonView))]
    public abstract class RpcMonoBehaviour : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Whether to send the calls to new players or not")]
        protected bool m_sendToNewPlayers = false;

        private bool m_didAwake;

        protected int m_setFrameCount;

        protected virtual bool IsImmediate => false;

        protected virtual bool CanRPC()
        {
            return m_setFrameCount < Time.frameCount;
        }

        private PhotonView m_photonView;
        protected PhotonView PhotonView
        {
            get
            {
                if (!m_photonView)
                {
                    m_photonView = GetComponent<PhotonView>();
                    if (m_photonView && m_photonView.ViewID != 0 && !m_didAwake)
                    {
                        PhotonNetwork.RegisterPhotonView(m_photonView);
                    }
                }
                return m_photonView;
            }
        }

        protected virtual void Awake()
        {
            m_didAwake = true;
        }

        public virtual void Register()
        {
            if (!m_photonView)
            {
                m_photonView = GetComponent<PhotonView>();
                if (m_photonView && m_photonView.ViewID != 0 && !m_didAwake)
                {
                    PhotonNetwork.RegisterPhotonView(m_photonView);
                }
            }
        }


        protected S GetNetComponent<S>(int viewId) where S : Component
        {
            return PhotonView.Find(viewId)?.GetComponent<S>();
        }

        protected bool SelfRPC(string methodName, params object[] parameters)
        {
            if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
            {
                if (m_sendToNewPlayers)
                {
                    PhotonView.RPC(methodName, RpcTarget.OthersBuffered, parameters);
                }
                else
                {
                    PhotonView.RPC(methodName, RpcTarget.Others, parameters);
                }
                if (IsImmediate)
                {
                    PhotonNetwork.SendAllOutgoingCommands();
                }
                return true;
            }
            return false;
        }

        protected bool SelfBufferedRPC(string methodName, params object[] parameters)
        {
            if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
            {
                PhotonView.RPC(methodName, RpcTarget.OthersBuffered, parameters);
                if (IsImmediate)
                {
                    PhotonNetwork.SendAllOutgoingCommands();
                }
                return true;
            }
            return false;
        }
    }
    

    public abstract class RpcMonoBehaviour<T> : RpcMonoBehaviour where T : Component
    {

        protected T GetSelf(int viewId)
        {
            return PhotonView.Find(viewId)?.GetComponent<T>();
        }
    }
}
