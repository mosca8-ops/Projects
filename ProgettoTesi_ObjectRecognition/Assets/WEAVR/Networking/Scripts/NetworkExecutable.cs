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
    [RequireComponent(typeof(AbstractExecutable))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Interactions/Network Executable")]
    public class NetworkExecutable :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private AbstractExecutable m_executable;
        protected AbstractExecutable Executable
        {
            get
            {
                if (m_executable == null)
                {
                    m_executable = GetComponent<AbstractExecutable>();
                }
                return m_executable;
            }
        }

        private void OnEnable()
        {
            Executable.onExecute.AddListener(RemoteExecute);
        }

        private void OnDisable()
        {
            Executable.onExecute.RemoveListener(RemoteExecute);
        }

        protected void RemoteExecute()
        {
            if (CanRPC())
            {
                SelfBufferedRPC(nameof(RemoteExecutableExecuteRPC));
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteExecutableExecuteRPC()
        {
            m_setFrameCount = Time.frameCount;
            Executable.Execute();
        }
#else
        private void RemoteExecutableExecuteRPC()
        {
        }
#endif
    }
}
