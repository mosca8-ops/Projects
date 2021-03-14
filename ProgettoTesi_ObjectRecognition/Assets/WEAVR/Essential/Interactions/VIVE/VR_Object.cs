using TXT.WEAVR.Core;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
using BaseClass = Valve.VR.InteractionSystem.Interactable;
#else
using BaseClass = UnityEngine.MonoBehaviour;
#endif

namespace TXT.WEAVR.Interaction
{
    [ExecuteInEditMode]
    [DoNotExpose]
    [Stateless]
    [AddComponentMenu("")]
    public partial class VR_Object : BaseClass
    {

        public enum HoveringMode
        {
            Any,
            Finger
        };

        public enum InteractionMode
        {
            GlueToObject,
            BlendToFinalPose,
            None
        }

        public InteractionMode m_interactionMode = InteractionMode.BlendToFinalPose;
        [HideInInspector]
        public float m_GlueHandDistance = 0.1f;
        [HideInInspector]
        public float m_GlueHandTime = 0.1f;

#if WEAVR_VR

        private InteractionController m_interactionController = null;

        protected override void Start()
        {
            if (Application.isPlaying)
            {
                base.Start();
                m_interactionController = gameObject.GetComponent<InteractionController>();
                switch (m_interactionMode)
                {
                    case InteractionMode.GlueToObject:
                    case InteractionMode.BlendToFinalPose:
                        skeletonPoser = null;
                        break;
                }
            }
        }

        protected override void Update()
        {
            highlightOnHover = false;
            base.Update();
        }

        public virtual void HandleStandardInteraction()
        {

        }

        public virtual void HandleHovering()
        {

        }

        public InteractionMode GetInteractionMode()
        {
            return m_interactionMode;
        }

        public VR_Skeleton_Poser GetCurrentPoser()
        {
            VR_Skeleton_Poser wRet = skeletonPoser as VR_Skeleton_Poser;
            if (m_interactionController)
            {
                AbstractInteractiveBehaviour wCurBehaviour = m_interactionController.GetCurrentBehaviourVR();
                if (wCurBehaviour != null)
                {
                    IVR_Poser wVRPoser = wCurBehaviour as IVR_Poser;
                    if (wVRPoser != null)
                    {
                        wRet = wVRPoser.GetSkeletonPoser();
                    }
                }
            }
            if (wRet == null)
            {
                wRet = gameObject.GetComponent<VR_Skeleton_Poser>();
            }
            return wRet;
        }

        public Transform GetAttachmentPoint(Hand iHand)
        {
            if (m_interactionController != null && m_interactionController.GetCurrentBehaviourVR() is IVR_Attachable wVR_Poser)
            {
                return wVR_Poser.GetAttachmentPoint(iHand);
            }
            return transform;
        }


        public HoveringMode GetHoveringMode()
        {
            if (m_interactionController != null && m_interactionController.GetCurrentBehaviourVR() is IVR_Poser wVR_Poser)
            {
                return wVR_Poser.GetHoveringMode();
            }
            return HoveringMode.Any;
        }

        public Valve.VR.SteamVR_Skeleton_JointIndexEnum GetFingerJointHovering()
        {
            if (m_interactionController != null && m_interactionController.GetCurrentBehaviourVR() is IVR_Poser wVR_Poser)
            {
                return wVR_Poser.GetFingerHoverIndex();
            }
            return Valve.VR.SteamVR_Skeleton_JointIndexEnum.indexTip;
        }

        public bool HasRotationAxis()
        {
            if (m_interactionController != null && m_interactionController.GetCurrentBehaviourVR() is IVR_Attachable wVR_Poser)
            {
                return wVR_Poser.HasRotationAxis();
            }
            return false;
        }

        public bool IsHandParent()
        {
            if (m_interactionController != null && m_interactionController.GetCurrentBehaviourVR() is IVR_Attachable wVR_Poser)
            {
                return wVR_Poser.IsHandParent();
            }
            return false;
        }

        public float GetGlueHandOnHoverDistance()
        {
            return m_GlueHandDistance;
        }

        public float GetGlueHandTime()
        {
            return m_GlueHandTime;
        }

        public virtual void SetupHandToObjectInteraction(bool iWithPose)
        {
            if (iWithPose)
            {
                hideHandOnAttach = false;
                hideSkeletonOnAttach = false;
                hideControllerOnAttach = true;
            }
            else
            {
                //fallback as old WEAVR
                hideHandOnAttach = true;
                hideSkeletonOnAttach = true;
                hideControllerOnAttach = true;
            }
            handFollowTransform = true;
        }
        public virtual void SetupObjectToHandInteraction(bool iWithPose)
        {
            if (iWithPose)
            {
                hideHandOnAttach = false;
                hideSkeletonOnAttach = false;
                hideControllerOnAttach = true;
            }
            else
            {
                //fallback as old WEAVR
                hideHandOnAttach = true;
                hideSkeletonOnAttach = true;
                hideControllerOnAttach = true;
            }
            handFollowTransform = false;
        }

        protected override void OnAttachedToHand(Hand hand)
        {
            base.OnAttachedToHand(hand);
            if (hand.skeleton)
            {
                hand.skeleton.BlendTo(0.0f, 0.1f);
            }
        }

        protected override void OnDetachedFromHand(Hand hand)
        {
            base.OnDetachedFromHand(hand);
            if (hand.skeleton)
            {
                hand.skeleton.skeletonBlend = 1.0f;
            }
        }

        private void HandHoverUpdate(Hand hand)
        {
            if (m_interactionController == null)
            {
                if(VR_ControllerManager.GetStandardInteractionButtonDown(hand))
                {
                    HandleStandardInteraction();
                }
            }
        }

        
#endif

    }

}