using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class TrafficNode : BaseNode, ITransitionOwner
    {
        [SerializeField]
        private List<BaseTransition> m_outputTransitions;
        [SerializeField]
        private int m_inputTransitions;
        [SerializeField]
        private bool m_endIncomingFlows = true;
        [System.NonSerialized]
        private int? m_transitionsToWait;

        public event OnExecutionStateChanged StateChanged;

        public List<BaseTransition> OutputTransitions => m_outputTransitions;

        [NonSerialized]
        private List<ExecutionFlow> m_incomingFlows;
        [NonSerialized]
        private List<BaseTransition> m_transitionsToBeHandled;

        public int InputTransitionsCount
        {
            get => m_inputTransitions;
            set
            {
                if (m_inputTransitions != value)
                {
                    BeginChange();
                    m_inputTransitions = value;
                    PropertyChanged(nameof(InputTransitionsCount));
                }
            }
        }

        public int TransitionsToWait
        {
            get => m_transitionsToWait ?? InputTransitionsCount;
            set
            {
                if (m_transitionsToWait != value)
                {
                    m_transitionsToWait = value;
                    PropertyChanged(nameof(TransitionsToWait));
                }
            }
        }

        public bool EndIncomingFlows => m_endIncomingFlows && InputTransitionsCount > 1;

        //ExecutionState IFlowElement.CurrentState { get; set; }
        public string ErrorMessage { get; set; }
        public Exception Exception { get; set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_outputTransitions == null)
            {
                m_outputTransitions = new List<BaseTransition>();
            }
            for (int i = 0; i < m_outputTransitions.Count; i++)
            {
                var transition = m_outputTransitions[i];
                if (!transition)
                {
                    m_outputTransitions.RemoveAt(i--);
                }
                else
                {
                    transition.SourcePort = this;
                }
            }
            m_transitionsToWait = null;

            m_incomingFlows = new List<ExecutionFlow>();
            m_transitionsToBeHandled = new List<BaseTransition>();
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            foreach (var transition in m_outputTransitions)
            {
                if (transition)
                {
                    transition.CollectProcedureObjects(list);
                }
            }
        }

        public override ProcedureObject Clone()
        {
            var clone = base.Clone() as TrafficNode;
            if (clone == null) { return clone; }

            for (int i = 0; i < clone.m_outputTransitions.Count; i++)
            {
                clone.m_outputTransitions[i] = clone.m_outputTransitions[i].Clone() as BaseTransition;
            }

            return clone;
        }

        public void Reset()
        {
            m_transitionsToWait = null;
            m_currentState = ContextState.Standby;
        }

        public override void OnExecutionStarted(ExecutionFlow flow)
        {
            base.OnExecutionStarted(flow);
            if (!m_transitionsToWait.HasValue)
            {
                TransitionsToWait = InputTransitionsCount > 0 ? InputTransitionsCount : 1;
                m_incomingFlows.Clear();
                m_transitionsToBeHandled.Clear();
                m_transitionsToBeHandled.AddRange(OutputTransitions);
            }
            m_incomingFlows.Add(flow);
            TransitionsToWait--;
        }

        public override void OnExecutionEnded(ExecutionFlow currentFlow)
        {
            base.OnExecutionEnded(currentFlow);

            if (TransitionsToWait == 0 && OutputTransitions.Count > 0 && m_transitionsToWait.HasValue)
            {
                currentFlow.AutoAdvance = false;
                currentFlow.Pause();

                if (m_transitionsToBeHandled.Count > 0)
                {
                    currentFlow.EnqueueContext(m_transitionsToBeHandled[0]);
                    m_transitionsToBeHandled.RemoveAt(0);
                }

                if (m_transitionsToBeHandled.Count > 0 && !currentFlow.ExecutionEngine.IsLocked)
                {
                    // Register new flows
                    ExecutionFlow[] newFlows = new ExecutionFlow[m_transitionsToBeHandled.Count];
                    for (int i = 0; i < m_transitionsToBeHandled.Count; i++)
                    {
                        var newFlow = currentFlow.ExecutionEngine.CreateExecutionFlow(currentFlow.IsPrimaryFlow);
                        newFlow.ExecutionMode = currentFlow.ExecutionMode;
                        newFlow.IsPrimaryFlow = currentFlow.IsPrimaryFlow;
                        newFlow.SetStartContext(m_transitionsToBeHandled[i]);
                        newFlows[i] = newFlow;
                    }

                    // Unleash new flows
                    foreach (var flow in newFlows)
                    {
                        currentFlow.ExecutionEngine.StartExecutionFlow(flow);
                    }
                }

                // Resume already existing flows
                foreach (var flow in m_incomingFlows)
                {
                    flow.Resume();
                    flow.AutoAdvance = true;
                }

                m_transitionsToWait = null;
                m_incomingFlows.Clear();
                m_transitionsToBeHandled.Clear();
            }
            else if (m_endIncomingFlows && m_incomingFlows.Remove(currentFlow))
            {
                return;
            }
            else
            {
                currentFlow.AutoAdvance = false;
                currentFlow.Pause();

                if (m_transitionsToBeHandled.Count > 0)
                {
                    currentFlow.EnqueueContext(m_transitionsToBeHandled[0]);
                    m_transitionsToBeHandled.RemoveAt(0);
                }
            }

            // Reset the state to allow other flows to end their execution
            //flow.ExecutionEngine.KillExecutionFlow(flow);
        }

        public void RemoveTransition(BaseTransition transition)
        {
            if (m_outputTransitions != null && m_outputTransitions.Contains(transition))
            {
                BeginChange();
                m_outputTransitions.Remove(transition);
                PropertyChanged(nameof(OutputTransitions));
            }
        }
    }
}
