using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TXT.WEAVR.Interaction
{
#if WEAVR_VR
    [RequireComponent(typeof(Valve.VR.InteractionSystem.Hand))]
#endif
    [AddComponentMenu("WEAVR/VR/Interactions/Hand Pointer")]
    public class VR_HandPointer : WorldPointer, IPointer3D
    {
        [Space]
        public OptionalColor color;
        public GameObject holder;
        [Tooltip("How much thicker the final ray dot should be")]
        public float pointSizeFactor = 3;
        public bool addRigidBody = false;
        [Space]
        public VR_ControllerAction pointerShowButton;
        public VR_ControllerAction pointerDownButton;

        protected GameObject m_pointDot;

        public Color Color { 
            get => color.value;
            set => color.value = value; 
        }


        public Transform PointingLine { get; protected set; }

        public Transform PointingDot => m_pointDot ? m_pointDot.transform : null;

#if WEAVR_VR

        protected Valve.VR.InteractionSystem.Hand m_hand;

        protected override GameObject GetPointerObject(GameObject startupPointer)
        {
            m_hand = GetComponentInParent<Valve.VR.InteractionSystem.Hand>();

            pointerShowButton.Initialize(m_hand);
            pointerDownButton.Initialize(m_hand);

            holder = new GameObject();
            holder.transform.parent = this.transform;
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localRotation = Quaternion.identity;

            var pointerObject = startupPointer ? startupPointer : GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointerObject.transform.parent = holder.transform;
            pointerObject.transform.localScale = new Vector3(thickness, thickness, 100f);
            pointerObject.transform.localPosition = new Vector3(0f, 0f, 50f);
            pointerObject.transform.localRotation = Quaternion.identity;

            PointingLine = pointerObject.transform;

            BoxCollider collider = pointerObject.GetComponent<BoxCollider>();
            if (addRigidBody)
            {
                if (collider)
                {
                    collider.isTrigger = true;
                }
                Rigidbody rigidBody = pointerObject.AddComponent<Rigidbody>();
                rigidBody.isKinematic = true;
            }
            else
            {
                if (collider)
                {
                    Object.Destroy(collider);
                }
            }
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            newMaterial.SetColor("_Color", color);
            pointerObject.GetComponent<MeshRenderer>().material = newMaterial;

            m_pointDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_pointDot.transform.SetParent(holder.transform, false);
            m_pointDot.hideFlags = HideFlags.HideAndDontSave;
            m_pointDot.transform.localScale *= thickness * pointSizeFactor;
            Destroy(m_pointDot.GetComponent<Collider>());
            
            m_pointDot.GetComponent<MeshRenderer>().material = newMaterial;

            return pointerObject;
        }

        protected override void OnPointerDisable()
        {
            base.OnPointerDisable();
            m_pointDot.SetActive(false);
        }

        protected override void OnPointerEnable()
        {
            base.OnPointerEnable();
            m_pointDot.SetActive(true);
        }

        protected override void OnPointerUpdate(float pointerLength, float pointerThickness)
        {
            base.OnPointerUpdate(pointerLength, pointerThickness);
            m_pointDot.transform.localScale = Vector3.one * pointerThickness * pointSizeFactor;
            m_pointDot.transform.localPosition = Vector3.forward * pointerLength;
        }

        protected override Vector2 GetScrollDelta(GameObject target)
        {
            return TXT.WEAVR.Interaction.VR_ControllerManager.getTrackpadAxis(m_hand);
        }

        protected override bool IsValid()
        {
            return Core.WeavrManager.UseWorldPointer && m_hand != null;
        }

        public override bool GetPointerEnabled()
        {
            return m_hand.hoveringInteractable == null 
                && (m_hand.currentAttachedObject == null 
                    || m_hand.currentAttachedObject.GetComponent<InteractionController>() == null) 
                && pointerShowButton.IsTriggered();
        }

        public override bool GetPointerDown()
        {
            return pointerDownButton.IsTriggered();
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
