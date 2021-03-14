using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class WaitAsyncAction : BaseAction, IProgressElement, IReplayModeElement
    {
        public enum EndAction
        {
            Continue,
            StopAndContinue,
        }

        [SerializeField]
        [AbsoluteValue]
        [Tooltip("If there are still async actions waiting after specified time in seconds, stop them")]
        private float m_timeout = 30;
        [SerializeField]
        [Tooltip("The action to perform when the timeout has expired")]
        private EndAction m_onTimeout = EndAction.StopAndContinue;

        private ExecutionFlow m_currentFlow;

        public float StopAfterSeconds
        {
            get => m_timeout;
            set
            {
                if (m_timeout != value)
                {
                    BeginChange();
                    m_timeout = Mathf.Abs(value);
                    PropertyChanged(nameof(StopAfterSeconds));
                }
            }
        }

        public EndAction OnTimeout
        {
            get => m_onTimeout;
            set
            {
                if(m_onTimeout != value)
                {
                    BeginChange();
                    m_onTimeout = value;
                    PropertyChanged(nameof(OnTimeout));
                }
            }
        }

        public float Progress { get; private set; } = 0;

        private float m_remainingTime;

        protected override void OnEnable()
        {
            base.OnEnable();
            Progress = 0;
        }

        public override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            m_currentFlow = flow;
            m_remainingTime = m_timeout;
            Progress = 0;
        }

        public override bool Execute(float dt)
        {
            if (m_currentFlow.FullyAsyncElements.Count == 0 
                || (AsyncThread != 0 
                    && m_currentFlow.FullyAsyncElements.Count == 1 
                    && Equals(m_currentFlow.FullyAsyncElements[0]))) { return true; }
            m_remainingTime -= dt;
            Progress = m_timeout > 0 ? (m_timeout - m_remainingTime) / m_timeout : 1;
            if(m_remainingTime <= 0)
            {
                if (m_onTimeout == EndAction.StopAndContinue)
                {
                    m_currentFlow.StopAllAsync();
                }
                return true;
            }
            return false;
        }

        public override void OnStop()
        {
            base.OnStop();
            m_remainingTime = 0;
        }

        public override string GetDescription()
        {
            return $"Wait async actions. On timeout " + (m_onTimeout == EndAction.StopAndContinue ? "stop and continue" : "continue");
        }

        public void ResetProgress()
        {
            Progress = 0;
        }
    }
}
