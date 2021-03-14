using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class FlowConditionsContainer : GraphObject, IFlowElement, INetworkProcedureObjectsContainer
    {
        private const ExecutionState k_HandleExecutionEndedState = ExecutionState.ForceStopped 
                                                                 | ExecutionState.Skipped 
                                                                 | ExecutionState.Finished;

        [SerializeField]
        private List<FlowCondition> m_flowConditions;

        public List<FlowCondition> Conditions
        {
            get {
                if (m_flowConditions == null)
                {
                    m_flowConditions = new List<FlowCondition>();
                }
                return m_flowConditions;
            }
        }
        
        private ExecutionState m_state = ExecutionState.NotStarted;

        public ExecutionState CurrentState
        {
            get { return m_state; }
            set
            {
                if (m_state != value)
                {
                    m_state = value;
                    StateChanged?.Invoke(this, value);
                    PropertyChanged(nameof(CurrentState));
                    if((m_state & k_HandleExecutionEndedState) != 0 && m_evaluationEndedNodes?.Count > 0)
                    {
                        foreach (var client in m_evaluationEndedNodes)
                        {
                            client.NodesEvaluationEnded();
                        }
                    }
                }
            }
        }

        [System.NonSerialized]
        private string m_errorMessage;
        public string ErrorMessage
        {
            get => m_errorMessage;
            set
            {
                if(m_errorMessage != value)
                {
                    m_errorMessage = value;
                    PropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        public Exception Exception { get; set; }

        public IEnumerable<INetworkProcedureObject> NetworkObjects
        {
            get
            {
                List<INetworkProcedureObject> networkObjects = new List<INetworkProcedureObject>();
                foreach (var condition in m_flowConditions)
                {
                    networkObjects.AddRange(condition.GetGlobalConditions());
                }
                return networkObjects;
            }
        }

        public event OnExecutionStateChanged StateChanged;

        private HashSet<IEvaluationNode> m_nodesToEvaluate;
        private HashSet<IEvaluationEndedCallback> m_evaluationEndedNodes;
        
        [System.NonSerialized]
        private ExecutionFlow m_currentFlow;
        
        //[System.NonSerialized]
        //private bool m_blockEmptyConditions;

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_flowConditions == null)
            {
                m_flowConditions = new List<FlowCondition>();
            }
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            foreach(var condition in m_flowConditions)
            {
                condition.CollectProcedureObjects(list);
            }
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            foreach (var condition in Conditions)
            {
                if (condition) { condition.Procedure = value; }
            }
        }

        public bool Execute(float dt)
        {
            foreach(var node in m_nodesToEvaluate)
            {
                node.Evaluate();
            }
            foreach(var condition in m_flowConditions)
            {
                if (condition.Value)
                {
                    condition.Triggered = true;
                    if (condition.Transition)
                    {
                        condition.OnEvaluationEnded();
                        m_currentFlow.EnqueueContext(condition.Transition);
                        return true;
                    }
                    else
                    {
                        bool noOutputTransitions = true;
                        foreach(var c in m_flowConditions)
                        {
                            if (c.Transition)
                            {
                                noOutputTransitions = false;
                                break;
                            }
                        }
                        if (noOutputTransitions) { return true; }
                    }
                }
            }

            return m_flowConditions.Count == 0;
        }

        public void OnStart(ExecutionFlow flow, ExecutionMode executionMode)
        {
            m_currentFlow = flow;
            if(m_nodesToEvaluate == null)
            {
                m_nodesToEvaluate = new HashSet<IEvaluationNode>();
            }
            else
            {
                m_nodesToEvaluate.Clear();
            }
            if(m_evaluationEndedNodes == null)
            {
                m_evaluationEndedNodes = new HashSet<IEvaluationEndedCallback>();
            }
            else
            {
                m_evaluationEndedNodes.Clear();
            }
            
            foreach(var condition in Conditions)
            {
                condition.Triggered = false;
                condition.CollectNodesToEvaluate(flow, executionMode, m_nodesToEvaluate);
            }

            foreach (var node in m_nodesToEvaluate)
            {
                //node.PrepareForEvaluation(flow, executionMode);
                if(node is IEvaluationEndedCallback callbackClient)
                {
                    m_evaluationEndedNodes.Add(callbackClient);
                }
            }
        }

        public void ResetEvaluations()
        {
            foreach (var condition in m_flowConditions)
            {
                if (condition) { condition.ResetEvaluation(); }
            }
        }

        public void OnStop()
        {
            foreach (var client in m_evaluationEndedNodes)
            {
                client.NodesEvaluationEnded();
            }
            foreach (var node in m_nodesToEvaluate)
            {
                node.OnEvaluationEnded();
            }
        }

        private bool HandleEvaluationEndedClients()
        {
            if(m_evaluationEndedNodes.Count == 0) { return true; }
            foreach(var client in m_evaluationEndedNodes)
            {
                client.NodesEvaluationEnded();
            }
            return true;
        }

        public bool CanExecute(ExecutionMode executionMode)
        {
            return true;
        }

        public void OnPause()
        {
            
        }

        public void OnResume()
        {
            
        }

        public void FastForward()
        {
            foreach(var condition in m_flowConditions)
            {
                if(condition.Transition && m_currentFlow.IsNextInQueue(condition.Transition))
                {
                    OnStop();
                    if (!condition.Value)
                    {
                        condition.ForceEvaluation();
                    }
                    condition.Triggered = true;
                    condition.OnEvaluationEnded();
                    return;
                }
            }
            foreach (var condition in m_flowConditions)
            {
                if (condition.Transition && condition.Value)
                {
                    OnStop();
                    condition.Triggered = true;
                    m_currentFlow.EnqueueContext(condition.Transition);
                    condition.OnEvaluationEnded();
                    return;
                }
            }
            foreach (var condition in m_flowConditions)
            {
                if (condition.Transition)
                {
                    OnStop();
                    condition.Triggered = true;
                    m_currentFlow.EnqueueContext(condition.Transition);
                    condition.OnEvaluationEnded();
                    return;
                }
            }
        }
    }
}
