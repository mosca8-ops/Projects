using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.Interaction
{
    [AddComponentMenu("WEAVR/VR/Interactions/Pointer 3D")]
    public class VR_Pointer3D : WorldPointer, IPointer3D
    {
        [Space]
        public VR_ControllerAction.ActionType actionType = VR_ControllerAction.ActionType.TriggerPressed;
        [Space]
        public OptionalColor color;
        [Tooltip("How much thicker the final ray dot should be")]
        public float pointSizeFactor = 3;

        [Space]
        [SerializeField]
        protected Transform m_pointDot;
        [SerializeField]
        protected Material m_material;

        protected Transform m_pointLine;

        public Color Color { 
            get => color;
            set => color.value = value; 
        }

        public Transform PointingLine => m_pointLine;

        public Transform PointingDot => m_pointDot;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!m_material && pointer && !Application.isPlaying)
            {
                m_material = pointer.GetComponentInChildren<Renderer>(true).sharedMaterial;
            }
        }

#if WEAVR_VR
        protected Valve.VR.InteractionSystem.Hand m_lastHand;


        protected override GameObject GetPointerObject(GameObject startupPointer)
        {
            m_pointLine = startupPointer ? startupPointer.transform : GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            m_pointLine.SetParent(transform, false);
            m_pointLine.localScale = new Vector3(thickness, thickness, maxDistance);
            m_pointLine.localPosition = new Vector3(0f, 0f, maxDistance * 0.5f);
            m_pointLine.localRotation = Quaternion.identity;
            var collider = m_pointLine.GetComponent<Collider>();
            if (collider)
            {
                Destroy(collider);
            }
            var pointLineRenderer = m_pointLine.GetComponentInChildren<Renderer>(true);
            Material newMaterial = m_material ? m_material : pointLineRenderer ? pointLineRenderer.material : new Material(Shader.Find("Unlit/Color"));
            if (color.enabled)
            {
                newMaterial.SetColor("_Color", color);
            }
            else
            {
                color.value = newMaterial.color;
            }
            pointLineRenderer.material = newMaterial;

            m_pointDot = m_pointDot ? m_pointDot : GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            m_pointDot.transform.SetParent(transform, false);
            m_pointDot.gameObject.hideFlags = HideFlags.HideAndDontSave;
            m_pointDot.transform.localScale *= thickness * pointSizeFactor;
            if (m_pointDot.GetComponent<Collider>())
            {
                Destroy(m_pointDot.GetComponent<Collider>());
            }

            if (m_pointDot.GetComponent<Renderer>().material)
            {
                var dotColor = newMaterial.color;
                dotColor.a = m_pointDot.GetComponent<Renderer>().material.color.a;
                m_pointDot.GetComponent<Renderer>().material.SetColor("_Color", dotColor);
            }
            else 
            {
                m_pointDot.GetComponent<Renderer>().material = newMaterial;
            }

            return m_pointLine.gameObject;
        }

        protected override void OnPointerDisable()
        {
            base.OnPointerDisable();
            m_pointDot.gameObject.SetActive(false);
            m_lastHand = null;
        }

        protected override void OnPointerEnable()
        {
            base.OnPointerEnable();
            m_pointDot.gameObject.SetActive(true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InvokeDelayed(() =>  m_lastHand = GetComponentInParent<Valve.VR.InteractionSystem.Hand>(), 0.1f);
        }

        private bool Grabbable_IsHoveringOtherObjects()
        {
            throw new System.NotImplementedException();
        }

        protected void InvokeDelayed(System.Action action, float delay)
        {
            StartCoroutine(InvokeCoroutine(action, delay));
        }

        private IEnumerator InvokeCoroutine(System.Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action();
        }

        protected override void OnPointerUpdate(float pointerLength, float pointerThickness)
        {
            base.OnPointerUpdate(pointerLength, pointerThickness);
            m_pointDot.transform.localScale = Vector3.one * pointerThickness * pointSizeFactor;
            m_pointDot.transform.localPosition = Vector3.forward * pointerLength;
        }

        protected override Vector2 GetScrollDelta(GameObject target)
        {
            return VR_ControllerManager.getTrackpadAxis(m_lastHand);
        }

        protected override bool IsValid()
        {
            return Core.WeavrManager.UseWorldPointer && m_lastHand != null;
        }

        public override bool GetPointerEnabled()
        {
            return m_lastHand;
        }

        public override bool GetPointerDown()
        {
            return m_lastHand && VR_ControllerManager.getActionState(actionType, m_lastHand);
        }


#else
        public override bool GetPointerDown()
        {
            return false;
        }

        public override bool GetPointerEnabled()
        {
            return false;
        }

        protected override GameObject GetPointerObject(GameObject startupPointer)
        {
            return null;
        }

        protected override Vector2 GetScrollDelta(GameObject target)
        {
            return Vector2.zero;
        }

        protected override bool IsValid()
        {
            return false;
        }
#endif
    }
}
