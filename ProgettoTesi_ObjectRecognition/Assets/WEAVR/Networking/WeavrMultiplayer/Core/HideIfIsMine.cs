
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Networking
{

    [AddComponentMenu("WEAVR/Network/Hide If Mine")]
    public class HideIfIsMine : Photon.Pun.MonoBehaviourPun
    {
        [Draggable]
        public List<Behaviour> components;

        private void OnEnable()
        {
            foreach (var child in GetComponentsInChildren<Renderer>(true))
            {
                //Debug.Log(child);
                //Debug.Log("enabled "+ !photonView.IsMine);
                child.enabled = !photonView.IsMine;
            }

            foreach (var component in components)
            {
                component.enabled = !photonView.IsMine;
            }
        }
    }
}
