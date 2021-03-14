
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Networking
{
    [DefaultExecutionOrder(-27000)]
    [RequireComponent(typeof(Photon.Pun.PhotonView))]
    [AddComponentMenu("WEAVR/Network/Destroy If Not Mine")]
    public class DestroyIfNotMine : Photon.Pun.MonoBehaviourPun
    {
        [Draggable]
        public List<Behaviour> components;

        private void Awake()
        {
            foreach (var component in components)
            {
                if (!photonView.IsMine)
                {
                    Destroy(component);
                }
            }
        }
    }
}
