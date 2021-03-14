using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

#if WEAVR_VR
using Valve.VR.InteractionSystem;
#endif

namespace TXT.WEAVR.Interaction
{

    [AddComponentMenu("WEAVR/VR/Manipulators/Vibration")]
    public class VR_Vibration : VR_Manipulator
    {
        [Tooltip("The number of haptic pulses evenly distributed along the mapping")]
        public int teethCount = 128;

        [Tooltip("Minimum duration of the haptic pulse")]
        public int minimumPulseDuration = 500;

        [Tooltip("Maximum duration of the haptic pulse")]
        public int maximumPulseDuration = 900;

        [Tooltip("This event is triggered every time a haptic pulse is made")]
        public UnityEvent onPulse;

#if WEAVR_VR
        private float m_value;

        public override bool CanHandleData(object value)
        {
            return value is float;
        }

        public override void UpdateValue(float value)
        {
            m_value = value;
        }

        protected override void UpdateManipulator(Hand hand, Interactable interactable)
        {
            
        }

        private Hand m_hand;
        private int previousToothIndex = -1;

        private Span m_defaultSpan = new Span(0, 1);
        private Span m_currentSpan = new Span(0, 1);

        public override void StartManipulating(Hand hand, Interactable interactable, bool iIsKeepPressedLogic, Func<float> getter, Action<float> setter, Span? span = null)
        {
            base.StartManipulating(hand, interactable, iIsKeepPressedLogic, getter, setter, span);
            m_hand = hand;
            m_currentSpan = span ?? m_defaultSpan;
        }

        //-------------------------------------------------
        void Update()
        {
            if (m_isActive)
            {
                int currentToothIndex = Mathf.RoundToInt(m_floatGetter() * teethCount - 0.5f);
                if (currentToothIndex != previousToothIndex)
                {
                    Pulse();
                    previousToothIndex = currentToothIndex;
                }
            }
        }


        //-------------------------------------------------
        private void Pulse()
        {
            if (m_hand && VR_ControllerManager.GetStandardInteractionButton(m_hand))
            {
                ushort duration = (ushort)UnityEngine.Random.Range(minimumPulseDuration, maximumPulseDuration + 1);
                m_hand.TriggerHapticPulse(duration);

                onPulse.Invoke();
            }
        }
#endif
    }
}
