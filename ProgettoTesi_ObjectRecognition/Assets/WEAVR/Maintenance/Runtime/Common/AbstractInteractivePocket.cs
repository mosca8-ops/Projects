using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using UnityEngine;

namespace TXT.WEAVR.Maintenance
{
    [RequireComponent(typeof(Pocket))]
    public abstract class AbstractInteractivePocket : AbstractInteractiveBehaviour
    {
        public bool grabOnPocketOut = true;

        private Pocket m_pocket;

        public Pocket Pocket => m_pocket;

        private void OnEnable()
        {
            if (!m_pocket)
            {
                m_pocket = GetComponent<Pocket>();
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return currentBag.Selected ? "Pocket in" : "Take out from Pocket";
        }

        public override bool CanInteract(ObjectsBag currentBag)
        {
            return currentBag.Selected ? Pocket.CanPocketIn(currentBag.Selected) : Pocket.PocketedObjects.Count > 0;
        }

        public override void Interact(ObjectsBag currentBag)
        {
            if (currentBag.Selected)
            {
                Pocket.PocketIn(currentBag.Selected);
            }
            else if (Pocket.PocketedObjects.Count > 0)
            {
                var pocketed = Pocket.PocketOutFirst();
                if (grabOnPocketOut && pocketed)
                {
                    var grabbable = pocketed.GetComponentInParent<AbstractGrabbable>();
                    if (grabbable)
                    {
                        grabbable.Grab();
                    }
                }
            }
        }
    }
}
