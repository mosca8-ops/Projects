using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Network Component Toggle")]
    public class NetworkComponentToggle :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        [Tooltip("If true, the gameobject will be destroyed if this component is destroyed")]
        public bool destroyGameObject = false;
        [GenericComponent]
        public Behaviour[] componentsToToggle;

        private bool[] m_componentStatuses;

        private void OnEnable()
        {
            m_componentStatuses = new bool[componentsToToggle.Length];
            for (int i = 0; i < componentsToToggle.Length; i++)
            {
                if (componentsToToggle[i] != null)
                {
                    m_componentStatuses[i] = componentsToToggle[i].enabled;
                }
            }
            if (CanRPC())
            {
                SelfRPC(nameof(EnableNetwork), -1, true);
            }
        }

        private void OnDisable()
        {
            if (!CanRPC()) { return; }
            if (!destroyGameObject || !gameObject.activeInHierarchy)
            {
                SelfRPC(nameof(EnableNetwork), -1, false);
            }
        }

        private void OnDestroy()
        {
            SelfRPC(nameof(DestroyNetwork));
        }

#if WEAVR_NETWORK

        private void Update()
        {
            if (!CanRPC()) { return; }
            for (int i = 0; i < componentsToToggle.Length; i++)
            {
                if (componentsToToggle[i] != null && componentsToToggle[i].enabled != m_componentStatuses[i])
                {
                    m_componentStatuses[i] = componentsToToggle[i].enabled;
                    SelfRPC(nameof(EnableNetwork), i, componentsToToggle[i].enabled);
                }
            }
        }

        [PunRPC]
        private void EnableNetwork(int index, bool enable)
        {
            m_setFrameCount = Time.frameCount;
            if(index < 0)
            {
                enabled = enable;
            }
            else if(0 <= index && index < componentsToToggle.Length && componentsToToggle[index] != null)
            {
                componentsToToggle[index].enabled = enable;
            }
        }

        [PunRPC]
        private void DestroyNetwork()
        {
            m_setFrameCount = Time.frameCount;
            if (gameObject != null && destroyGameObject)
            {
                Destroy(gameObject);
            }
        }
#else
        private void EnableNetwork(int index, bool enable)
        {

        }
        
        private void DestroyNetwork()
        {

        }
#endif
    }
}
