using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Common
{
    [AddComponentMenu("WEAVR/Interactions/Doors/Slide Door")]
    [RequireComponent(typeof(InteractionController))]
    public class SlideDoor : AbstractSlideDoor, IVR_Poser
#if WEAVR_VR
        , IVR_Attachable
#endif
    {
        public override bool CanBeDefault => true;

#if WEAVR_VR
         
        private Hand m_lockedHand;
        public VR_Skeleton_Poser m_SkeletonPoser;
        private VR_Object m_VRObject;

        protected override void Start()
        {
            base.Start();
            m_VRObject = transform.GetComponent<VR_Object>();
            if (m_SkeletonPoser == null)
            {
                m_SkeletonPoser = transform.GetComponent<VR_Skeleton_Poser>();
            }
            else if (m_VRObject != null)
            {
                m_VRObject.skeletonPoser = m_SkeletonPoser;
            }
            
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            StopDoorMove();
            StartSliding(hand);
        }

        protected override void StopInteractionVR(AbstractInteractiveBehaviour nextBehaviour)
        {
            base.StopInteractionVR(nextBehaviour);
            if(m_lockedHand != null)
            {
                StopSliding(m_lockedHand);
            }
        }

        private void HandHoverUpdate(Hand hand)
        {
            if(!m_isValid || hand != m_lockedHand)
            {
                return;
            }

            if (!enabled || !gameObject.activeInHierarchy)
            {
                m_mappingChangeRate = 0;
                StopSliding(hand);
                return;
            }

            if (hand.GetGrabEnding() != GrabTypes.None)
            {
                StopSliding(hand);
            }

            if (hand == m_lockedHand)
            {

                UpdateLinearMapping(hand.transform);
            }
        }

        private void StopSliding(Hand hand)
        {
            RegisterAction(DoorAction.EndInteraction);
            if (hand != null)
            {
                hand.HoverUnlock(GetComponent<Interactable>());
                hand.DetachObject(gameObject);
                if (hand.GetType() == typeof(VR_Hand))
                {
                    VR_Hand wHand = (VR_Hand)hand;
                    wHand.StopAttachmentPointOverride();
                    //wHand.RestoreControllerPose();
                }
            }
            if(m_lockedHand != null && m_lockedHand != hand)
            {
                m_lockedHand.HoverUnlock(GetComponent<Interactable>());
            }
            m_lockedHand = null;

            CalculateMappingChangeRate();
        }

        private void StartSliding(object hand)
        {
            var handVR = hand as Hand;
            if (handVR != null)
            {
                RegisterAction(DoorAction.StartInteraction);
                handVR.HoverLock(GetComponent<Interactable>());
                if (handVR.GetType() == typeof(VR_Hand) && m_SkeletonPoser != null)
                {
                    VR_Hand wHand = (VR_Hand)hand;
                    wHand.StartAttachmentPointOverride(m_SkeletonPoser.GetAttachmentPoint(handVR.handType, false), false);
                    //wHand.PrepareInteractionPose(m_SkeletonPoser);
                }
                m_VRObject?.SetupHandToObjectInteraction(m_SkeletonPoser != null);
                handVR.AttachObject(gameObject, handVR.GetGrabStarting(), Hand.AttachmentFlags.DetachOthers);
                m_lockedHand = handVR;

                m_initialMappingOffset = m_currentOpening - CalculateLinearMapping(handVR.transform);
                m_sampleCount = 0;
                m_mappingChangeRate = 0.0f;

                HideHints(handVR);
            }
        }

        private void HideHints(Hand hand)
        {
            ControllerButtonHints.HideButtonHint(hand, hand.uiInteractAction);
            ControllerButtonHints.HideTextHint(hand, hand.uiInteractAction);
            ControllerButtonHints.HideTextHint(hand, hand.grabGripAction);
            ControllerButtonHints.HideButtonHint(hand, hand.grabGripAction);
            ControllerButtonHints.HideTextHint(hand, hand.grabPinchAction);
            ControllerButtonHints.HideButtonHint(hand, hand.grabPinchAction);
        }


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
#endif

    }
}
