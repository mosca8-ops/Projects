using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class ExecutionModeCondition : BaseCondition
    {
        [SerializeField]
        [Tooltip("Which is the current execution mode")]
        [ArrayElement(nameof(m_executionModes))]
        private ExecutionMode m_modeIs;
        [SerializeField]
        [HideInInspector]
        private List<ExecutionMode> m_executionModes;

        private bool m_isValid;

        protected override void OnEnable()
        {
            base.OnEnable();
            CanCacheValue = true;
            if (Procedure && m_executionModes.Count > 0) {
                for (int i = 0; i < m_executionModes.Count; i++)
                {
                    if (Procedure.ExecutionModes.Contains(m_executionModes[i]))
                    {
                        m_executionModes = new List<ExecutionMode>(Procedure.ExecutionModes);
                        if (!Procedure.ExecutionModes.Contains(m_modeIs))
                        {
                            m_modeIs = m_executionModes[0];
                        }
                        break;
                    }
                }
            }
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            m_executionModes = new List<ExecutionMode>(value.ExecutionModes);
            if(!m_modeIs || !m_executionModes.Contains(m_modeIs))
            {
                if(m_executionModes.Count > 0) { m_modeIs = m_executionModes[0]; }
            }
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_isValid = mode == m_modeIs;
        }

        protected override bool EvaluateCondition()
        {
            return m_isValid;
        }

        public override string GetDescription()
        {

            return $"Execution mode is " + (m_modeIs ? m_modeIs.ModeName : "none");
        }

        public override string ToFullString()
        {
            return $"[Mode: {(m_modeIs ? m_modeIs.ModeName : "none")}]";
        }
    }
}
