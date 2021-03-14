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
    public class CockpitSwitchWithLock : CockpitElement
    {
        public enum SwitchState { UP, LOCK, DOWN }

        [SerializeField]
        private SwitchState _state;
        private SwitchState _previousState;

        public SwitchState State {
            get {
                return _state;
            }

            set {
                if (_state != value) {
                    _previousState = _state;
                    _state = value;
                    ChangeState(_state);
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData) {
            switch (State) {
                case SwitchState.UP:
                    State = SwitchState.LOCK;
                    break;
                case SwitchState.LOCK:
                    State = _previousState == SwitchState.DOWN ? SwitchState.UP : SwitchState.DOWN;
                    break;
                case SwitchState.DOWN:
                    State = SwitchState.LOCK;
                    break;
            }
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