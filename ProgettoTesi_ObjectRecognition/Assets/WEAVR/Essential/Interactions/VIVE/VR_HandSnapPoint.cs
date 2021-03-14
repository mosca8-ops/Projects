using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/VR/Interactions/Hand Snap Point")]
    public class VR_HandSnapPoint : MonoBehaviour
    {
        
#if WEAVR_VR
        
        private Hand m_hand;
        private Transform m_bridgeParent;

        // Use this for initialization
        void Start()
        {
            m_hand = GetComponentInParent<Hand>();
        }

        public virtual void Snap(Transform toSnap, bool moveParent)
        {
            ProjectSnapTransform(toSnap, moveParent ? toSnap.parent : toSnap);
        }

        public virtual void ProjectSnapTransform(Transform snapPoint, Transform transformToProject)
        {
            if (m_bridgeParent == null)
            {
                m_bridgeParent = new GameObject("SnapPointBridgeParent").transform;
                m_bridgeParent.SetParent(transform, false);
                m_bridgeParent.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            m_bridgeParent.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
            var prevParent = transformToProject.parent;
            transformToProject.SetParent(m_bridgeParent, true);
            m_bridgeParent.localPosition = Vector3.zero;
            m_bridgeParent.localRotation = Quaternion.identity;
            transformToProject.SetParent(prevParent, true);
        }
#endif
    }
}