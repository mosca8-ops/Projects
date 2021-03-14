using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.UI;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Interactions/Basic/Operable", 0)]
    [RequireComponent(typeof(InteractionController))]
    public class Operable : AbstractOperable
    {


        public override void Interact(ObjectsBag currentBag)
        {
#if WEAVR_VR
            if (m_hoveringHand != null && CanInteractVR(currentBag, m_hoveringHand))
            {
                InteractVR(currentBag, m_hoveringHand);
            }
            else
            {
#endif
                ShowPopup();
#if WEAVR_VR
            }
#endif
        }

        protected override void OnDisableInteractionInternal()
        {
#if WEAVR_VR
            DisableOperableVR(m_hoveringHand);
#endif
        }

#if WEAVR_VR

        private bool m_wasTouching;
        private Vector2 m_lastAxisValue;
        private Hand m_hoveringHand;

        protected List<VR_Manipulator> m_manipulators = new List<VR_Manipulator>();

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return (m_compatibleTools.Length == 0 && !bag.GetSelected(hand)) || m_compatibleTools.Contains(bag.GetSelected(hand));
        }

        public override void InteractVR(ObjectsBag currentBag, object handObject)
        {
            base.InteractVR(currentBag, handObject);

            var hand = handObject as Hand;

            if (!ValueIndicator)
            {
                hand.GetComponent<VR_ValueMenu>()?.Show(property, () => m_value.ToString("n2"), () => IsValidValue ? validColor : notValidColor);
            }

            //hand.GetComponent<VR_HandHoverSphere>()?.SetHoverSphere(null, 0.1f);

            m_manipulators.Clear();

            var interactable = GetComponent<Interactable>();

            foreach (var manipulator in GetComponents<VR_Manipulator>())
            {
                if (manipulator.enabled && manipulator.CanHandleData(Value))
                {
                    m_manipulators.Add(manipulator);
                    manipulator.StartManipulating(hand, interactable, IsKeepPressedLogic(), () => Value, v => Value = v, limits);
                }
            }
        }

        //-------------------------------------------------
        // Called when a Hand starts hovering over this object
        //-------------------------------------------------
        private void OnHandHoverBegin(Hand hand)
        {
            if (!Controller.enabled) { return; }
            m_hoveringHand = hand;
        }

        //-------------------------------------------------
        // Called every Update() while a Hand is hovering over this object
        //-------------------------------------------------
        private void HandHoverUpdate(Hand hand)
        {
            if (m_manipulators.Count > 0) { return; } // Let manipulators handle the event

            //VR_ValueMenu valueMenu = hand.GetComponent<VR_ValueMenu>();
            //if (!valueMenu || !valueMenu.IsVisible) { return; }
            //bool isCurrentlyTouching = VR_ControllerManager.isTrackpadTouched(hand);
            //if (isCurrentlyTouching)
            //{
            //    if (!m_wasTouching)
            //    {

            //        m_lastAxisValue = VR_ControllerManager.getTrackpadAxis(hand);
            //    }
            //    else
            //    {
            //        var currentAxisValue = VR_ControllerManager.getTrackpadAxis(hand);
            //        var diff = currentAxisValue - m_lastAxisValue;
            //        var angle = -Vector2.SignedAngle(m_lastAxisValue, currentAxisValue);
            //        switch (m_swipeDirection)
            //        {
            //            case SwipeDirection.Horizontal:
            //                if (Mathf.Abs(diff.x) > 0.1f)
            //                {
            //                    Value += valueStep * Discretize(diff.x, 0.1f);
            //                    m_lastAxisValue = currentAxisValue;
            //                }
            //                break;
            //            case SwipeDirection.Vertical:
            //                if (Mathf.Abs(diff.y) > 0.1f)
            //                {
            //                    Value += valueStep * Discretize(diff.y, 0.1f);
            //                    m_lastAxisValue = currentAxisValue;
            //                }
            //                break;
            //            case SwipeDirection.Circular:
            //                if (Mathf.Abs(angle) > 5)
            //                {
            //                    Value += valueStep * Discretize(angle, 5f);
            //                    m_lastAxisValue = currentAxisValue;
            //                }
            //                break;
            //        }
            //    }
            //}
            //m_wasTouching = isCurrentlyTouching;
        }


        //-------------------------------------------------
        // Called when a Hand stops hovering over this object
        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            if (!Controller.enabled) { return; }
            //ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
            DisableOperableVR(hand);
        }

        private void DisableOperableVR(Hand hand)
        {
            if (hand != null)
            {
                hand.GetComponent<VR_ValueMenu>()?.Hide();
                hand.GetComponent<VR_HandHoverSphere>()?.RemoveHoverSphere(null);

                m_manipulators.Clear();

                m_hoveringHand = null;
            }
        }
#endif
    }
}