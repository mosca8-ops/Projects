using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class NormalAction : BaseAction, IProgressElement, IFlowContextClosedElement
    {
        [SerializeField]
        [HideInInspector]
        private bool m_revertOnExit;

        [SerializeField]
        [HideInInspector]
        private float m_duration = 0.001f;

        public bool RevertOnExit
        {
            get => m_revertOnExit;
            set
            {
                if (m_revertOnExit != value)
                {
                    BeginChange();
                    m_revertOnExit = value;
                    PropertyChanged(nameof(RevertOnExit));
                }
            }
        }

        public float Duration
        {
            get => m_duration;
            protected set
            {
                value = value < 0 ? 0 : value;
                if(m_duration != value)
                {
                    BeginChange();
                    m_duration = value;
                    PropertyChanged(nameof(Duration));
                }
            }
        }

        [NonSerialized]
        private float m_timeProgress;
        public float CurrentTimeProgress
        {
            get => m_timeProgress;
            private set
            {
                value = Mathf.Clamp(value, 0, Duration);
                if(m_timeProgress != value)
                {
                    m_timeProgress = value;
                    Progress = Mathf.Clamp01(value / m_duration);
                }
            }
        }

        public float Progress { get; private set; }
        protected ExecutionFlow CurrentFlow { get; private set; }
        protected ExecutionMode CurrentMode { get; private set; }
        protected float? RevertDuration { get; set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetProgress();
        }

        public sealed override void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            base.OnStart(flow, executionMode);
            CurrentFlow = flow;
            CurrentMode = executionMode;
            CurrentTimeProgress = 0;
            OnBegin();
        }

        public void OnContextExit(ExecutionFlow flow)
        {
            CurrentFlow = flow;
            if (RevertDuration.HasValue)
            {
                flow.StartCoroutine(SmoothRevert(RevertDuration.Value));
            }
            else
            {
                Process(0);
            }
        }

        private IEnumerator SmoothRevert(float startingFrom)
        {
            float currentTime = startingFrom;
            while(currentTime >= 0)
            {
                currentTime -= Time.deltaTime;
                Process(Mathf.Clamp01(currentTime / startingFrom));
                yield return null;
            }
        }

        public override void FastForward()
        {
            base.FastForward();
            CurrentTimeProgress = Duration;
            Process(1);
        }

        public virtual void ResetProgress()
        {
            Progress = 0;
        }

        public sealed override bool Execute(float dt)
        {
            CurrentTimeProgress += dt;
            Process(Progress);
            return Progress >= 1;
        }

        /// <summary>
        /// Apply the action processing with a normalized value (0 -> standby, 1 -> execution ended).
        /// Should be implemented by derived classes as the main execution method
        /// </summary>
        /// <param name="normalizedValue">The normalization value to use for processing (0 -> standby, 1 -> execution ended)</param>
        protected abstract void Process(float normalizedValue);

        /// <summary>
        /// The very first execution of the action
        /// </summary>
        protected virtual void OnBegin() { }
    }
}
