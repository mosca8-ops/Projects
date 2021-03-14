using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ObjectIsActiveCondition : BaseCondition, ITargetingObject
    {
        [SerializeField]
        [Tooltip("The target object to check")]
        [Draggable]
        public ValueProxyGameObject m_target;
        [SerializeField]
        [Tooltip("Whether it is active or not")]
        public ValueProxyBool m_isActive;

        public Object Target {
            get => m_target;
            set => m_target.Value = value is Component c ? c.gameObject : value is GameObject go ? go : m_target.Value;
        }

        public string TargetFieldName => nameof(m_target);

        protected override bool EvaluateCondition()
        {
            return m_target.Value.activeInHierarchy == m_isActive;
        }

        public override string GetDescription()
        {
            return m_target.ToString() + (m_isActive ? " is Active" : " is Not Active");
        }

        public override void ForceEvaluation()
        {
            base.ForceEvaluation();
            m_target.Value.SetActive(m_isActive);
        }

        public override string ToFullString()
        {
            return $"[{GetDescription()}]";
        }
    }
}
