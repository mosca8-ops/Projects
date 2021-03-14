namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using TXT.WEAVR.Core;
    using UnityEngine;


#if WEAVR_VR
    using Valve.VR.InteractionSystem;
    using Valve.VR;
#endif

    [Stateless]
    [AddComponentMenu("WEAVR/Interactions/Basic/Executable", 0)]
    [RequireComponent(typeof(InteractionController))]
    public class Executable : AbstractExecutable, IVR_Poser
    {
        public VR_Object.HoveringMode m_hoveringMode;


#if WEAVR_VR
        //TODO FIX SERIALIZATION
        [HideInInspector]
        public SteamVR_Skeleton_JointIndexEnum m_jointIndexForHovering = SteamVR_Skeleton_JointIndexEnum.indexTip;

        public VR_Skeleton_Poser m_SkeletonPoser = null;
        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            if (hand is VR_Hand wHand)
            {
                return wHand.currentAttachedObject == null;
            }
            return false;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            base.InteractVR(currentBag, hand);
            Execute();
        }

        public override bool UseStandardVRInteraction(ObjectsBag bag, object hand)
        {
            return bag.Selected != null || !Controller.Has<AbstractGrabbable>();
        }

        protected override void Start()
        {
            base.Start();
            var wVRObject = GetComponent<VR_Object>();
            if (m_SkeletonPoser == null)
            {
                m_SkeletonPoser = transform.GetComponent<VR_Skeleton_Poser>();
            }
            else if (wVRObject != null)
            {
                wVRObject.skeletonPoser = m_SkeletonPoser;
            }
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
            return m_hoveringMode;
        }

#endif
    }
}