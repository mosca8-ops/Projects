namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

#if WEAVR_VR
    using Valve.VR.InteractionSystem;
#endif

    [AddComponentMenu("WEAVR/Interactions/Basic/Connectable", 0)]
    [RequireComponent(typeof(InteractionController))]
    public class Connectable : AbstractConnectable
    {
        protected override void IsDisconnectedInternal()
        {
#if !WEAVR_VR
            if (m_rigidBody != null && activeConnector)
            {
                m_rigidBody.isKinematic = m_wasKinematic;
            }
#endif
        }

#if WEAVR_VR

        //protected bool m_otherConnectableInHand;
        protected VR_HandHoverSphere m_handHover;
        public Transform m_handHoverPoint = null;

        public override bool UseStandardVRInteraction(ObjectsBag bag, object hand)
        {
            return bag.Selected != null;
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return bag.Selected != null || bag.GetSelected(hand) != null;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            base.InteractVR(currentBag, hand);
            Interact(currentBag);
        }

        //protected virtual void OnTriggerEnter(Collider other)
        //{
        //    var interractionController = other.GetComponent<InteractionController>();
        //    if(interractionController == null) { return; }

        //    if (interractionController.Has<Grabbable>())
        //    {

        //    }
        //}

        //-------------------------------------------------
        // Called when a Hand starts hovering over this object
        //-------------------------------------------------
        //private void OnHandHoverBegin(Hand hand)
        //{
        //    if (!Controller.enabled) { return; }
        //    //ControllerButtonHints.ShowButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //    //ControllerButtonHints.ShowTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger, "Grab");

        //    //var interactionController = hand.currentAttachedObject.GetComponent<InteractionController>();
        //    //if (interactionController == null) { return; }

        //    if (CanConnect(hand.currentAttachedObject))
        //    {
        //        ControllerButtonHints.HideTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //        ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);

        //        //Debug.Log("Showing Connectable Hints");

        //        if (WeavrManager.ShowHints)
        //        {
        //            ControllerButtonHints.ShowTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger, "Connect", true);
        //        }
        //        else
        //        {
        //            ControllerButtonHints.ShowButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //        }
        //    }
        //}


        //-------------------------------------------------
        // Called when a Hand stops hovering over this object
        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            if (!Controller.enabled) { return; }
            //ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
            //var interactionController = hand.currentAttachedObject.GetComponent<InteractionController>();
            //if (interactionController == null) { return; }

            if (hand.currentAttachedObject && CanConnect(hand.currentAttachedObject.gameObject))
            {
                ControllerButtonHints.HideTextHint(hand, hand.grabGripAction);
                ControllerButtonHints.HideButtonHint(hand, hand.grabGripAction);

                //Debug.Log("Hiding connectable hints");
            }
        }


        //-------------------------------------------------
        // Called every Update() while a Hand is hovering over this object
        //-------------------------------------------------
        //private void HandHoverUpdate(Hand hand)
        //{
        //    if (!Controller.enabled) { return; }
        //    if (hand.GetStandardInteractionButtonUp() || ((hand.controller != null) && hand.controller.GetPressUp(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger)))
        //    {
        //        var interactionController = hand.currentAttachedObject.GetComponent<AbstractInteractionController>();
        //        if (interactionController == null) { return; }

        //        if (interactionController.Has<AbstractConnectable>(false) && CanConnect(interactionController.GetComponent<AbstractConnectable>()))
        //        {
        //            Connect(interactionController.GetComponent<AbstractConnectable>());
        //            ControllerButtonHints.HideTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //            ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //        }
        //        else if (CanConnect(interactionController.GetComponent<AbstractInteractiveBehaviour>()))
        //        {
        //            Connect(interactionController.GetComponent<AbstractInteractiveBehaviour>());
        //            ControllerButtonHints.HideTextHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //            ControllerButtonHints.HideButtonHint(hand, Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
        //        }

        //    }
        //}

        private void OnAttachedToHand(Hand hand)
        {
            if (!Controller.enabled) { return; }
            m_handHover = null;
            if (m_connectionPoint != null)
            {
                m_handHover = hand.GetComponentInChildren<VR_HandHoverSphere>();
                m_handHover?.SetHoverSphere(m_handHoverPoint, false);
            }
        }

        ////-------------------------------------------------
        //// Called every Update() while this GameObject is attached to the hand
        ////-------------------------------------------------
        //private void HandAttachedUpdate(Hand hand)
        //{
        //    if (!Controller.enabled) { return; }
        //    if (connectionPoint == null || m_handHover == null) { return; }
        //    m_handHover.IsHighlighted = hand.hoveringInteractable != null && CanConnect(hand.hoveringInteractable.gameObject);
        //}

        //-------------------------------------------------
        // Called when this GameObject is detached from the hand
        //-------------------------------------------------
        private void OnDetachedFromHand(Hand hand)
        {
            m_handHover?.RemoveHoverSphere(m_handHoverPoint);
            if (!Controller.enabled) { return; }
            m_handHover = null;
        }

        protected override void Start()
        {
            base.Start();
            if (m_handHoverPoint == null)
            {
                m_handHoverPoint = m_connectionPoint;
            }
        }

#endif
    }
}