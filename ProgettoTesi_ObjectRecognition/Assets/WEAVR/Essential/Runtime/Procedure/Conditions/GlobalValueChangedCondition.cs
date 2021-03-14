using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class GlobalValueChangedCondition : BaseCondition
    {
        [SerializeField]
        [Tooltip("The variable to check the if its value changed")]
        private string m_variable;

        [System.NonSerialized]
        private object m_lastValue;
        [System.NonSerialized]
        private ValuesStorage.Variable m_var;

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_var = GlobalValues.Current.GetVariable(m_variable);
            m_lastValue = m_var?.Value;
        }

        protected override bool EvaluateCondition()
        {
            if(m_var == null)
            {
                m_var = GlobalValues.Current.GetVariable(m_variable);
                m_lastValue = m_var?.Value;
                return false;
            }
            return !Equals(m_lastValue, m_var?.Value);
        }

        public override string GetDescription()
        {
            return $"[{m_variable}] changed value";
        }

        public override string ToFullString()
        {
            return $"[{m_variable}] changed";
        }
    }
}
