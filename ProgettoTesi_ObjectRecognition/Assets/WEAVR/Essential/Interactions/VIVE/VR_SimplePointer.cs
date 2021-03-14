using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


#if WEAVR_VR
using Valve.VR;
using Valve.VR.InteractionSystem;
using TXT.WEAVR.Interaction;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Interactions/Simple Pointer")]
    public class VR_SimplePointer : WorldPointer
    {
        [Space]
        public VR_ControllerAction pointerDownButton;

#if WEAVR_VR
        public SteamVR_Skeleton_JointIndexEnum m_FingerJoint = SteamVR_Skeleton_JointIndexEnum.indexTip;
        public VR_Skeleton_Poser m_poser;

        private VR_Hand m_hand;

        protected override GameObject GetPointerObject(GameObject startupPointer)
        {
            return startupPointer != null ? startupPointer : gameObject;
        }

        protected override Vector2 GetScrollDelta(GameObject target)
        {
            return TXT.WEAVR.Interaction.VR_ControllerManager.getTrackpadAxis(m_hand);
        }

        protected override void OnPointerUpdate(float pointerLength, float pointerThickness)
        {
            // Nothing
        }

        public void Initialize(VR_Hand iHand)
        {
            m_hand = iHand;
            pointerDownButton = new VR_ControllerAction
            {
                mAction = VR_ControllerAction.ActionType.TriggerPressed,
                trigger = VR_ControllerAction.ActionState.OnPress
            };
            pointer = null;
            disableOnStart = false;
            thickness = 0.002f;
            maxDistance = 0.06f;
            dragThreshold = 0.02f;
            dynamicCanvasSearch = false;
            onlySpecialCanvases = true;
            ignoreReversedGraphics = true;
            raycastObjects = WorldPointer.RaycastType.Canvases;
            removeConflictingLayers = true;
            enabled = true;
            pointerDownButton.Initialize(iHand);
        }

        protected override bool IsValid()
        {
            return enabled;
        }

        public override bool GetPointerEnabled()
        {
            return enabled;
        }

        public override bool GetPointerDown()
        {
            return enabled && pointerDownButton.IsTriggered();
        }

        protected override void OnPointerDisable()
        {
            enabled = false;
        }
        protected override void OnPointerEnable()
        {
            enabled = true;
        }
#else
        public override bool GetPointerDown()
        {
            return false;
        }

        public override bool GetPointerEnabled()
        {
            return false;
        }

        protected override GameObject GetPointerObject(GameObject startupPointer)
        {
            return null;
        }

        protected override Vector2 GetScrollDelta(GameObject target)
        {
            return Vector2.zero;
        }

        protected override bool IsValid()
        {
            return false;
        }
#endif
    }
}
