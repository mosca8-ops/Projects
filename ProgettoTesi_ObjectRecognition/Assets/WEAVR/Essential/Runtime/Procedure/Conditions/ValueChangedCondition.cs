using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ValueChangedCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The object to get the value to check changes on")]
        [Draggable]
        private GameObject m_target;
        [SerializeField]
        [Tooltip("The property to check if the value changed")]
        [PropertyDataFrom(nameof(m_target), isSetter: false)]
        private Property m_property;

        [System.NonSerialized]
        private object m_currentValue;

        public GameObject Target
        {
            get => m_target;
            set
            {
                if (m_target != value)
                {
                    BeginChange();
                    m_target = value;
                    PropertyChanged(nameof(Target));
                }
            }
        }

        Object ITargetingObject.Target
        {
            get => m_target;
            set => Target = value is Component c ? c.gameObject : value is GameObject go ? go : value == null ? null : m_target;
        }

        public string PropertyPath
        {
            get => m_property?.Path;
            set
            {
                if (m_property == null)
                {
                    m_property = new Property();
                }
                if (m_property.Path != value)
                {
                    m_property.Path = value;
                }
            }
        }

        public System.Type PropertyType => m_property?.PropertyType;

        public object CurrentValue => m_property.Value;

        public string TargetFieldName => nameof(m_target);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_property == null)
            {
                m_property = new Property();
            }
        }

        public static ValueChangedCondition Create(GameObject target)
        {
            var condition = CreateInstance<ValueChangedCondition>();
            condition.m_target = target;
            return condition;
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            // Prepare Property
            m_property.Target = m_target;
            m_currentValue = m_property.Value;
        }

        protected override bool EvaluateCondition()
        {
            return !object.Equals(m_currentValue, m_property.Value);
        }

        public override string GetDescription()
        {
            return !m_target ? "Target not set" :
                               m_property == null || string.IsNullOrEmpty(m_property.Path) ? $"{m_target.name}.[ ? ]" :
                               $"{m_target.name}.{m_property.PropertyName} changed value";
        }

        public override string ToFullString()
        {
            return $"{(m_target ? m_target.name : "?")} changed";
        }
    }
}
