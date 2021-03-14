using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Interactions/Controls/N-Way Switch")]
    [RequireComponent(typeof(InteractionController))]
    public class NWaySwitch : AbstractNWaySwitch
    {
        [Space]
        [SerializeField]
        private bool m_hoverLock;
#if WEAVR_VR

        private bool m_isStillActive;

        protected override void OnValidate()
        {
            base.OnValidate();
            for (int i = 0; i < m_states.Count; i++)
            {
                if (m_states[i].isContinuous)
                {
                    InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
                    break;
                }
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
            for (int i = 0; i < m_states.Count; i++)
            {
                if (m_states[i].isContinuous)
                {
                    InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
                    break;
                }
            }
        }

        public override bool CanBeDefault => true;

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
            if (m_hoverLock && hand is Hand)
            {
                ((Hand)hand).HoverLock(GetComponent<VR_Object>());
            }
        }

        protected override void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteractionVR(nextBehaviour);
            m_isStillActive = false;
        }

        private void HandHoverUpdate(Hand hand)
        {
            if (m_isStillActive && !VR_ControllerManager.GetStandardInteractionButton(hand))
            {
                m_isStillActive = false;
                if (m_hoverLock)
                {
                    hand.HoverUnlock(GetComponent<VR_Object>());
                }
            }
        }

        private void OnHandHoverEnd(Hand hand)
        {
            StopInteractionVR(null);
        }

#endif

    }
}
