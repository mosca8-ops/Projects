namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;



    public abstract class AbstractComposable : AbstractInteractiveBehaviour
    {
        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return "Compose";
        }

        public override void Interact(ObjectsBag currentBag)
        {
            
        }
    }
}