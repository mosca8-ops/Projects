using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Interactions/Hand Hover Sphere")]
    public class VR_HandHoverSphere : MonoBehaviour
    {
        public Material highlightMaterial;

#if WEAVR_VR
        private Transform m_defaultSphereTransform;
        private float m_defaultRadius;
        private Hand m_hand;

        private MeshRenderer m_sphereRenderer;

        public bool IsHighlighted {
            get { return m_sphereRenderer != null && m_sphereRenderer.enabled; }
            set {
                if(m_sphereRenderer != null && m_sphereRenderer.enabled != value)
                {
                    m_sphereRenderer.enabled = value;
                }
            }
        }

        // Use this for initialization
        void Start()
        {
            m_hand = GetComponentInParent<Hand>();
            if(m_hand == null) { return; }
            m_defaultSphereTransform = m_hand.hoverSphereTransform;
            m_defaultRadius = m_hand.hoverSphereRadius;

            m_sphereRenderer = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshRenderer>();
            m_sphereRenderer.gameObject.name = "HoverSphere";
            Destroy(m_sphereRenderer.GetComponent<Collider>());
            //m_sphereRenderer.gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_sphereRenderer.transform.SetParent(transform, false);
            m_sphereRenderer.transform.localScale = Vector3.one * m_defaultRadius;
            m_sphereRenderer.material = highlightMaterial;
            m_sphereRenderer.enabled = false;
        }


        public void SetHoverSphere(Transform hoverSphere, float radius, bool highlight = false)
        {
            if (hoverSphere != null)
            {
                m_hand.hoverSphereTransform = hoverSphere;
                m_sphereRenderer.transform.localScale = Vector3.one * radius * 2;
                m_sphereRenderer.enabled = highlight;
                m_sphereRenderer.transform.SetPositionAndRotation(hoverSphere.position, hoverSphere.rotation);
            }
            m_hand.hoverSphereRadius = radius;
        }

        public void SetHoverSphere(Transform hoverSphere, bool highlight = false)
        {
            SetHoverSphere(hoverSphere, m_defaultRadius, highlight);
        }

        public void RemoveHoverSphere(Transform hoverSphere)
        {
            if(hoverSphere == m_hand.hoverSphereTransform)
            {
                ResetToDefault();
            }
        }

        public void ResetToDefault()
        {
            m_hand.hoverSphereTransform = m_defaultSphereTransform;
            m_hand.hoverSphereRadius = m_defaultRadius;

            m_sphereRenderer.enabled = false;
        }

        //// Update is called once per frame
        //void Update()
        //{
            
        //}
#endif
    }
}
