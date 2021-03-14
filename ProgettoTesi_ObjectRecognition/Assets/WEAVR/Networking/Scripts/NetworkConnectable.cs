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
    [RequireComponent(typeof(AbstractConnectable))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Connectable")]
    public class NetworkConnectable :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractConnectable m_connectable;
        protected AbstractConnectable Connectable
        {
            get
            {
                if (m_connectable == null)
                {
                    m_connectable = GetComponent<AbstractConnectable>();
                }
                return m_connectable;
            }
        }

        private void OnEnable()
        {
            Connectable.OnConnected.AddListener(RemoteOnConnected);
            Connectable.OnDisconnected.AddListener(RemoteOnDisconnected);
        }

        private void OnDisable()
        {
            Connectable.OnConnected.RemoveListener(RemoteOnConnected);
            Connectable.OnDisconnected.RemoveListener(RemoteOnDisconnected);
        }

        protected void RemoteOnDisconnected()
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteDisconnectRPC));
            }
        }

        protected void RemoteOnConnected()
        {
            if (CanRPC() && Connectable.ConnectedObject != null)
            {
                SelfRPC(nameof(RemoteConnectRPC), Connectable.ConnectedObject.GetHierarchyPath());
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteDisconnectRPC()
        {
            m_setFrameCount = Time.frameCount;
            Connectable.IsConnected = false;
        }

        [PunRPC]
        private void RemoteConnectRPC(string connectGO)
        {
            //m_setFrameCount = Time.frameCount + 1;
            var go = GameObjectExtensions.FindInScene(connectGO);
            if (go != null)
            {
                Debug.Log($"Remote Connecting to {go}");
                StartCoroutine(SetConnectedForMoreFrames(go.GetComponent<AbstractConnectable>()));
            }
        }

        private IEnumerator SetConnectedForMoreFrames(AbstractConnectable connectable)
        {
            if (connectable != null)
            {
                int framesToCheck = 180;
                m_setFrameCount = Time.frameCount + 1;
                //Connectable.ConnectedObject = connectable;
                Connectable.Connect(connectable);
                yield return new WaitForEndOfFrame();
                while (!Connectable.IsConnected && Connectable.PotentialConnectable == connectable && framesToCheck-- > 0)
                {
                    m_setFrameCount = Time.frameCount + 1;
                    Connectable.Connect(connectable);
                    //Connectable.ConnectedObject = connectable;
                    yield return new WaitForEndOfFrame();
                }
                Debug.Log($"Exited coroutine: Connected = {Connectable.IsConnected} and PotentialConnector = {Connectable.PotentialConnectable} with frames remaining: {framesToCheck}");
            }
        }
#else
        private void RemoteDisconnectRPC()
        {
        }
        
        private void RemoteConnectRPC(string connectGO)
        {
        }
#endif
    }
}
