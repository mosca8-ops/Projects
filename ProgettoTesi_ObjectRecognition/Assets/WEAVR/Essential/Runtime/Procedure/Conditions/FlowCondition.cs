using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class FlowCondition : BaseCondition, IConditionParent, ITransitionOwner, INetworkProcedureObjectsContainer
    {
        [SerializeField]
        private BaseTransition m_transition;

        [SerializeField]
        private BaseCondition m_condition;
        
        [System.NonSerialized]
        private bool m_canContinue;

        [System.NonSerialized]
        private ProcedureRunner m_runner;

        public BaseTransition Transition
        {
            get => m_transition;
            set
            {
                if(m_transition != value)
                {
                    BeginChange();
                    m_transition = value;
                    PropertyChanged(nameof(Transition));
                }
            }
        }

        [System.NonSerialized]
        private bool m_triggered;
        public bool Triggered
        {
            get => m_triggered;
            set
            {
                if(m_triggered != value)
                {
                    m_triggered = value;
                    PropertyChanged(nameof(Triggered));
                }
            }
        }

        public BaseCondition Condition
        {
            get { return m_condition; }
            set
            {
                if(m_condition != value)
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

        public BaseCondition Child { get => Condition; set => Condition = value; }

        private GenericNode m_node;
        public GenericNode Node
        {
            get
            {
                if(!m_node && Procedure && Application.isEditor)
                {
                    m_node = Procedure?.Graph.Nodes
                                 .Find(n => n is GenericNode node && node.FlowElements
                                        .Find(o => o is FlowConditionsContainer fc && fc.Conditions.Contains(this))) as GenericNode;
                    if (m_node)
                    {
                        m_node.OnPropertyChanged += Node_OnPropertyChanged;
                    }
                }
                return m_node;
            }
        }

        public IEnumerable<INetworkProcedureObject> NetworkObjects => GetGlobalConditions();

        protected override void OnEnable()
        {
            base.OnEnable();
            CanCacheValue = true;
            m_triggered = false;
            if (Condition)
            {
                Condition.Parent = this;
            }
            RefreshTransitions();

            if (Procedure)
            {
                Procedure.Configuration.ExecutionModeChanged -= Configuration_ExecutionModeChanged;
                Procedure.Configuration.ExecutionModeChanged += Configuration_ExecutionModeChanged;
            }
        }

        public List<BaseCondition> GetGlobalConditions()
        {
            List<BaseCondition> conditions = new List<BaseCondition>();
            GetGlobalConditionsRecursive(Child, conditions);
            return conditions;
        }

        private void GetGlobalConditionsRecursive(BaseCondition condition, List<BaseCondition> list)
        {
            if (!condition) { return; }

            if(condition.CanBeShared && condition.IsGlobal)
            {
                list.Add(condition);
            }

            if(condition is IConditionsContainer container)
            {
                foreach(var child in container.Children)
                {
                    GetGlobalConditionsRecursive(child, list);
                }
            }
            else if(condition is IConditionParent parent)
            {
                GetGlobalConditionsRecursive(parent.Child, list);
            }
        }

        private void Configuration_ExecutionModeChanged(ExecutionMode obj)
        {
            Modified();
        }

        private void Node_OnPropertyChanged(ProcedureObject node, string property)
        {
            if(property == nameof(GenericNode.IsMandatory))
            {
                Modified();
            }
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            m_transition?.CollectProcedureObjects(list);
        }

        public void RefreshTransitions()
        {
            if (m_transition)
            {
                m_transition.SourcePort = this;
            }
        }

        public void ChildChangedParent(BaseCondition child, IEvaluationNode newParent)
        {
            if(Condition == child)
            {
                Condition = null;
            }
        }

        public override void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            base.PrepareForEvaluation(flow, mode);
            m_canContinue = !m_condition;
            m_runner = null;
            if (mode.RequiresNextToContinue)
            {
                bool isEmpty = !m_condition || (m_condition is IConditionParent p && !p.Child);
                if(isEmpty && flow.CurrentContext is IProcedureStep step 
                    && step.IsMandatory && flow.ExecutionEngine is ProcedureRunner runner)
                {
                    m_canContinue = false;
                    m_runner = runner;
                    runner.MoveNextOverride = OnMoveNext;
                }
            }
        }

        private void OnMoveNext()
        {
            m_canContinue = true;
            m_cachedValue = true;
            if (m_runner)
            {
                m_runner.MoveNextOverride = null;
            }
        }

        public override void OnEvaluationEnded()
        {
            base.OnEvaluationEnded();
            if (m_runner && m_runner.MoveNextOverride == OnMoveNext)
            {
                m_runner.MoveNextOverride = null;
            }
        }

        protected override bool EvaluateCondition()
        {
            return m_canContinue || (m_condition != null && m_condition.Value);
        }

        public override string GetDescription()
        {
            var childContainer = Condition as IConditionsContainer;
            return Condition == null ?
                    (Node && m_node.IsMandatory && Procedure && Procedure.DefaultExecutionMode.RequiresNextToContinue ? "Next Clicked" : "Continue") :
                    childContainer == null ?
                        Condition.GetDescription() :
                        childContainer.Children.Count > 1 ?
                        $"[{childContainer.Children.Count} Conditions]" :
                            childContainer.Children.Count > 0 ?
                            childContainer.Children[0].GetDescription() : "Continue without condition";
        }

        public void RemoveTransition(BaseTransition transition)
        {
            if (Transition == transition)
            {
                Transition = null;
            }
        }
    }
}
