namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [Serializable]
    public class DummyState : BaseDiscreteState
    {
        public override bool UseOwnerEvents {
            get {
                return false;
            }
        }

        public override void OnStateEnter(BaseDiscreteState fromState) {
            Debug.LogFormat("{0} became active", this);
        }

        public override void OnStateExit(BaseDiscreteState toState) {
            throw new NotImplementedException();
        }
    }
}