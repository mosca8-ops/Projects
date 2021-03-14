using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using UnityEngine.UI;
using UnityEngine.Events;

using Photon.Pun;

namespace TXT.WEAVR.Networking
{
    [RequireComponent(typeof(Text))]
    [AddComponentMenu("WEAVR/Network/Network Player Text")]
    public class NetworkPlayerText : RpcMonoBehaviour
    {
        
        public string Text
        {
            get { return TextComponent?.text; }
            set
            {
                if (TextComponent != null && TextComponent.text != value)
                {
                    TextComponent.text = value;
                    if (value != null) { SyncText(); }
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

        public void SendAndHide(string text)
        {
            gameObject.SetActive(true);
            Text = text;
            gameObject.SetActive(false);
        }

        //private void OnEnable()
        //{
        //    StartCoroutine(DelayedSyncText(1));
        //}

        private IEnumerator DelayedSyncText(float delay)
        {
            yield return new WaitForSeconds(delay);
            SyncText();
        }

        private void SyncText()
        {
            if (CanRPC())
            {
                SelfBufferedRPC(nameof(RemotePublishNameRPC), Text);
            }
        }

        [PunRPC]
        private void RemotePublishNameRPC(string text)
        {
            m_setFrameCount = Time.frameCount;
            Text = text;
        }
    }
}
