using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using UnityEngine.UI;
using UnityEngine.Events;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(Text))]
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Advanced/Network UI Text")]
    public class NetworkText :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        
        private string m_text;
        public string Text
        {
            get { return TextComponent?.text; }
            set
            {
                if(TextComponent != null && TextComponent.text != value)
                {
                    TextComponent.text = value;
                }
            }
        }

        private Text m_textComponent;
        protected Text TextComponent
        {
            get
            {
                if (m_textComponent == null)
                {
                    m_textComponent = GetComponent<Text>();
                }
                return m_textComponent;
            }
        }

        private void OnEnable()
        {
            m_text = TextComponent.text;
        }

#if WEAVR_NETWORK
        private void Update()
        {
            if(m_text != TextComponent.text)
            {
                m_text = TextComponent.text;
                if (CanRPC())
                {
                    SelfBufferedRPC(nameof(RemoteTextRPC), m_text);
                }
            }
        }

        [PunRPC]
        private void RemoteTextRPC(string text)
        {
            m_setFrameCount = Time.frameCount;
            TextComponent.text = text;
            m_text = text;
        }
#endif
    }
}
