using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Interaction;
using UnityEngine;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Maintenance
{
    [AddComponentMenu("WEAVR/Interactions/Basic/Pocket", 0)]
    [RequireComponent(typeof(InteractionController))]
    public class InteractivePocket : AbstractInteractivePocket
    {
        public override bool CanBeDefault => true;

#if WEAVR_VR

        private void OnValidate()
        {
            InteractTrigger = BehaviourInteractionTrigger.OnPointerDown;
        }

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return !bag.Selected;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            if (Pocket.PocketedObjects.Count > 0)
            {
                var pocketed = Pocket.PocketOutFirst();
                if (grabOnPocketOut && pocketed)
                {
                    var grabbable = pocketed.GetComponentInParent<AbstractGrabbable>();
                    if (grabbable)
                    {
                        grabbable.InteractVR(currentBag, hand);
                    }
                }
            }
        }
#endif
    }
}
