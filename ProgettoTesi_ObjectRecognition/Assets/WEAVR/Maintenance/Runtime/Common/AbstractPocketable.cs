namespace TXT.WEAVR.Maintenance
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Interaction;
    using UnityEngine;

    public abstract class AbstractPocketable : AbstractInteractiveBehaviour
    {
        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return currentBag.ContainsKey(gameObject.name) ? "Remove from bag" : "Add to bag";
        }

        public override void Interact(ObjectsBag currentBag)
        {
            if (currentBag.ContainsKey(gameObject.name))
            {
                currentBag.Remove(gameObject.name);
            }
            else
            {
                currentBag.Add(gameObject.name, gameObject);
            }
        }
    }
}