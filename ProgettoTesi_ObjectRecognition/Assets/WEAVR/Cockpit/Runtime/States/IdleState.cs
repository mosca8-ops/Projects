namespace TXT.WEAVR.Cockpit
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [DiscreteState("Idle")]
    public class IdleState : BaseDiscreteState
    {
        public float moveTime = 0.5f;

        //public override bool HasValue {
        //    get {
        //        return false;
        //    }
        //}

        public override bool IsEditable {
            get {
                return false;
            }

            protected set {
                base.IsEditable = value;
            }
        }

        public override string Name {
            get {
                return "Idle";
            }

            set {
                base.Name = value;
            }
        }

        public override bool UseOwnerEvents {
            get {
                return true;
            }
        }

        public override void OnStateEnter(BaseDiscreteState fromState) {
            if (UseAnimator && AnimatorParameter.name != null) {
                ApplyValueUpdate();
                AnimatorParameter.SetValue(Owner.animator);
            }
            else {
                Owner.MoveTo(this, Owner.defaultLocalPosition, moveTime, ApplyValueUpdate);
                Owner.RotateTo(this, Owner.defaultLocalRotation, moveTime);
            }
        }

        public override void OnStateExit(BaseDiscreteState toState) {
            
        }
    }
}