using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GenericCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The object to get the value to compare from")]
        [Draggable]
        private GameObject m_target;
        [SerializeField]
        [Tooltip("The property to compare")]
        [PropertyDataFrom(nameof(m_target), isSetter: false)]
        private Property m_property;
        [SerializeField]
        [Tooltip("The comparison operator")]
        private GenericOperator m_operator;
        [SerializeField]
        [Tooltip("The value to compare against")]
        [GenericValueTypeFrom(nameof(m_property), true)]
        private GenericValue m_value;

        public GameObject Target
        {
            get => m_target;
            set
            {
                if(m_target != value)
                {
                    BeginChange();
                    m_target = value;
                    PropertyChanged(nameof(Target));
                }
            }
        }

        Object ITargetingObject.Target {
            get => m_target;
            set => Target = value is Component c ? c.gameObject : value is GameObject go ? go : value == null ? null : m_target;
        }

        public string PropertyPath
        {
            get => m_property?.Path;
            set
            {
                if(m_property == null)
                {
                    m_property = new Property();
                }
                if(m_property.Path != value)
                {
                    m_property.Path = value;
                }
            }
        }

        public System.Type PropertyType => m_property?.PropertyType;

        public object CurrentValue => m_property.Value;

        public object ExpectedValue
        {
            get => m_value.Value;
            set
            {
                m_value.Value = value;
            }
        }

        public ComparisonOperator Operator
        {
            get => m_operator.Operator;
            set
            {
                m_operator.Operator = value;
            }
        }

        public string TargetFieldName => nameof(m_target);

        protected override void OnEnable()
        {
            base.OnEnable();
            CanCacheValue = false;
            if(m_property == null)
            {
                m_property = new Property();
            }
            if(m_value == null)
            {
                m_value = new GenericValue();
            }
            m_value.ReferencesResolver = Procedure?.ReferencesResolver;
            m_operator.OnChanged -= Modified;
            m_operator.OnChanged += Modified;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_operator.OnChanged -= Modified;
        }

        public static GenericCondition Create(GameObject target)
        {
            var condition = CreateInstance<GenericCondition>();
            condition.m_target = target;
            return condition;
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            // Prepare Property
            m_property.Target = m_target;
        }

        protected override bool EvaluateCondition()
        {
            return m_operator.Evaluate(m_property.Value, m_value.Value);
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            try
            {
                m_property.TrySetValue(m_operator.GetPotentialFirstValue(m_value.Value));
            }
            catch (System.Exception e)
            {
                WeavrDebug.LogException(this, e);
            }
        }

        public override string GetDescription()
        {
            var value = m_value?.Value;
            string valueString = m_value != null && m_value.IsVariableValue ? $"[{m_value.VariableName}]" : value is Object && (value as Object) ? (value as Object).name : value?.ToString();
            return !m_target ? "Target not set" : 
                               m_property == null || string.IsNullOrEmpty(m_property.Path) ? $"{m_target.name}.[ ? ]" : 
                               $"{m_target.name}.{m_property.PropertyName} {m_operator} {valueString}" ;
        }
        
    }
}
