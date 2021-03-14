namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

#if WEAVR_VR
    using Valve.VR.InteractionSystem;
#endif

    [AddComponentMenu("WEAVR/Interactions/Basic/Placeable", 0)]
    [RequireComponent(typeof(InteractionController))]
    public class Placeable : AbstractPlaceable
    {

        protected override void StartGrabbable()
        {
            m_grabbable = GetComponent<AbstractGrabbable>();
#if !WEAVR_VR
            if(m_grabbable) {
                m_grabbable.onUngrab.AddListener(TryReturnToItsPlace);
            }
#endif
        }

#if WEAVR_VR

        public override bool CanInteractVR(ObjectsBag bag, object hand)
        {
            return true;
        }

        public override void InteractVR(ObjectsBag currentBag, object hand)
        {
            base.InteractVR(currentBag, hand);
            Interact(currentBag);
        }

#endif
    }
}