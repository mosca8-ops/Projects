using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Interactions/Controls/Push Button")]
    [RequireComponent(typeof(InteractionController))]
    public class PushButton : AbstractPushButton
    {


#if WEAVR_VR

        private Hand m_lockedHand;

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_isContinuous)
            {
                InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
            }
        }

        protected override void Reset()
        {
            base.Reset();
            Controller.DefaultBehaviour = this;
        }

        protected override void Start()
        {
            base.Start();
            m_isStillActive = false;
            if (m_isContinuous)
            {
                InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
            }
        }

        protected override bool IsInteractionStillActive()
        {
            return m_isStillActive;
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return bag.Selected == null && bag.GetSelected(hand) == null;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            m_isStillActive = true;
            Interact(currentBag);
            if(m_isContinuous && m_hoverLock)
            {
                if (m_lockedHand)
                {
                    m_lockedHand.HoverUnlock(GetComponent<Interactable>());
                }
                if(hand is Hand h)
                {
                    h.HoverLock(GetComponent<Interactable>());
                    m_lockedHand = h;
                }
            }
        }

        protected override void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteractionVR(nextBehaviour);
            m_isStillActive = false;
            UnlockHover();
        }

        private void HandHoverUpdate(Hand hand)
        {
            if (m_isStillActive && !VR_ControllerManager.GetStandardInteractionButton(hand))
            {
                m_isStillActive = false;
                UnlockHover();
            }
        }

        private void UnlockHover()
        {
            if (m_lockedHand)
            {
                m_lockedHand.HoverUnlock(GetComponent<Interactable>());
                m_lockedHand = null;
            }
        }

        private void OnHandHoverEnd(Hand hand)
        {
            StopInteractionVR(null);
        }

#endif
    }

}
