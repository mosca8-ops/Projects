using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Interactions/Controls/3-Way Switch")]
    [RequireComponent(typeof(InteractionController))]
    public class ThreeWaySwitch : AbstractThreeWaySwitch
#if WEAVR_VR
        , IVR_Poser, IVR_Attachable
#endif
    {
        [Space]
        [SerializeField]
        private bool m_hoverLock;
#if WEAVR_VR

        public bool HasRotationAxis()
        {
            return false;
        }

        public bool IsHandParent()
        {
            return false;
        }

        public VR_Skeleton_Poser GetSkeletonPoser()
        {
            return m_SkeletonPoser;
        }

        public SteamVR_Skeleton_JointIndexEnum GetFingerHoverIndex()
        {
            return SteamVR_Skeleton_JointIndexEnum.indexTip;
        }

        public VR_Object.HoveringMode GetHoveringMode()
        {
            return VR_Object.HoveringMode.Any;
        }

        public Transform GetAttachmentPoint(Hand iHand)
        {
            return m_SkeletonPoser?.GetAttachmentPoint(iHand.handType, HasRotationAxis());
        }

        protected List<VR_Manipulator> m_manipulators = new List<VR_Manipulator>();
        private bool m_isStillActive;

        private float m_statesDistance;
        private float m_statesAngle;
        private VR_Object m_VRObject;
        private float m_value;
        private VR_Skeleton_Poser m_SkeletonPoser;

        private float Value {
            get { return 1f - (float)m_currentState * 0.5f; }
            set {
                float clampedValue = Mathf.Clamp01(value);
                transform.localPosition = Vector3.MoveTowards(m_down.LocalPosition, m_up.LocalPosition, clampedValue * m_statesDistance);
                transform.localRotation = Quaternion.RotateTowards(m_down.LocalRotation, m_up.LocalRotation, clampedValue * m_statesAngle);
                if (clampedValue < 0.25f)
                {
                    //SetState(Switch3WayState.Down);
                    //Debug.Log($"State: {Switch3WayState.Down} -> {clampedValue}");
                    SetManipulatorStep(Switch3WayState.Down);
                }
                else if (clampedValue > 0.75f)
                {
                    //SetState(Switch3WayState.Up);
                    //Debug.Log($"State: {Switch3WayState.Up} -> {clampedValue}");
                    SetManipulatorStep(Switch3WayState.Up);
                }
                else
                {
                    //SetState(Switch3WayState.Middle);
                    //Debug.Log($"State: {Switch3WayState.Middle} -> {clampedValue}");
                    SetManipulatorStep(Switch3WayState.Middle);
                }
            }
        }

        private void SetManipulatorStep(Switch3WayState state)
        {
            SetState(state);
            //ChangeStateTo(state, m_transitionTime);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (m_up.isContinuous || m_down.isContinuous)
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
            if (m_up.isContinuous || m_down.isContinuous)
            {
                InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
            }

            m_statesDistance = Vector3.Distance(m_down.LocalPosition, m_up.LocalPosition);
            m_statesAngle = Quaternion.Angle(m_down.LocalRotation, m_up.LocalRotation);

            m_VRObject = GetComponent<VR_Object>();
            if (m_SkeletonPoser == null)
            {
                m_SkeletonPoser = transform.GetComponent<VR_Skeleton_Poser>();
            }
            else if (m_VRObject != null)
            {
                m_VRObject.skeletonPoser = m_SkeletonPoser;
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
            if (m_hoverLock && hand is Hand)
            {
                ((Hand)hand).HoverLock(GetComponent<VR_Object>());
            }

            m_manipulators.Clear();

            var interactable = GetComponent<Interactable>();

            foreach (var manipulator in GetComponents<VR_Manipulator>())
            {
                if (manipulator.enabled && manipulator.CanHandleData(Value))
                {
                    m_manipulators.Add(manipulator);
                    manipulator.StartManipulating(hand as Hand, interactable, IsKeepPressedLogic(), () => Value, v => Value = v);
                }
            }

            if (m_manipulators.Count == 0)
            {
                Interact(currentBag);
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            foreach (var manipulator in GetComponents<VR_Manipulator>())
            {
                if (manipulator.enabled && manipulator.CanHandleData(Value))
                {
                    return "Switch";
                }
            }
            return base.GetInteractionName(currentBag);
        }

        protected override void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteractionVR(nextBehaviour);
            m_isStillActive = false;
            if (!GetSwitchState(m_currentState).isStable)
            {
                //SetState(Switch3WayState.Middle);
                Value = 0.5f;
            }
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
