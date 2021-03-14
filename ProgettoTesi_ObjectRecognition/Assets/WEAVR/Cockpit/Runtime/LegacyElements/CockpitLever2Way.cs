namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    public class CockpitLever2Way : CockpitLever
    {
        public enum LeverState { UP, DOWN }

        [SerializeField]
        private LeverState _state;
        public LeverState State {
            get {
                return _state;
            }
            set {
                if(_state != value) {
                    _state = value;
                    ChangeState(value);
                }
            }
        }

        public override void OnPointerUp(PointerEventData eventData) {
            State = State == LeverState.DOWN ? LeverState.UP : LeverState.DOWN;
        }

        protected override bool AutoRegisterStates() {
            return true;
        }

        protected override Enum GetDefaultState() {
            return _state;
        }

        protected override void SetInitialState(Enum state) {
            if(state is LeverState) {
                _state = (LeverState)state;
            }
        }

        protected override void RegisterStates() {

        }

        // Use this for initialization
        void Start() {
            ChangeState(_state);
        }

        protected override void State_PointerUp(object sender, PointerEventData data) {
            if(sender is InteractiveElementState) {
                var newState = (sender as InteractiveElementState).GetStateEnum(_state);
                if (newState is LeverState) {
                    State = (LeverState)(newState);
                    data.Use();
                }
            }
        }
    }
}