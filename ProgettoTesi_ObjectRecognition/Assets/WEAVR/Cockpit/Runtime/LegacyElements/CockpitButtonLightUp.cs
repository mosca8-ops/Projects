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
    public class CockpitButtonLightUp : CockpitButton
    {
        private ElementLightUp _lightUpComponent;

        public ButtonState lightUpOn;

        public override ButtonState State {
            get {
                return base.State;
            }

            set {
                if (_state != value) {
                    _state = value;
                    ChangeState(_state);
                    if (_lightUpComponent != null) {
                        if (_state == lightUpOn) {
                            _lightUpComponent.LightsOn();
                        }
                        else {
                            _lightUpComponent.LightsOff();
                        }
                    }
                }
            }
        }

        protected override void Start() {
            _lightUpComponent = GetComponent<ElementLightUp>();
            if (_lightUpComponent != null && _state == lightUpOn) {
                _lightUpComponent.LightsOn();
            }
            base.Start();
        }
    }
}