using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Procedure;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Common
{

    [AddComponentMenu("WEAVR/Components/Conditional")]
    public class ConditionalComponent : MonoBehaviour, IEvaluationNode
    {
        [SerializeField]
        private BaseCondition m_condition;

        [SerializeField]
        //[Button(nameof(ResetColors), "Reset Colors", width: 120)]
        [DoNotExpose]
        [Tooltip("Evaluate automatically, if false, evaluation is triggered manually")]
        private bool m_autoEvaluate = true;
        [SerializeField]
        [Tooltip("Whether to raise events continuously on positive evaluation or not")]
        private bool m_raiseEventsContinuously = false;
        [SerializeField]
        [Tooltip("When evaluated to true, stop the evaluation")]
        [DisabledBy(nameof(m_raiseEventsContinuously), disableWhenTrue: true)]
        private bool m_stopOnEvaluation = false;
        [SerializeField]
        [DoNotExpose]
        private UnityEvent m_onEvaluated;
        [SerializeField]
        [DoNotExpose]
        private UnityEventFloat m_onContinuousEvaluation;

        [NonSerialized]
        private bool m_isReadyForEvaluation;
        [NonSerialized]
        private bool m_previousEvaluation;
        [NonSerialized]
        private float m_firstEvaluationTime;
        [NonSerialized]
        private float m_evaluationDeadline;

        private bool m_canEvaluate;

        public bool AutoEvaluate
        {
            get => m_autoEvaluate;
            set
            {
                if(m_autoEvaluate != value)
                {
                    m_autoEvaluate = value;
                }
            }
        }

        public bool CanEvaluate
        {
            get => m_canEvaluate;
            set
            {
                if(m_canEvaluate != value)
                {
                    m_canEvaluate = value;
                    if (value)
                    {
                        InitializeEvaluation();
                    }
                    else
                    {
                        StopEvaluation();
                    }
                }
            }
        }

        private void ResetColors()
        {
            if (m_condition)
            {
                m_condition.ResetEvaluation();
            }
        }

        public void Evaluate()
        {
            var canEvaluate = CanEvaluate;
            CanEvaluate = true;
            Execute(Time.deltaTime);
            CanEvaluate &= canEvaluate;
        }

        public void Evaluate(float duration)
        {
            CanEvaluate = true;
            m_evaluationDeadline = Time.time + duration;
        }
        
        private HashSet<IEvaluationNode> m_nodesToEvaluate;
        private HashSet<IEvaluationEndedCallback> m_evaluationEndedNodes;

        private void Reset()
        {
            if (!m_condition)
            {
                m_condition = ProcedureObject.Create<ConditionAnd>(null);
            }
            m_condition.Parent = this;
        }

        private void OnValidate()
        {
            if (m_condition && !Equals(m_condition.Parent, null) && !Equals(m_condition.Parent, this))
            {
                m_condition = m_condition.CloneTree();
            }
            m_condition.Parent = this;
        }

        protected virtual void OnEnable()
        {
            m_evaluationDeadline = float.MinValue;
        }

        private void Start()
        {
            m_canEvaluate = m_autoEvaluate;
        }

        private void Update()
        {
            if (m_autoEvaluate || Time.time < m_evaluationDeadline)
            {
                Execute(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            StopEvaluation();
        }

        private void Execute(float dt)
        {
            if (!m_canEvaluate) { return; }

            if (!m_isReadyForEvaluation)
            {
                InitializeEvaluation();
            }

            foreach (var node in m_nodesToEvaluate)
            {
                node.Evaluate();
            }

            bool currentEvaluation = m_condition.Value;
            if(currentEvaluation && !m_previousEvaluation)
            {
                m_firstEvaluationTime = Time.time;
                m_onEvaluated.Invoke();
            }
            m_previousEvaluation = currentEvaluation;
            if(currentEvaluation && m_raiseEventsContinuously)
            {
                m_onContinuousEvaluation.Invoke(Time.time - m_firstEvaluationTime);
            }
            if(currentEvaluation && m_stopOnEvaluation)
            {
                CanEvaluate = false;
            }
        }

        public void InitializeEvaluation()
        {
            m_previousEvaluation = false;
            if (m_nodesToEvaluate == null)
            {
                m_nodesToEvaluate = new HashSet<IEvaluationNode>();
            }
            else
            {
                m_nodesToEvaluate.Clear();
            }
            if (m_evaluationEndedNodes == null)
            {
                m_evaluationEndedNodes = new HashSet<IEvaluationEndedCallback>();
            }
            else
            {
                m_evaluationEndedNodes.Clear();
            }
            
            m_condition.CollectNodesToEvaluate(null, null, m_nodesToEvaluate);

            foreach (var node in m_nodesToEvaluate)
            {
                if (node is IEvaluationEndedCallback callbackClient)
                {
                    m_evaluationEndedNodes.Add(callbackClient);
                }
            }

            m_isReadyForEvaluation = true;
        }

        public void ResetEvaluations()
        {
            if (m_condition) { m_condition.ResetEvaluation(); }
            m_previousEvaluation = false;
        }

        public void StopEvaluation()
        {
            foreach (var client in m_evaluationEndedNodes)
            {
                if (client != null)
                {
                    client.NodesEvaluationEnded();
                }
            }
            foreach (var node in m_nodesToEvaluate)
            {
                if (node != null)
                {
                    node.OnEvaluationEnded();
                }
            }
            m_isReadyForEvaluation = false;
            m_previousEvaluation = false;
            m_evaluationDeadline = float.MinValue;

            //ResetColors();
        }

        public void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode) { }

        bool IEvaluationNode.Evaluate() => m_condition.Value;

        void IEvaluationNode.Reset()
        {

        }

        public void OnEvaluationEnded()
        {
        }
    }
}
