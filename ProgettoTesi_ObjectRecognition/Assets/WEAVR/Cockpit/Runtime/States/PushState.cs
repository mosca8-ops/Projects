namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    [Serializable]
    [DiscreteState("Push State")]
    public class PushState : BaseDiscreteState
    {
        public float moveTime = 0.25f;
        public Vector3 deltaPosition;

        public bool isStable = false;
        public bool continuosValue = false;
        public bool hasTriggerState = true;
        public BaseDiscreteState triggerState;

        private BaseDiscreteState _returnState;
        private bool _triggerIsValid;
        private bool _canGoBack;

        private Vector3? _moveBackPosition;

        public UnityEvent onPush;

        public override bool UseOwnerEvents {
            get {
                return true;
            }
        }

        public override void OnStateEnter(BaseDiscreteState fromState) {
            _moveBackPosition = null;
            _returnState = fromState;

            if (UseAnimator && AnimatorParameter.name != null) {
                ApplyValueUpdate();
                AnimatorParameter.SetValue(Owner.animator);
            }
            else if (fromState != null) {
                Owner.MoveTo(this, fromState.GetDefaultPosition() + deltaPosition, moveTime, ApplyValueUpdate);
            }
            else {
                _moveBackPosition = Owner.transform.localPosition;
                Owner.MoveTo(this, Owner.defaultLocalPosition + deltaPosition, moveTime, ApplyValueUpdate);
            }
        }

        

        public override bool CanEnterState(object value) {
            return base.CanEnterState(value);
        }

        public override bool CanEnterState(BaseDiscreteState fromState) {
            return !_triggerIsValid || triggerState == fromState;
        }

        public override void OnStateExit(BaseDiscreteState toState) {

        }

        public override bool OnPointerUp(PointerEventData eventData) {
            if (isStable){
                // Check if needs to go back
                if (Owner.CurrentState == this && _canGoBack) {
                    Owner.CurrentState = _returnState;
                    return true;
                }
                else if (Owner.CurrentState != this && CanEnterState(Owner.CurrentState)) {
                    Owner.CurrentState = this;
                    if (onPush != null) {
                        onPush.Invoke();
                    }
                    return true;
                }
                else {
                    return false;
                }
            }
            else if(_returnState != null) {
                Owner.CurrentState = _returnState;
                _returnState = null;
                return true;
            }

            return false;
        }

        public override bool OnPointerDown(PointerEventData eventData) {
            if (isStable) { return false; }
            if (Owner.CurrentState != this) {
                Owner.CurrentState = this;
                if(onPush != null) {
                    onPush.Invoke();
                }
                return true;
            }
            if (continuosValue && EffectiveBinding.mode != BindingMode.Read && HasValue) {
                EffectiveBinding.Property.Value = Value;
            }

            return false;
        }

        public override void Initialize() {
            base.Initialize();
            _triggerIsValid = hasTriggerState && triggerState != null && triggerState != this;
            _canGoBack = !hasTriggerState || triggerState != this;
        }
    }
}