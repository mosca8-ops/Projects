namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    public class CockpitLeverThrottle : CockpitLever
    {
        public enum LeverState { POWER, IDLE, MAXREV }

        [SerializeField]
        private LeverState _state;
        private LeverState _previousState;
        public LeverState State {
            get {
                return _state;
            }
            set {
                if (_state != value) {
                    _previousState = _state;
                    _state = value;
                    ChangeState(value);
                }
            }
        }

        // Use this for initialization
        void Start() {
            ChangeState(_state);
        }

        protected override void State_PointerUp(object sender, PointerEventData data) {
            if (sender is InteractiveElementState) {
                var newState = (sender as InteractiveElementState).GetStateEnum(_state);
                if (newState is LeverState) {
                    State = (LeverState)(newState);
                    Debug.Log("InteractiveState Clicked");
                    data.Use();
                }
            }
        }

        protected override void RegisterStates() {  }

        protected override bool AutoRegisterStates() {
            return true;
        }

        protected override Enum GetDefaultState() {
            return _state;
        }

        protected override void SetInitialState(Enum state) {
            if (state is LeverState) {
                _state = (LeverState)state;
            }
        }

        public override void OnPointerUp(PointerEventData eventData) {
            if (eventData.rawPointerPress == gameObject) {
                Debug.Log("Object Clicked");
                switch (State) {
                    case LeverState.IDLE:
                        State = _previousState == LeverState.POWER ? LeverState.MAXREV : LeverState.POWER;
                        break;
                    case LeverState.POWER:
                    case LeverState.MAXREV:
                        State = LeverState.IDLE;
                        break;
                }
            }
        }
    }
}