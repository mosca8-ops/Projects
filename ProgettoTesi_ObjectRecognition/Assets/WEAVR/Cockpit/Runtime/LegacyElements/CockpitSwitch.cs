namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using Common;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    [Serializable]
    public class CockpitSwitch : CockpitElement
    {
        public enum SwitchState { UP, DOWN }

        [SerializeField]
        private SwitchState _state;

        public SwitchState State {
            get {
                return _state;
            }

            set {
                if (_state != value) {
                    _state = value;
                    ChangeState(_state);
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData) {
            State = State == SwitchState.DOWN ? SwitchState.UP : SwitchState.DOWN;
        }

        private void Start() {
            ChangeState(_state);
        }

        protected override void RegisterStates() {

        }

        protected override bool AutoRegisterStates() {
            return true;
        }

        protected override Enum GetDefaultState() {
            return _state;
        }

        protected override void SetInitialState(Enum state) {
            if (state is SwitchState) {
                _state = (SwitchState)state;
            }
        }
    }
}