using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Network RPC Registry")]
    public class NetworkRPCRegistry : MonoBehaviour
    {
        [SerializeField]
        [Button(nameof(RefreshList), "Refresh")]
        [ShowAsReadOnly]
        private int rpcsCount;
        [SerializeField]
        [Draggable]
        private List<RpcMonoBehaviour> m_rpcBehaviours;
        private bool m_registered;

        private void Reset()
        {
            RefreshList();
        }

        private void OnValidate()
        {
            if (m_rpcBehaviours != null)
            {
                rpcsCount = m_rpcBehaviours.Count;
            }
        }

        // Use this for initialization
        void Start()
        {
            if (!m_registered)
            {
                m_registered = true;
                for (int i = 0; i < m_rpcBehaviours.Count; i++)
                {
                    if (m_rpcBehaviours[i])
                    {
                        m_rpcBehaviours[i].Register();
                    }
                }
            }
        }

        [ContextMenu("Refresh List")]
        private void RefreshList()
        {
            if(m_rpcBehaviours == null)
            {
                m_rpcBehaviours = new List<RpcMonoBehaviour>();
            }
            else
            {
                m_rpcBehaviours.Clear();
            }

            HashSet<GameObject> rpcGO = new HashSet<GameObject>();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach(var rpc in root.GetComponentsInChildren<RpcMonoBehaviour>(true))
                {
                    if (!rpcGO.Contains(rpc.gameObject))
                    {
                        m_rpcBehaviours.Add(rpc);
                        rpcGO.Add(rpc.gameObject);
                    }
                }
                //m_rpcBehaviours.AddRange(root.GetComponentsInChildren<RpcMonoBehaviour>(true));
            }
            rpcsCount = m_rpcBehaviours.Count;
        }
    }
}
