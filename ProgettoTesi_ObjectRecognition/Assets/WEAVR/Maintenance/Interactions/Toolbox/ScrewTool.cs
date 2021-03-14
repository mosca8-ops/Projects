namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

#if WEAVR_VR
    using Valve.VR.InteractionSystem;
#endif

    [AddComponentMenu("")]
    public class ScrewTool : AbstractScrewTool
    {

        protected override void Execute()
        {
#if WEAVR_VR
            m_operable.Controller.TemporaryMainBehaviour = m_operable;
#else
            if (m_operable != null)
            {
                m_operable.Interact(null);
            }
            else
            {
                ValueChangerMenu.Show(transform, true, "Screw Value", _currentScrewable.value,
                                            _currentScrewable.limits.min, _currentScrewable.limits.max,
                                            screwStep, UpdateScrewableValue);
                UpdateScrewableValue(_currentScrewable.value);
            }
#endif
        }

        protected override bool CanUpdate()
        {
#if WEAVR_VR
            //if (m_hoveringHand != null && _grabbable.Controller.TemporaryMainBehaviour != _grabbable && Vector3.Distance(m_hoveringHand.hoverSphereTransform.transform.position, transform.position) > 0.3f)
            //{
            //    //_grabbable.Controller.TemporaryMainBehaviour = m_operable;
            //    _grabbable.Controller.TemporaryMainBehaviour = null;
            //}
            if (GetComponent<Interaction.VR_Rotator>() != null)
            {
                return false;
            }
#endif
            return true;
        }

#if WEAVR_VR

        protected Hand m_hoveringHand;

        //-------------------------------------------------
        // Called when a Hand starts hovering over this object
        //-------------------------------------------------
        private void OnHandHoverBegin(Hand hand)
        {
            m_hoveringHand = hand;
        }


        //-------------------------------------------------
        // Called when a Hand stops hovering over this object
        //-------------------------------------------------
        private void OnHandHoverEnd(Hand hand)
        {
            m_hoveringHand = null;
            if (!_grabbable.IsGrabbed && _grabbable.Controller.TemporaryMainBehaviour != _grabbable && Vector3.Distance(hand.hoverSphereTransform.position, transform.position) > 0.3)
            {
                _grabbable.Controller.TemporaryMainBehaviour = null;
                _grabbable.InteractVR(null, hand);
            }
        }


        ////-------------------------------------------------
        //// Called every Update() while a Hand is hovering over this object
        ////-------------------------------------------------
        //private void HandHoverUpdate(Hand hand)
        //{

        //}
#endif
    }
}