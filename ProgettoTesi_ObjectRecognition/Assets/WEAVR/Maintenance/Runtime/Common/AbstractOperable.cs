using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.UI;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Maintenance
{
    public enum SwipeDirection { Horizontal, Vertical, Circular }

    public abstract class AbstractOperable : AbstractInteractiveBehaviour
    {
        [Space]
        [Tooltip("The name of the operation to be seen in menus")]
        public string operationName = "Operate";
        [Tooltip("The field name to show when showing value change popup")]
        public string property = "Value";
        [SerializeField]
        protected float m_value;

        public OptionalFloat valueStep = 0.01f;

        [Header("Intervals")]
        public Span limits = new Span(0, 100);
        [InclusiveSpan(nameof(limits))]
        public Span valid = new Span(50, 70);

        [Header("Colors")]
        public Color validColor = Color.green;
        public Color notValidColor = Color.red;

        [Header("External")]
        [SerializeField]
        [Draggable]
        private ValueIndicator m_valueIndicator;
        [SerializeField]
        [HiddenBy(nameof(m_valueIndicator))]
        private string m_measureUnit;
        [SerializeField]
        [Draggable]
        protected GameObject[] m_compatibleTools;

        [Header("Virtual Reality")]
        [SerializeField]
        protected SwipeDirection m_swipeDirection = SwipeDirection.Vertical;

        [Space]
        [SerializeField]
        protected Events m_events;

        public UnityEventFloat onValueChange => m_events.onValueChange;
        public UnityEvent onValidRangeEnter => m_events.onValidRangeEnter;
        public UnityEvent onValidRangeExit => m_events.onValidRangeExit;

        public Func<float, float> Filter = f => f;

        public ValueIndicator ValueIndicator
        {
            get => m_valueIndicator;
            set
            {
                if(m_valueIndicator != value)
                {
                    m_valueIndicator = value;
                }
            }
        }

        public string MeasureUnit
        {
            get => m_measureUnit;
            set
            {
                if (m_measureUnit != value)
                {
                    m_measureUnit = value;
                }
            }
        }
        
        public float Value {
            get { return m_value; }
            set {
                float v = Filter(value);
                if (m_value != v)
                {
                    bool wasValid = valid.IsValid(m_value);
                    m_value = valueStep.enabled ? limits.Clamp((int)(v / valueStep) * valueStep)
                                                : limits.Clamp(v);
                    bool isValid = valid.IsValid(m_value);
                    if(wasValid && !isValid)
                    {
                        m_events.onValidRangeExit.Invoke();
                    }
                    else if(isValid && !wasValid)
                    {
                        m_events.onValidRangeEnter.Invoke();
                    }
                    m_events.onValueChange.Invoke(m_value);
                    if (m_valueIndicator)
                    {
                        m_valueIndicator.Measure = property;
                        m_valueIndicator.MeasureUnit = m_measureUnit;
                        m_valueIndicator.SetValue(m_value, isValid ? ValueIndicator.ValueImportance.Valid : ValueIndicator.ValueImportance.Normal);
                    }

                    Controller.CurrentBehaviour = this;
                }
            }
        }

        [IgnoreStateSerialization]
        private float m_targetAutoValue;
        [IgnoreStateSerialization]
        public float AutoValue {
            get {
                return m_targetAutoValue;
            }
            set {
                m_targetAutoValue = value;
                ValueChangerMenu.Show(transform, true, property, Value,
                                                    limits.min, limits.max,
                                                    valueStep, UpdateValue)
                                .SetAutomaticTargetValue(value, valueStep * 5);
            }
        }

        protected override void Reset()
        {
            base.Reset();
            valueStep = 0.1f;
            valueStep.enabled = false;
        }

        protected virtual void OnValidate()
        {
            valueStep.value = Mathf.Approximately(valueStep, 0) ? 1 : valueStep;
        }

        protected virtual void Start()
        {
            valueStep.value = Mathf.Approximately(valueStep, 0) ? 1 : valueStep;
            limits.step = valueStep;

            if (m_valueIndicator)
            {
                m_valueIndicator.Measure = property;
                m_valueIndicator.MeasureUnit = m_measureUnit;
                m_valueIndicator.SetValue(m_value, valid.IsValid(m_value) ? ValueIndicator.ValueImportance.Valid : ValueIndicator.ValueImportance.Normal);
            }

            m_events.onValueChange.Invoke(m_value);
            if (valid.IsValid(m_value))
            {
                m_events.onValidRangeEnter.Invoke();
            }
            else
            {
                m_events.onValidRangeExit.Invoke();
            }
        }

        public override string GetInteractionName(ObjectsBag currentBag)
        {
            return operationName;
        }

        public void ShowPopup()
        {
            ValueChangerMenu.Show(transform, true, property, Value,
                                                    limits.min, limits.max,
                                                    valueStep, UpdateValue);
            UpdateValue(Value);
        }

        private void UpdateValue(float newValue)
        {
            bool wasValid = IsValidValue;
            m_value = newValue;
            if (IsValidValue)
            {
                ValueChangerMenu.Instance.ValueColor = validColor;
                if (!wasValid)
                {
                    m_events.onValidRangeEnter.Invoke();
                }
            }
            else
            {
                ValueChangerMenu.Instance.ValueColor = notValidColor;
                if (wasValid)
                {
                    m_events.onValidRangeExit.Invoke();
                }
            }
            m_events.onValueChange.Invoke(Value);
        }

        public bool IsValidValue {
            get { return valid.IsValid(Value); }
        }

        public override bool CanBeDefault => true;

        private void OnDisable()
        {
            if (Controller.TemporaryMainBehaviour == this)
            {
                Controller.TemporaryMainBehaviour = null;
            }
        }

        public override void OnDisableInteraction()
        {
            base.OnDisableInteraction();

            OnDisableInteractionInternal();
        }

        protected abstract void OnDisableInteractionInternal();

        [Serializable]
        protected struct Events
        {
            public UnityEventFloat onValueChange;
            public UnityEvent onValidRangeEnter;
            public UnityEvent onValidRangeExit;
        }
    }
}