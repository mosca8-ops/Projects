using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TXT.WEAVR.Common;
using TXT.WEAVR.UI;

#if WEAVR_NETWORK
using Photon.Pun;
#endif

namespace TXT.WEAVR.Networking
{
#if WEAVR_NETWORK
    [RequireComponent(typeof(PhotonView))]
#endif
    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Setup/Network Billboard Manager")]
    public class NetworkBillboard :
#if WEAVR_NETWORK
        RpcMonoBehaviour
#else
        NoRpcMonoBehaviour
#endif
    {
        private BillboardManager m_billboardManager;

        private void OnEnable()
        {
            m_billboardManager = BillboardManager.Instance;
            if (m_billboardManager != null)
            {
                m_billboardManager.OnBillboardShowWithText.AddListener(RemoteOnBillboardShow);
                m_billboardManager.OnBillboardHide.AddListener(RemoteOnBillboardHide);
            }
        }

        private void OnDisable()
        {
            if (m_billboardManager != null)
            {
                m_billboardManager.OnBillboardShowWithText.RemoveListener(RemoteOnBillboardShow);
                m_billboardManager.OnBillboardHide.RemoveListener(RemoteOnBillboardHide);
            }
        }

        protected void RemoteOnBillboardShow(GameObject go, string text)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteShowBillboardOn), go.GetHierarchyPath(), text);
            }
        }

        protected void RemoteOnBillboardHide(GameObject go)
        {
            if (CanRPC())
            {
                SelfRPC(nameof(RemoteHideBillboardOn), go.GetHierarchyPath());
            }
        }

#if WEAVR_NETWORK
        [PunRPC]
        private void RemoteShowBillboardOn(string goPath, string text)
        {
            m_setFrameCount = Time.frameCount;
            var go = GameObjectExtensions.FindInScene(goPath);
            if(go != null)
            {
                BillboardManager.Instance.ShowBillboardOn(go, text);
            }
        }

        [PunRPC]
        private void RemoteHideBillboardOn(string goPath)
        {
            m_setFrameCount = Time.frameCount;
            var go = GameObjectExtensions.FindInScene(goPath);
            if (go != null)
            {
                BillboardManager.Instance.HideBillboardOn(go);
            }
        }
#else
        private void RemoteShowBillboardOn(string goPath, string text)
        {
        }
        
        private void RemoteHideBillboardOn(string goPath)
        {
        }
#endif
    }
}
