using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class BaseCondition : ProcedureObject, IEvaluationNode, IRequiresValidation, INetworkProcedureObject
    {
        public static Func<BaseCondition, BaseCondition> s_Clone;
        public event Action<BaseCondition, bool> EvaluationChanged;
        public event Action<BaseCondition, bool> LocalEvaluationChanged;

        private int m_evaluationFrame;
        protected bool? m_cachedValue;
        private IEvaluationNode m_parent;

        [SerializeField]
        private int m_variant;

        [SerializeField]
        private bool m_canCacheValue = false;

        [SerializeField]
        private bool m_isGlobal = true;

        [SerializeField]
        private bool m_isNegated;

        public int Variant
        {
            get => m_variant;
            set
            {
                if (m_variant != value)
                {
                    BeginChange();
                    m_variant = value;
                    PropertyChanged(nameof(Variant));
                }
            }
        }

        public bool IsGlobal => m_isGlobal;

        private bool? m_networkValue;
        public bool? NetworkValue {
            get => m_networkValue;
            set
            {
                if(m_networkValue != value)
                {
                    m_networkValue = value;
                    if (value.HasValue/* && Value != value*/)
                    {
                        EvaluationChanged?.Invoke(this, value.Value);
                        //Value = value.Value;
                    }
                }
            }
        }

        public virtual bool CanBeShared => true;

        public bool CanCacheValue
        {
            get { return m_canCacheValue; }
            set
            {
                if(m_canCacheValue != value)
                {
                    BeginChange();
                    m_canCacheValue = value;
                    PropertyChanged(nameof(CanCacheValue));
                }
            }
        }

        public bool IsNegated
        {
            get { return m_isNegated; }
            set
            {
                if(m_isNegated != value)
                {
                    BeginChange();
                    m_isNegated = value;
                    PropertyChanged(nameof(IsNegated));
                }
            }
        }

        /// <summary>
        /// Gets the value of the current condition evaluation
        /// </summary>
        public bool Value
        {
            get
            {
                //if (m_networkValue.HasValue)
                //{
                //    return m_networkValue.Value ^ IsNegated;
                //}
                if (!m_cachedValue.HasValue || (!m_canCacheValue && m_evaluationFrame != Time.frameCount))
                {
                    bool value = (NetworkValue == true) || EvaluateCondition();
                    if (value != m_cachedValue)
                    {
                        m_evaluationFrame = Time.frameCount;
                        m_cachedValue = value;
                        OnEvaluationChange(value);
                    }
                }
                return m_cachedValue.Value ^ IsNegated;
            }
            protected set
            {
                if (value != m_cachedValue)
                {
                    m_cachedValue = value;
                    m_evaluationFrame = Time.frameCount;
                    OnEvaluationChange(value);
                    if (Parent != null)
                    {
                        Parent.Reset();
                        Parent.Evaluate();
                    }
                }
            }
        }

        private void OnEvaluationChange(bool value)
        {
            EvaluationChanged?.Invoke(this, value);
            if (m_networkValue != value)
            {
                LocalEvaluationChanged?.Invoke(this, value);
            }
            //m_networkValue = value;
        }

        public bool? CachedValue => m_cachedValue;

        public bool Evaluate()
        {
            return m_canCacheValue ? Value : Value = EvaluateAndGetNewValue() ^ IsNegated;
        }

        private bool EvaluateAndGetNewValue()
        {
            return NetworkValue == true || EvaluateCondition();
        }

        public virtual void Reset()
        {
            if (m_cachedValue.HasValue)
            {
                m_networkValue = null;
            }
            m_cachedValue = null;
            if(Parent != null)
            {
                Parent.Reset();
            }
        }

        internal void ResetEvaluation()
        {
            if (this is IConditionsContainer cp && cp.Children.Count > 0)
            {
                foreach (var c in cp.Children)
                {
                    if (c) { c.ResetEvaluation(); }
                }
            }
            else if (this is IConditionParent p && p.Child)
            {
                p.Child.ResetEvaluation();
            }
            else
            {
                if (m_cachedValue.HasValue)
                {
                    m_networkValue = null;
                }
                m_cachedValue = false;
                //m_networkValue = null;
            }
        }

        public virtual void ForceEvaluation()
        {
            if(this is IConditionsContainer container)
            {
                foreach(var child in container.Children)
                {
                    child.ForceEvaluation();
                }
            }
            else if(this is IConditionParent parent && parent.Child)
            {
                parent.Child.ForceEvaluation();
            }
        }

        protected override void OnNotifyModified()
        {
            base.OnNotifyModified();
            if(Parent != null && Parent is ProcedureObject)
            {
                (Parent as ProcedureObject).Modified();
            }
        }

        public bool Contains(BaseCondition child)
        {
            if(child == this) { return true; }
            else if(this is IConditionsContainer cc)
            {
                foreach (var item in cc.Children)
                {
                    if (item.Contains(child)) { return true; }
                }
            }
            else if(this is IConditionParent p && p.Child) { return p.Child.Contains(child); }
            return false;
        }

        public virtual void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode)
        {
            Reset();
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            base.CollectProcedureObjects(list);
            if(this is IConditionsContainer container)
            {
                foreach(var condition in container.Children)
                {
                    condition.CollectProcedureObjects(list);
                }
            }
            else if(this is IConditionParent parent)
            {
                parent.Child?.CollectProcedureObjects(list);
            }
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            if(this is IConditionsContainer container)
            {
                foreach(var child in container.Children)
                {
                    if (child)
                    {
                        child.Procedure = value;
                    }
                }
            }
            else if(this is IConditionParent parent && parent.Child)
            {
                parent.Child.Procedure = value;
            }
        }

        protected abstract bool EvaluateCondition();

        public virtual void OnEvaluationEnded() 
        {
            m_networkValue = null;
        }

        public virtual string ToFullString()
        {
            return GetType().Name;
        }
        
        public virtual IEvaluationNode Parent
        {
            get { return m_parent; }
            set
            {
                if(m_parent != value)
                {
                    var prevParent = m_parent;
                    m_parent = value;
                    if(prevParent is IConditionParent)
                    {
                        ((IConditionParent)prevParent).ChildChangedParent(this, m_parent);
                    }
                    PropertyChanged(nameof(Parent));
                }
            }
        }

        public T GetFirstAncenstor<T>() where T : BaseCondition
        {
            var parent = Parent;
            while(parent is BaseCondition c)
            {
                if(parent is T ancestor) { return ancestor; }
                parent = c.Parent;
            }
            return null;
        }

        public bool IsChildOf(BaseCondition condition)
        {
            return condition != null && condition.Equals(Parent);
        }

        public bool IsAncestorOf(BaseCondition condition)
        {
            var parent = Parent as BaseCondition;
            while(parent != null && parent != condition)
            {
                parent = parent.Parent as BaseCondition;
            }
            return parent == condition;
        }

        public virtual void OnValidate()
        {
            
        }

        public virtual void CollectNodesToEvaluate(ExecutionFlow flow, ExecutionMode mode, HashSet<IEvaluationNode> nodes = null)
        {
            if(nodes == null)
            {
                nodes = new HashSet<IEvaluationNode>();
            }

            if(this is IConditionsContainer container)
            {
                foreach(var child in container.Children)
                {
                    if(child != null)
                    {
                        child.CollectNodesToEvaluate(flow, mode, nodes);
                    }
                }
            }
            else if(this is IConditionParent thisAsParent)
            {
                if (thisAsParent.Child != null)
                {
                    thisAsParent.Child.CollectNodesToEvaluate(flow, mode, nodes);
                }
            }

            PrepareForEvaluation(flow, mode);
            if (CanCacheValue)
            {
                Evaluate();
            }
            else
            {
                nodes.Add(this);
            }
        }

        public BaseCondition CloneTree()
        {
            var root = s_Clone != null ? s_Clone(this) : Instantiate(this);
            if(this is IConditionsContainer container)
            {
                var rootContainer = root as IConditionsContainer;
                rootContainer.Children.Clear();
                foreach(var item in container.Children)
                {
                    var clone = item.CloneTree();
                    clone.Parent = rootContainer;
                    rootContainer.Children.Add(clone);
                }
            }
            else if(this is IConditionParent parent)
            {
                var clone = parent.Child.CloneTree();
                clone.Parent = root;
                (root as IConditionParent).Child = clone;
            }

            return root;
        }
    }

    public interface IEvaluationNode
    {
        void PrepareForEvaluation(ExecutionFlow flow, ExecutionMode mode);

        /// <summary>
        /// Evaluates the node
        /// </summary>
        /// <returns>Evaluation outcome or null if cannot evaluate at the moment</returns>
        bool Evaluate();
        void Reset();
        void OnEvaluationEnded();
    }

    public interface IEvaluationEndedCallback
    {
        void NodesEvaluationEnded();
    }

    public interface IConditionParent : IEvaluationNode
    {
        void ChildChangedParent(BaseCondition child, IEvaluationNode newParent);
        BaseCondition Child { get; set; }
    }

    public interface IConditionsContainer : IConditionParent
    {
        void Add(BaseCondition child);
        bool Remove(BaseCondition child);
        List<BaseCondition> Children { get; }
    }
}
