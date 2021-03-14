using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Interactions/Doors/Door Lock")]
    [RequireComponent(typeof(InteractionController))]
    public class DoorLock : AbstractDoorLock
    {
        [SerializeField]
        [Range(0.01f, 0.49f)]
        protected float m_lockThreshold = 0.1f;

#if WEAVR_VR
        protected List<VR_Manipulator> m_manipulators = new List<VR_Manipulator>();
        protected Hand m_hoveringHand;

        private float m_value;
        private float Value {
            get { return m_value; }
            set {
                float clampedValue = Mathf.Clamp01(value);
                if (clampedValue < m_lockThreshold && !IsLocked)
                {
                    IsLocked = true;
                    StopManipulating(m_hoveringHand, true);
                }
                else if (clampedValue > (1 - m_lockThreshold) && IsLocked)
                {
                    IsLocked = false;
                    StopManipulating(m_hoveringHand, true);
                }
                else
                {
                    m_value = clampedValue;
                }
            }
        }

        public override bool IsLocked {
            get {
                return base.IsLocked;
            }

            set {
                base.IsLocked = value;
                m_value = IsLocked ? 0 : 1;
            }
        }

        protected override void Start()
        {
            base.Start();
            m_value = m_isLocked ? 0 : 1;

            foreach (var manipulator in GetComponents<VR_Manipulator>())
            {
                if (manipulator.enabled && manipulator.CanHandleData(Value))
                {
                    manipulator.SetInitialValue(m_value);
                }
            }
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return (m_keys.Count == 0 && bag?.Selected == null && bag.GetSelected(hand) == null)
                || m_keys.Contains(bag?.Selected) || m_keys.Contains(bag.GetSelected(hand));
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {

            if (IsKeepPressedLogic())
            {
                StartInteraction((Hand)hand, currentBag);
            }
            else
            {
                StartCoroutine(LateStartInteraction((Hand)hand, currentBag));
            }

        }

        private void StartInteraction(Hand hand, ObjectsBag currentBag)
        {
            m_manipulators.Clear();
            var interactable = GetComponent<Interactable>();
            foreach (var manipulator in GetComponents<VR_Manipulator>())
            {
                if (manipulator.enabled && manipulator.CanHandleData(Value))
                {
                    m_manipulators.Add(manipulator);
                }
            }
            if (m_manipulators.Count == 0)
            {
                Interact(currentBag);
            }
            else
            {
                if (m_useAnimator && m_animator != null)
                {
                    m_animator.enabled = false;
                }
                for (int i = 0; i < m_manipulators.Count; i++)
                {
                    m_manipulators[i].StartManipulating((Hand)hand, interactable, IsKeepPressedLogic(), () => Value, v => Value = v);
                }
                if (hand is Hand)
                {
                    //((Hand)hand).HoverLock(GetComponent<VR_Object>());
                    m_hoveringHand = hand as Hand;
                }

            }
        }

        private System.Collections.IEnumerator LateStartInteraction(Hand hand, ObjectsBag currentBag)
        {
            yield return new WaitForEndOfFrame();
            StartInteraction(hand, currentBag);
            StopAllCoroutines();
        }

        private void HandHoverUpdate(Hand hand)
        {
            if (m_manipulators.Count > 0)
            {
                if (IsKeepPressedLogic())
                {
                    if (!VR_ControllerManager.GetStandardInteractionButton(hand))
                    {
                        StopManipulating(hand);
                    }
                }
                else
                {
                    if (VR_ControllerManager.GetStandardInteractionButtonUp(hand))
                    {
                        StopManipulating(hand);
                    }
                }
            } 
        }

        private void OnDisable()
        {
            StopManipulating(m_hoveringHand, true);
        }

        private void StopManipulating(Hand hand, bool reenableAnimator = false)
        {
            StopInteractionVR(null);
            if (m_manipulators.Count > 0 && hand != null)
            {
                hand.HoverUnlock(GetComponent<VR_Object>());
                hand = null;
                m_manipulators.Clear();
                if (m_useAnimator && reenableAnimator && m_animator != null)
                {
                    m_animator.enabled = true;
                }
            }
        }
#endif
    }
}
