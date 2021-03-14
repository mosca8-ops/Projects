namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [ModifierState("Dummy")]
    public class DummyModifier : BaseModifierState
    {
        public override bool UseOwnerEvents {
            get {
                return false;
            }
        }
    }
}