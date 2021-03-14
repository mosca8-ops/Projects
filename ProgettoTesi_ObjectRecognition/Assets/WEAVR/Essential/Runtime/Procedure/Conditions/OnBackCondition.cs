using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class OnBackCondition : BaseCondition
    {
        private bool m_backWasPressed;
        private ProcedureRunner m_runner;

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_backWasPressed = false;
            m_runner = flow.ExecutionEngine as ProcedureRunner;
            if (m_runner)
            {
                m_runner.MovePreviousOverride -= MovePrevious;
                m_runner.MovePreviousOverride += MovePrevious;
            }
        }

        private void MovePrevious()
        {
            if (m_runner)
            {
                m_backWasPressed = true;
                m_runner.MovePreviousOverride -= MovePrevious;
            }
        }

        protected override bool EvaluateCondition()
        {
            return m_backWasPressed;
        }

        public override string GetDescription()
        {

            return $"Previous Clicked";
        }

        public override string ToFullString()
        {
            return $"[Previous Clicked]";
        }
    }
}
