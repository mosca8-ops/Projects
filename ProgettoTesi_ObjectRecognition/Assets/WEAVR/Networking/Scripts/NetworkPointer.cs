using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{

    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Advanced/Network Pointer")]
    public class NetworkPointer :
#if WEAVR_NETWORK
    MonoBehaviourPun, IPunObservable
#else
    MonoBehaviour
#endif
    {
        public GameObject pointer;
        private Renderer m_renderer;

#if WEAVR_NETWORK
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (pointer == null) { return; }
            if (stream.IsWriting)
            {
                stream.SendNext(pointer.activeSelf);
                stream.SendNext(Color.r);
                stream.SendNext(Color.g);
                stream.SendNext(Color.b);
                stream.SendNext(Color.a);
            }
            else
            {
                pointer.SetActive((bool)stream.ReceiveNext());
                var color = m_renderer ? m_renderer.material.color : Color.clear;
                color.r = (float)stream.ReceiveNext();
                color.g = (float)stream.ReceiveNext();
                color.b = (float)stream.ReceiveNext();
                color.a = (float)stream.ReceiveNext();

                if (m_renderer)
                {
                    m_renderer.material.color = color;
                }
            }
        }

        public Color Color { get; set; }

        // Use this for initialization
        void Start()
        {
            if (!pointer)
            {
                pointer = transform.Find("Cube").gameObject;
            }
            m_renderer = pointer.GetComponent<Renderer>();
        }
#endif

    }
}
