namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    public class CockpitLever3Way : CockpitLever
    {
        public enum LeverState { UP, CENTER, DOWN }

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
                case LeverState.UP:
                    State = LeverState.CENTER;
                    break;
                case LeverState.CENTER:
                    State = _previousState == LeverState.DOWN ? LeverState.UP : LeverState.DOWN;
                    break;
                case LeverState.DOWN:
                    State = LeverState.CENTER;
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