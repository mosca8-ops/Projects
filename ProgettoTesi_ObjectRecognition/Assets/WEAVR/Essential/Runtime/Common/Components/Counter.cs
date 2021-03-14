using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Utilities/Counter")]
    public class Counter : MonoBehaviour
    {
        [SerializeField]
        private int m_value;
        [SerializeField]
        private int m_resetValue;
        [Header("Events")]
        [SerializeField]
        private UnityEventInt m_onReset;
        [Space]
        [SerializeField]
        private IntEvents m_events;
        [SerializeField]
        private StringEvents m_stringEvents;

        [Header("Limits")]
        [SerializeField]
        private OptionalSpan m_limits;
        [SerializeField]
        [ShowIf(nameof(ShowLimits))]
        private LimitsEvents m_limitsEvents;

        public int Value
        {
            get => m_value;
            set
            {
                if(m_value != value)
                {
                    int delta = value - m_value;
                    m_value = value;
                    m_events.onValueChanged.Invoke(m_value);
                    m_stringEvents.onValueChanged.Invoke(m_value.ToString());
                    if (delta < 0)
                    {
                        m_events.onDecrement.Invoke(-delta);
                        m_stringEvents.onDecrement.Invoke((-delta).ToString());
                    }
                    else if (delta > 0)
                    {
                        m_events.onIncrement.Invoke(delta);
                        m_stringEvents.onIncrement.Invoke(delta.ToString());
                    }
                    if (m_limits.enabled)
                    {
                        if (!m_limits.value.IsValid(m_value)) {
                            if (m_value <= m_limits.value.min)
                            {
                                m_limitsEvents.onMinReached.Invoke((int)m_limits.value.min);
                            }
                            else if (m_value >= m_limits.value.max)
                            {
                                m_limitsEvents.onMaxReached.Invoke((int)m_limits.value.max);
                            }
                            m_value = (int)m_limits.value.Clamp(m_value);
                        }
                        m_limitsEvents.onNormalizedValueChanged.Invoke(m_limits.value.Normalize(m_value));
                    }
                }
            }
        }

        public int ValueToReset
        {
            get => m_resetValue;
            set
            {
                if(m_resetValue != value)
                {
                    m_resetValue = value;
                }
            }
        }

        private void Reset()
        {
            m_limits = new OptionalSpan(new Span(0, 100), false);
        }

        public void Increment()
        {
            Increment(1);
        }

        public void Increment(int value)
        {
            Value += value;
        }

        public void Decrement()
        {
            Increment(-1);
        }

        public void Decrement(int value)
        {
            Increment(-value);
        }

        public void ResetValue()
        {
            Value = m_resetValue;
            m_onReset.Invoke(m_resetValue);
        }

        public void ResetValueTo(int resetValue)
        {
            Value = resetValue;
            m_onReset.Invoke(resetValue);
        }

        private bool ShowLimits()
        {
            return m_limits.enabled;
        }


        [Serializable]
        private struct LimitsEvents
        {
            public UnityEventInt onMinReached;
            public UnityEventInt onMaxReached;
            public UnityEventFloat onNormalizedValueChanged;
        }

        [Serializable]
        private struct IntEvents
        {
            public UnityEventInt onIncrement;
            public UnityEventInt onDecrement;
            public UnityEventInt onValueChanged;
        }

        [Serializable]
        private struct StringEvents
        {
            public UnityEventString onIncrement;
            public UnityEventString onDecrement;
            public UnityEventString onValueChanged;
        }
    }
}
