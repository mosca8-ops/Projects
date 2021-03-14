using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Manipulators/Visibility Switcher")]
    public class VR_VisibilitySwitcher : VR_Manipulator
    {
        public enum SwitchMode { VisibleOnHover, HideOnHover }

        public SwitchMode mode = SwitchMode.VisibleOnHover;
        public GameObject manipulate;

#if WEAVR_VR
        
        public override bool CanHandleData(object value)
        {
            return true;
        }

        public override void UpdateValue(float value)
        {
            
        }

        protected override void UpdateManipulator(Hand hand, Interactable interactable)
        {
            
        }

        protected override void BeginManipulating(Hand hand, Interactable interactable)
        {
            base.BeginManipulating(hand, interactable);
            if(manipulate == null) { return; }
            switch (mode)
            {
                case SwitchMode.HideOnHover:
                    manipulate.SetActive(false);
                    break;
                case SwitchMode.VisibleOnHover:
                    manipulate.SetActive(true);
                    break;
            }
        }

        public override void StopManipulating(Hand hand, Interactable interactable)
        {
            base.StopManipulating(hand, interactable);
            if(manipulate == null) { return; }
            switch (mode)
            {
                case SwitchMode.HideOnHover:
                    manipulate.SetActive(true);
                    break;
                case SwitchMode.VisibleOnHover:
                    manipulate.SetActive(false);
                    break;
            }
        }

#endif
    }
}
