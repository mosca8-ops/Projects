using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    public class GenericComparisonCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The first object to get the value from")]
        [Draggable]
        private GameObject m_targetA;
        [SerializeField]
        [Tooltip("The property to get")]
        [PropertyDataFrom(nameof(m_targetA), isSetter: false)]
        private Property m_propertyA;
        [SerializeField]
        [Tooltip("The comparison operator")]
        private GenericOperator m_operator;
        [SerializeField]
        [Tooltip("The other object to compare to")]
        [Draggable]
        private GameObject m_targetB;
        [SerializeField]
        [Tooltip("The property of the second object to compare")]
        [PropertyDataFrom(nameof(m_targetB), nameof(m_propertyA), isSetter: false)]
        private Property m_propertyB;

        public GameObject TargetA
        {
            get => m_targetA;
            set
            {
                if (m_targetA != value)
                {
                    BeginChange();
                    m_targetA = value;
                    PropertyChanged(nameof(TargetA));
                }
            }
        }

        public GameObject TargetB
        {
            get => m_targetB;
            set
            {
                if (m_targetB != value)
                {
                    BeginChange();
                    m_targetB = value;
                    PropertyChanged(nameof(TargetB));
                }
            }
        }

        Object ITargetingObject.Target
        {
            get => m_targetA;
            set => TargetA = value is Component c ? c.gameObject : value is GameObject go ? go : value == null ? null : m_targetA;
        }

        public System.Type PropertyType => m_propertyA?.PropertyType;

        public object ValueA => m_propertyA.Value;
        public object ValueB => m_propertyB.Value;

        public string TargetFieldName => nameof(m_targetA);

        protected override void OnEnable()
        {
            base.OnEnable();
            m_operator.OnChanged += Modified;
        }

        public static GenericComparisonCondition Create(GameObject target)
        {
            var condition = CreateInstance<GenericComparisonCondition>();
            condition.m_targetA = target;
            return condition;
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            // Prepare Property
            m_propertyA.Target = m_targetA;
            m_propertyB.Target = m_targetB;
        }

        protected override bool EvaluateCondition()
        {
            return m_operator.Evaluate(m_propertyA.Value, m_propertyB.Value);
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            try
            {
                m_propertyA.TrySetValue(m_operator.GetPotentialFirstValue(m_propertyB.Value));
            }
            catch(Exception e)
            {
                WeavrDebug.LogException(this, e);
            }
        }

        public override string GetDescription()
        {
            string targetA = !m_targetA ? "[ ? ].[ ? ]" :
                            string.IsNullOrEmpty(m_propertyA?.Path) ?
                            $"{m_targetA.name}.[ ? ]" : $"{m_targetA.name}.{m_propertyA.PropertyName}";

            string targetB = !m_targetB ? "[ ? ].[ ? ]" :
                            string.IsNullOrEmpty(m_propertyB?.Path) ?
                            $"{m_targetB.name}.[ ? ]" : $"{m_targetB.name}.{m_propertyB.PropertyName}";
            return $"{targetA} {m_operator} {targetB}";
        }

        public override string ToFullString()
        {
            return $"[{m_targetA} vs {m_targetB}]";
        }
    }
}