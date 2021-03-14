using System;
using TXT.WEAVR.Common;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{
    public abstract class VR_Manipulator : MonoBehaviour
    {
        [HideInInspector]
        public bool alwaysOn = false;

        protected bool m_isActive;

        public bool IsActive {
            get { return m_isActive; }
        }

#if WEAVR_VR

        public abstract bool CanHandleData(object value);

        public abstract void UpdateValue(float value);

        protected Hand m_lastHandUsed;
        protected Interactable m_lastLockedInteractable;

        protected Action<object> m_genericSetter;
        protected Func<object> m_genericGetter;

        protected Action<float> m_floatSetter;
        protected Func<float> m_floatGetter;

        protected bool m_isKeepPressedLogic = true;



        public virtual void StartManipulating(Hand hand, Interactable interactable, bool iIsKeepPressedLogic, Func<float> getter, Action<float> setter, Span? span = null)
        {
            m_genericGetter = null;
            m_genericSetter = null;

            m_floatGetter = getter;
            m_floatSetter = setter;
            m_isKeepPressedLogic = iIsKeepPressedLogic;
            BeginManipulating(hand, interactable);
        }

        public virtual void ResetManipulator(Hand hand, Interactable interactable, float value, Span? span = null)
        {

        }

        protected virtual void BeginManipulating(Hand hand, Interactable interactable)
        {
            if (m_isKeepPressedLogic && VR_ControllerManager.GetStandardInteractionButtonDown(hand))
            {
                hand.HoverLock(interactable);
            }
            m_lastLockedInteractable = interactable;
            m_isActive = true;
        }

        public virtual void SetInitialValue(float value)
        {

        }

        protected abstract void UpdateManipulator(Hand hand, Interactable interactable);

        //-------------------------------------------------
        // Called every Update() while a Hand is hovering over this object
        //-------------------------------------------------
        private void HandHoverUpdate(Hand hand)
        {
            if (m_isKeepPressedLogic)
            {
                if (alwaysOn && VR_ControllerManager.GetStandardInteractionButtonDown(hand))
                {
                    BeginManipulating(hand, m_lastLockedInteractable);
                }

                if (VR_ControllerManager.GetStandardInteractionButtonUp(hand))
                {
                    StopManipulating(hand, m_lastLockedInteractable);
                }

                if (m_isActive)
                {
                    if (VR_ControllerManager.GetStandardInteractionButton(hand))
                    {
                        UpdateManipulator(hand, m_lastLockedInteractable);
                    }
                }
            }
            else
            {
                if (m_isActive)
                {
                    if (VR_ControllerManager.GetStandardInteractionButtonUp(hand))
                    {
                        StopManipulating(hand, m_lastLockedInteractable);
                    }
                    else
                    {
                        UpdateManipulator(hand, m_lastLockedInteractable);
                    }
                }
                else if(alwaysOn && VR_ControllerManager.GetStandardInteractionButtonUp(hand))
                {
                     BeginManipulating(hand, m_lastLockedInteractable);
                }
            }
        }


        //-------------------------------------------------
        // Called when a Hand stops hovering over this object
        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            StopManipulating(hand, m_lastLockedInteractable);
        }

        public virtual void StopManipulating(Hand hand, Interactable interactable)
        {
            m_isActive = false;
            hand?.HoverUnlock(interactable);
        }

        private void OnDisable()
        {
            StopManipulating(m_lastHandUsed, m_lastLockedInteractable);
        }
#endif
    }
}
