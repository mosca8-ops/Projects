namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    public class CockpitLever5Way : CockpitLever
    {
        public enum LeverState { STATE_1, STATE_2, STATE_3, STATE_4, STATE_5 }

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


        public override void OnPointerUp(PointerEventData eventData) {
            switch (State) {
                case LeverState.STATE_1:
                    State = LeverState.STATE_2;
                    break;
                case LeverState.STATE_2:
                    State = _previousState == LeverState.STATE_1 ? LeverState.STATE_3 : LeverState.STATE_1;
                    break;
                case LeverState.STATE_3:
                    State = _previousState == LeverState.STATE_2 ? LeverState.STATE_4 : LeverState.STATE_2;
                    break;
                case LeverState.STATE_4:
                    State = _previousState == LeverState.STATE_3 ? LeverState.STATE_5 : LeverState.STATE_3;
                    break;
                case LeverState.STATE_5:
                    State = LeverState.STATE_4;
                    break;
            }
        }

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

        protected override void RegisterStates() {

        }

        void Start() {
            ChangeState(_state);
        }

        protected override void State_PointerUp(object sender, PointerEventData data) {
            if (sender is InteractiveElementState) {
                var newState = (sender as InteractiveElementState).GetStateEnum(_state);
                if (newState is LeverState) {
                    State = (LeverState)(newState);
                    data.Use();
                }
            }
        }
    }
}