using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.Networking
{

    [DisallowMultipleComponent]
    [AddComponentMenu("WEAVR/Multiplayer/Advanced/Network Avatar VR")]
    public class NetworkAvatarVR :
#if WEAVR_NETWORK
        NetworkAvatar
#else
        MonoBehaviour
#endif
    {
        [Header("Head")]
        [SerializeField]
        private Transform m_avatarHead;

        [Header("Left Hand")]
        [SerializeField]
        private Transform m_avatarHandLeft;
        [SerializeField]
        private Transform m_avatarPointerLeft;
        [SerializeField]
        private NetworkPointer m_remotePointerLeft;

        [Header("Right Hand")]
        [SerializeField]
        private Transform m_avatarHandRight;
        [SerializeField]
        private Transform m_avatarPointerRight;
        [SerializeField]
        private NetworkPointer m_remotePointerRight;
        
        [Header("Objects Names")]
        [SerializeField]
        private string m_handLeftName = "Hand_Left";
        [SerializeField]
        private string m_handRightName = "Hand_Right";
        [SerializeField]
        private string m_vrCameraName = "VRCamera";
        [SerializeField]
        private string m_pointerName = "3D Pointer";
        
#if WEAVR_NETWORK

        private Transform m_playerLeftHand;
        private Transform m_playerRightHand;
        private Transform m_playerVRCamera;

        private IPointer3D m_pointerLeft;
        private IPointer3D m_pointerRight;

        private Transform m_playerPointerLeft;
        private Transform m_playerPointerRight;

        // Update is called once per frame
        protected override void Update()
        {
            base.Update();

            if (!IsMine) { return; }

            SyncTrasforms(m_playerVRCamera.transform, m_avatarHead.transform);
            SyncTrasforms(m_playerLeftHand.transform, m_avatarHandLeft.transform);
            SyncTrasforms(m_playerRightHand.transform, m_avatarHandRight.transform);

            if (m_pointerLeft != null && m_remotePointerLeft)
            {
                m_remotePointerLeft.Color = m_pointerLeft.Color;
            }
            if (m_pointerRight != null && m_remotePointerRight)
            {
                m_remotePointerRight.Color = m_pointerRight.Color;
            }

            if (m_playerPointerLeft && m_avatarPointerLeft)
            {
                m_avatarPointerLeft.gameObject.SetActive(m_playerPointerLeft.gameObject.activeInHierarchy);
                m_avatarPointerLeft.transform.localScale = m_playerPointerLeft.transform.localScale;
            }
            else
            {
                m_pointerLeft = m_playerLeftHand.GetComponentInChildren<IPointer3D>();
                if (m_pointerLeft?.PointingLine)
                {
                    m_playerPointerLeft = m_pointerLeft?.PointingLine;
                }
            }

            if (m_playerPointerRight && m_avatarPointerRight)
            {
                m_avatarPointerRight.gameObject.SetActive(m_playerPointerRight.gameObject.activeInHierarchy);
                m_avatarPointerRight.transform.localScale = m_playerPointerRight.transform.localScale;
            }
            else
            {
                m_pointerRight = m_playerRightHand.GetComponentInChildren<IPointer3D>();
                if (m_pointerRight?.PointingLine)
                {
                    m_playerPointerRight = m_pointerRight.PointingLine;
                }
            }
        }

        public override void InitializeAvatar(string playerName, GameObject scenePlayer)
        {
            base.InitializeAvatar(playerName, scenePlayer);

            GameObject foundObj = WeavrElement.Find(scenePlayer, m_vrCameraName);
            m_playerVRCamera = foundObj ? foundObj.transform : null;
            foundObj = WeavrElement.Find(scenePlayer, m_handLeftName);
            m_playerLeftHand = foundObj ? foundObj.transform : null;
            if (!m_playerLeftHand) 
            {
                foundObj = WeavrElement.Find(m_playerVRCamera.parent.gameObject, m_handLeftName);
                m_playerLeftHand = foundObj ? foundObj.transform : null; 
            }
            if (m_playerLeftHand)
            {
                foundObj = WeavrElement.Find(m_playerLeftHand.gameObject, m_pointerName);
                m_pointerLeft = foundObj ? foundObj.GetComponent<IPointer3D>() : null;
                m_playerPointerLeft = m_pointerLeft?.PointingLine;
                if(m_playerPointerLeft && m_avatarHandLeft)
                {
                    m_avatarHandLeft.transform.localPosition = m_playerPointerLeft.localPosition;
                    m_avatarHandLeft.transform.localRotation = m_playerPointerLeft.localRotation;
                }
            }
            m_playerRightHand = WeavrElement.Find(scenePlayer, m_handRightName)?.transform;
            if (!m_playerRightHand) { m_playerRightHand = WeavrElement.Find(m_playerVRCamera.parent.gameObject, m_handRightName)?.transform; }
            if (m_playerRightHand)
            {
                m_pointerRight = WeavrElement.Find(m_playerRightHand.gameObject, m_pointerName)?.GetComponent<IPointer3D>();
                m_playerPointerRight = m_pointerRight?.PointingLine;
                if (m_playerPointerRight && m_avatarHandRight)
                {
                    m_avatarHandRight.transform.localPosition = m_playerPointerRight.localPosition;
                    m_avatarHandRight.transform.localRotation = m_playerPointerRight.localRotation;
                }
            }
        }

#endif
    }
}
