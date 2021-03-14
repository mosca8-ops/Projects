using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class StopAsyncAction : BaseAction, IParameterlessAction, IReplayModeElement
    {
        private ExecutionFlow m_currentFlow;

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_currentFlow = flow;
        }

        public override bool Execute(float dt)
        {
            m_currentFlow.StopAllAsync();
            return true;
        }
        

        public override string GetDescription()
        {
            return $"Stop all async actions";
        }
    }
}
