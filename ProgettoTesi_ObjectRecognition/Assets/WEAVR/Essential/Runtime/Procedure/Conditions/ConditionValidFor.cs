using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TXT.WEAVR.Procedure
{

    public class ConditionValidFor : BaseCondition, IConditionParent, IProgressElement
    {
        [SerializeField]
        [Tooltip("For how long the condition should be valid for this condition to be true")]
        [FormerlySerializedAs("m_validFor")]
        private TXT.WEAVR.Common.ValueProxyFloat m_validationTime = 1;
        [SerializeField]
        [Tooltip("If Progressive is False than this condition will reset its progress to 0 when child is false")]
        private bool m_progressive = false;

        [SerializeField]
        private BaseCondition m_condition;

        [NonSerialized]
        private float m_timeProgress;

        public BaseCondition Condition
        {
            get { return m_condition; }
            set
            {
                if (m_condition != value)
                {
                    BeginChange();
                    if(m_condition != null && Equals(m_condition.Parent))
                    {
                        m_condition.Parent = null;
                    }
                    m_condition = value;
                    if (m_condition)
                    {
                        m_condition.Parent = this;
                    }
                    PropertyChanged(nameof(Condition));
                }
            }
        }

        public float ValidationTime
        {
            get { return m_validationTime; }
            set
            {
                if(m_validationTime != value)
                {
                    BeginChange();
                    m_validationTime = Mathf.Abs(value);
                    PropertyChanged(nameof(ValidationTime));
                }
            }
        }

        public bool IsProgressive
        {
            get => m_progressive;
            set
            {
                if(m_progressive != value)
                {
                    BeginChange();
                    m_progressive = value;
                    PropertyChanged(nameof(IsProgressive));
                }
            }
        }

        public BaseCondition Child { get => Condition; set => Condition = value; }

        public float Progress { get; private set; } = 0;

        protected override void OnEnable()
        {
            base.OnEnable();
            if(Condition != null)
            {
                Condition.Parent = this;
            }
        }

        public void ChildChangedParent(BaseCondition child, IEvaluationNode newParent)
        {
            if (Condition == child)
            {
                Condition = null;
            }
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_timeProgress = 0;

            Modified();
        }

        protected override bool EvaluateCondition()
        {
            if (m_condition == null) { return true; }
            if (m_validationTime <= 0)
            {
                return m_condition.Value;
            }
            return Application.isEditor ? EvaluateEditor() : EvaluateRuntime();
        }

        private bool EvaluateEditor()
        {
            bool value = EvaluateRuntime();

            Progress = value || m_progressive ? m_timeProgress / m_validationTime : 0;

            return value;
        }

        private bool EvaluateRuntime()
        {
            bool value = m_condition.Value;
            if (value)
            {
                if(m_timeProgress >= m_validationTime)
                {
                    return value;
                }
                else
                {
                    m_timeProgress = Mathf.Clamp(0, m_timeProgress + Time.deltaTime, m_validationTime);
                }
            }
            else if (m_progressive)
            {
                m_timeProgress = Mathf.Clamp(0, m_timeProgress - Time.deltaTime, m_validationTime);
            }
            else
            {
                m_timeProgress = 0;
            }
            return false;
        }

        public override string GetDescription()
        {
            return Child ? $"{Child.GetDescription()} for {m_validationTime} seconds" : "No child to validate";
        }

        public override string ToFullString()
        {
            return Child ? $"[{Child.ToFullString()} for {m_validationTime}s]" : base.ToFullString();
        }

        public void ResetProgress()
        {
            Progress = 0;
        }
    }
}
