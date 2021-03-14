namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    public class CockpitButton : CockpitElement
    {
        public enum ButtonState { UP, DOWN }

        [SerializeField]
        protected ButtonState _state;

        public virtual ButtonState State {
            get {
                return _state;
            }

            set {
                if(_state != value) {
                    _state = value;
                    ChangeState(_state);
                }
            }
        }

        public Vector3 downOffset = new Vector3(0, -0.01f, 0);

        public override void OnPointerUp(PointerEventData eventData) {
            State = State == ButtonState.DOWN ? ButtonState.UP : ButtonState.DOWN;
        }

        protected override void RegisterStates() {
            if(!RegisterState(ButtonState.UP, "CurrentState", 0)) {
                RegisterState(ButtonState.UP, transform.localPosition);
            }
            if(!RegisterState(ButtonState.DOWN, "CurrentState", 1)) {
                RegisterState(ButtonState.DOWN, transform.localPosition + downOffset);
            }
        }

        protected override void Start() {
            base.Start();
            ChangeState(_state);
        }

        protected override bool AutoRegisterStates() {
            return false;
        }

        protected override Enum GetDefaultState() {
            return _state;
        }
    }
}