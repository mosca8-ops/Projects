using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class WaitTimeAction : BaseAction, IProgressElement, IReplayModeElement
    {
        [SerializeField]
        [Tooltip("The time to wait before going forward")]
        [AbsoluteValue]
        private ValueProxyFloat m_waitTime;

        public float WaitTime
        {
            get => m_waitTime;
            set
            {
                if(m_waitTime != value)
                {
                    BeginChange();
                    m_waitTime = Mathf.Abs(value);
                    PropertyChanged(nameof(WaitTime));
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
            m_remainingTime = m_waitTime;
            Progress = 0;
        }

        public override bool Execute(float dt)
        {
            m_remainingTime -= dt;
            Progress = m_waitTime > 0 ? (m_waitTime - m_remainingTime) / m_waitTime : 1;
            return m_remainingTime <= 0;
        }

        public override void OnStop()
        {
            base.OnStop();
            m_remainingTime = 0;
        }

        public override string GetDescription()
        {
            return $"Wait {m_waitTime} seconds";
        }

        public void ResetProgress()
        {
            Progress = 0;
        }
    }
}
