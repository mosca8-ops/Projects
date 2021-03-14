using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public partial class BaseTransition : GraphObject, IFlowProvider
    {
        public const float k_DefaultPriority = 1;
        public enum ExecuteMode
        {
            Always,
            SameExecutionFlow,
            OnlyOnce,
        }

        [SerializeField]
        [Tooltip("Whether to always, only once or only for one execution flow")]
        protected ExecuteMode m_executionMode;
        [DoNotClone]
        [SerializeField]
        protected GraphObject m_from;
        [DoNotClone(cutReference: true)]
        [SerializeField]
        protected GraphObject m_to;
        [SerializeField]
        protected List<BaseAction> m_actions;

        [SerializeField]
        [HideInInspector]
        protected float m_weight = k_DefaultPriority;

        public virtual ITransitionOwner SourcePort { get; set; }

        public event Action<BaseTransition, BaseAction> ActionAdded;
        public event Action<BaseTransition, BaseAction> ActionRemoved;
        public event Action<BaseTransition, float> PriorityChanged;

        [NonSerialized]
        protected ExecutionFlow m_lastFlow;

        public virtual GraphObject From
        {
            get => m_from;
            set
            {
                if(m_from != value)
                {
                    BeginChange();
                    m_from = value;
                    PropertyChanged(nameof(From));
                    OnInputChanged();
                }
            }
        }

        public virtual GraphObject To
        {
            get => m_to;
            set
            {
                if (m_to != value)
                {
                    BeginChange();
                    m_to = value;
                    PropertyChanged(nameof(To));
                    OnOutputChanged();
                }
            }
        }

        public List<BaseAction> Actions => m_actions;

        public virtual float Priority
        {
            get => m_weight;
            set
            {
                if(m_weight != value)
                {
                    BeginChange();
                    m_weight = value;
                    PropertyChanged(nameof(Priority));
                    PriorityChanged?.Invoke(this, m_weight);
                }
            }
        }

        public virtual void ResetPriority() => Priority = k_DefaultPriority;

        private ContextState m_currentState;
        public ContextState CurrentState {
            get => m_currentState;
            set {
                if(m_currentState != value)
                {
                    m_currentState = value;
                    PropertyChanged(nameof(CurrentState));
                }
            }
        }

        public virtual ExecuteMode ExecuteType => m_executionMode;

        public virtual bool CanBeShared => Actions.Count > 0;

        public virtual bool CanExecute(ExecutionFlow flow) => m_executionMode == ExecuteMode.Always || !m_lastFlow || (m_executionMode == ExecuteMode.SameExecutionFlow && m_lastFlow == flow);

        protected override void OnEnable()
        {
            base.OnEnable();
            if(m_actions == null)
            {
                m_actions = new List<BaseAction>();
            }
            else
            {
                for (int i = 0; i < m_actions.Count; i++)
                {
                    if (!m_actions[i])
                    {
                        m_actions.RemoveAt(i--);
                        Debug.LogError($"[{name}]: Unable to identify a flow element and thus it was deleted.");
                    }
                }
            }
            CurrentState = ContextState.Standby;
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            foreach (var action in Actions)
            {
                if (action) { action.Procedure = value; }
            }
        }

        public virtual bool Contains(GraphObject obj)
        {
            if(obj == From || obj == To) { return true; }

            return obj is BaseAction && m_actions.Contains(obj as BaseAction);
        }

        public virtual void Add(BaseAction action)
        {
            if (m_actions.Contains(action)) { return; }

            m_actions.Add(action);
            ActionAdded?.Invoke(this, action);
            PropertyChanged(nameof(Actions));
        }

        public virtual void Remove(BaseAction action)
        {
            if (m_actions.Remove(action))
            {
                ActionRemoved?.Invoke(this, action);
                PropertyChanged(nameof(Actions));
            }
        }

        public virtual void Clear()
        {
            m_actions.Clear();
            PropertyChanged(nameof(Actions));
        }

        public List<IFlowElement> GetFlowElements()
        {
            List<IFlowElement> elements = new List<IFlowElement>();
            foreach(var action in m_actions)
            {
                elements.Add(action);
            }
            return elements;
        }

        protected virtual void OnInputChanged()
        {

        }

        protected virtual void OnOutputChanged()
        {

        }

        public void RemoveFromOwner()
        {
            SourcePort?.RemoveTransition(this);
        }

        public virtual void OnExecutionEnded(ExecutionFlow flow)
        {
            if (To is IFlowContext context && flow.NextContext != context)
            {
                if(!CanExecute(flow))
                {
                    flow.ExecutionEngine.StopExecutionFlow(flow);
                    return;
                }
                m_lastFlow = flow;
                flow.EnqueueContext(context);
            }
        }

        public virtual void OnExecutionStarted(ExecutionFlow flow)
        {

        }
    }
}
