using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using System.Linq;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Network Objects Toggle")]
    public class NetworkObjectsToggle :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        public GameObject[] objectsToTrack;
        private bool[] objectsStatuses;

        private void Start()
        {
            objectsStatuses = new bool[objectsToTrack.Length];
            for (int i = 0; i < objectsToTrack.Length; i++)
            {
                objectsStatuses[i] = objectsToTrack[i].activeSelf;
            }
        }

#if WEAVR_NETWORK

        private void Reset()
        {
            objectsToTrack = GetComponentsInChildren<PhotonView>().Select(c => c.gameObject).ToArray();
        }

        private void Update()
        {
            for (int i = 0; i < objectsToTrack.Length; i++)
            {
                if(objectsToTrack[i].activeSelf != objectsStatuses[i])
                {
                    objectsStatuses[i] = objectsToTrack[i].activeSelf;
                    SelfBufferedRPC(nameof(ToggleGameObject), i, objectsToTrack[i].activeSelf);
                }
            }
        }

        [PunRPC]
        private void ToggleGameObject(int index, bool enable)
        {
            if(0 <= index && index < objectsToTrack.Length)
            {
                objectsToTrack[index].SetActive(enable);
                objectsStatuses[index] = enable;
            }
        }

#endif
    }
}
