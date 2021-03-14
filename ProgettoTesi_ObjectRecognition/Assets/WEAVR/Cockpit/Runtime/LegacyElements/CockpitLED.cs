namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    [Obsolete("Use Element which is newer and more customizable")]
    [AddComponentMenu("")]
    [RequireComponent(typeof(ElementLightUp))]
    public class CockpitLED : CockpitElement
    {
        public enum LEDState { ON, OFF }

        public bool interactable = false;

        [SerializeField]
        private LEDState _state;
        public LEDState State {
            get {
                return _state;
            }
            set {
                if(_state != value) {
                    _state = value;
                    ChangeState(_state);
                    if (_lightUpComponent != null) {
                        if(_state == LEDState.ON) {
                            _lightUpComponent.LightsOn();
                        }
                        else {
                            _lightUpComponent.LightsOff();
                        }
                    }
                }
            }
        }

        private ElementLightUp _lightUpComponent;

        public override void OnPointerUp(PointerEventData eventData) {
            if (interactable) {
                State = State == LEDState.ON ? LEDState.OFF : LEDState.ON;
                eventData.Use();
            }
        }

        protected override bool AutoRegisterStates() {
            return true;
        }

        protected override Enum GetDefaultState() {
            return _state;
        }

        protected override void ChangeToNewState(ElementState newState) {
            var enumState = Enum.Parse(typeof(LEDState), newState.state);
            if(enumState != null) {
                State = (LEDState)enumState;
            }
        }

        protected override void RegisterStates() {
            //RegisterState(LEDState.ON, transform.localPosition);
            //RegisterState(LEDState.OFF, transform.localPosition);
        }

        protected override void Start() {
            base.Start();
            _lightUpComponent = GetComponent<ElementLightUp>();
            if(_lightUpComponent != null && _state == LEDState.ON) {
                _lightUpComponent.LightsOn();
            }
            ChangeState(_state);
        }
    }
}