namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Serializable]
    [DiscreteState("Switch State")]
    public class PositionState : BaseDiscreteState
    {
        public bool isDelta;
        public float moveTime = 0.5f;
        public bool positionEnabled;
        public Vector3 position;
        public bool rotationEnabled;
        public Quaternion rotation;

        public bool isStable = true;
        public BaseDiscreteState fallbackState;

        private bool _valueUpdated = false;

        public override bool UseOwnerEvents {
            get {
                return false;
            }
        }

        public override void OnStateEnter(BaseDiscreteState fromState) {
            _valueUpdated = false;

            if (UseAnimator && AnimatorParameter.name != null) {
                ApplyValueUpdate();
                AnimatorParameter.SetValue(Owner.animator);
            }
            else {
                if (isDelta) {
                    if (positionEnabled) { Owner.MoveTo(this, ConvertToLocal(position), moveTime, ApplyValueUpdate); }
                    if (rotationEnabled) { Owner.RotateTo(this, ConvertToLocal(rotation), moveTime, ApplyValueUpdate); }
                }
                else {
                    if (positionEnabled) { Owner.MoveTo(this, position, moveTime, ApplyValueUpdate); }
                    if (rotationEnabled) { Owner.RotateTo(this, rotation, moveTime, ApplyValueUpdate); }
                }
            }
        }

        public override void OnStateExit(BaseDiscreteState toState) {
            
        }

        protected override void ApplyValueUpdate() {
            if (!_valueUpdated) {
                _valueUpdated = true;
                base.ApplyValueUpdate();
            }
        }

        public override bool OnPointerUp(PointerEventData eventData) {
            if (isStable) {
                Owner.CurrentState = this;
                return true;
            }
            else if(fallbackState != null) {
                Owner.CurrentState = fallbackState;
            }

            return false;
        }

        public override bool OnPointerDown(PointerEventData eventData) {
            if(!isStable && fallbackState != null) {
                Owner.CurrentState = this;
                return true;
            }
            return false;
        }

        public override Vector3 GetDefaultPosition() {
            return positionEnabled ? (isDelta ? ConvertToLocal(position) : position) : base.GetDefaultPosition();
        }

        protected virtual Vector3 ConvertToLocal(Vector3 position) {
            return Owner.defaultLocalPosition + position;
        }

        protected virtual Quaternion ConvertToLocal(Quaternion rotation) {
            return Owner.defaultLocalRotation * rotation;
        }
    }
}