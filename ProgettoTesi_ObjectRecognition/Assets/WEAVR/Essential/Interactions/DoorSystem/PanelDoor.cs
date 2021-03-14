using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Interactions/Doors/Panel Door")]
    [RequireComponent(typeof(InteractionController))]
    public class PanelDoor : AbstractPanelDoor
    {

#if WEAVR_VR

        private Valve.VR.ISteamVR_Action_In_Source m_trigger = Valve.VR.SteamVR_Input.GetAction<Valve.VR.SteamVR_Action_Boolean>("GrabGrip");

        private bool m_hintsAreShown;
        private InteractionController m_vrController;
        private InteractionController VRController => m_vrController;

        protected override void Start()
        {
            base.Start();
            m_vrController = Controller as InteractionController;
        }

        protected override void UpdateState()
        {
            if (VRController.AttachedHand != null)
            {
                CanCloseWhileInHand(VRController.AttachedHand);
            }
            else
            {
                base.UpdateState();
            }
        }

        private bool CanCloseWhileInHand(Hand hand)
        {
            bool canClose = Vector3.Distance(transform.localPosition, m_closedLocalPosition) < m_actualCloseThreshold;
            if (canClose && !ControllerButtonHints.IsButtonHintActive(hand, m_trigger))
            {
                m_hintsAreShown = true;
                ControllerButtonHints.ShowTextHint(hand, m_trigger, "Close");

                return true;
            }
            else if (!canClose && m_hintsAreShown)
            {
                ControllerButtonHints.HideTextHint(hand, m_trigger);
                ControllerButtonHints.HideButtonHint(hand, m_trigger);

                m_hintsAreShown = false;
            }
            return false;
        }



        //-------------------------------------------------
        // Called when this GameObject becomes attached to the hand
        //-------------------------------------------------
        private void OnAttachedToHand(Hand hand)
        {
            IsClosed = false;
        }


        //-------------------------------------------------
        // Called when this GameObject is detached from the hand
        //-------------------------------------------------
        private void OnDetachedFromHand(Hand hand)
        {
            if (CanCloseWhileInHand(hand))
            {
                base.UpdateState();
            }
        }


        ////-------------------------------------------------
        //// Called every Update() while this GameObject is attached to the hand
        ////-------------------------------------------------
        //private void HandAttachedUpdate(Hand hand)
        //{
        //    //textMesh.text = "Attached to hand: " + hand.name + "\nAttached time: " + (Time.time - attachTime).ToString("F2");
        //}
#endif

        // ADD HERE VR INTERACTIONS...


    }
}
