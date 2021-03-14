using UnityEngine;

namespace TXT.WEAVR.Interaction
{
    [ExecuteInEditMode]
    [AddComponentMenu("")]
    public class VR_Skeleton_PoserUpdater : MonoBehaviour
    {
        private Transform m_ReferenceTransform = null;
        private bool m_IsInverseTransform = false;

        public void setReferenceTransform(Transform iTransform, bool iIsInverse)
        {
            m_ReferenceTransform = iTransform;
            m_IsInverseTransform = iIsInverse;
        }
        

        public void UpdateTransform()
        {
#if UNITY_EDITOR
            if (m_ReferenceTransform != null)
            {
                if (m_IsInverseTransform)
                {
                    m_ReferenceTransform.position = transform.parent.position;
                    m_ReferenceTransform.rotation = transform.parent.rotation;
                    transform.localPosition = m_ReferenceTransform.localPosition;
                    transform.localRotation = m_ReferenceTransform.localRotation;
                }
                else
                {
                    m_ReferenceTransform.localPosition = transform.parent.transform.parent.InverseTransformPoint(transform.position);
                    m_ReferenceTransform.localRotation = transform.localRotation;
                }
                //transform.localScale = m_ReferenceTransform.localScale;
            }
#endif
        }
        public virtual void Awake()
        {
            UpdateTransform();
        }

        public virtual void OnEnable()
        {
            UpdateTransform();
        }

        public virtual void Update()
        {
            UpdateTransform();
        }
    };
}
