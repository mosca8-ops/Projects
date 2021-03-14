using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class DistanceCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The object A for the distance computation")]
        [Draggable]
        public ValueProxyTransform m_target;
        [SerializeField]
        [Tooltip("The maximum distance to make this condition true")]
        [AbsoluteValue]
        public ValueProxyFloat m_lessThan;
        [SerializeField]
        [Tooltip("The object B for the distance computation")]
        [Draggable]
        public ValueProxyTransform m_from;

        private float m_sqrDistance;

        public Object Target {
            get => m_target;
            set => m_target.Value = value is Component c ? c.transform : value is GameObject go ? go.transform : m_target.Value;
        }

        public string TargetFieldName => nameof(m_target);

        public override void OnValidate()
        {
            base.OnValidate();
            if(m_from == m_target)
            {
                m_from = null;
            }
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_sqrDistance = m_lessThan * m_lessThan;
        }

        protected override bool EvaluateCondition()
        {
            return (m_target.Value.position - m_from.Value.position).sqrMagnitude <= m_sqrDistance;
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            var target = m_target.Value;
            var from = m_from.Value;

            if (!target.gameObject.isStatic)
            {
                var line = target.position - from.position;
                target.position = from.position + line.normalized * m_lessThan;
            }
            else if (!from.gameObject.isStatic)
            {
                var line = from.position - target.position;
                from.position = target.position + line.normalized * m_lessThan;
            }
        }

        public override string GetDescription()
        {
            return $"{m_target} to {m_from} ≤ {m_lessThan} m";
        }

        public override string ToFullString()
        {
            return $"[{GetDescription()}]";
        }
    }
}
