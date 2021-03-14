using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class ConditionNode : BaseCondition, IConditionsContainer
    {
        [SerializeField]
        private List<BaseCondition> m_children;

        [NonSerialized]
        private bool m_isDisabling;

        public override bool CanBeShared => false;

        public List<BaseCondition> Children
        {
            get
            {
                if(m_children == null)
                {
                    m_children = new List<BaseCondition>();
                }
                return m_children;
            }
        }

        public BaseCondition Child
        {
            get => Children.Count > 0 ? Children[0] : null;
            set
            {
                if(Children.Count == 0 && value)
                {
                    Add(value);
                    PropertyChanged(nameof(Child));
                }
                else if(Children.Count > 0 && Children[0] != value)
                {
                    BeginChange();
                    Children[0] = value;
                    PropertyChanged(nameof(Children));
                    PropertyChanged(nameof(Child));
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            CanCacheValue = true;
            for (int i = 0; i < Children.Count; i++)
            {
                if(Children[i] == null)
                {
                    Children.RemoveAt(i--);
                }
                else
                {
                    Children[i].Parent = this;
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_isDisabling = true;
            foreach(var child in Children)
            {
                if (child && Equals(child.Parent))
                {
                    child.Parent = null;
                }
            }
            m_isDisabling = false;
        }

        public virtual void Clear()
        {
            if(m_children != null)
            {
                m_children.Clear();
                PropertyChanged(nameof(Children));
            }
        }

        public virtual bool Remove(BaseCondition condition)
        {
            if (m_children == null)
            {
                m_children = new List<BaseCondition>();
                return false;
            }

            BeginChange();
            if (m_children.Remove(condition))
            {
                if (Equals(condition.Parent))
                {
                    condition.Parent = null;
                }
                PropertyChanged(nameof(Children));
                return true;
            }
            return false;
        } 

        public virtual bool RemoveAt(int i)
        {
            return 0 <= i && i < m_children.Count && Remove(m_children[i]);
        }

        public virtual void Add(BaseCondition condition)
        {
            if(m_children == null)
            {
                m_children = new List<BaseCondition>();
            }

            if (m_children.Contains(condition))
            {
                throw new ArgumentException("Condition '" + condition + "' already is added");
            }

            BeginChange();
            m_children.Add(condition);
            condition.Parent = this;
            PropertyChanged(nameof(Children));
        }

        public void ChildChangedParent(BaseCondition child, IEvaluationNode newParent)
        {
            if (!m_isDisabling)
            {
                Remove(child);
            }
        }

        public override string GetDescription()
        {
            return Children.Count > 1 ?
                        $"[{Children.Count} Conditions]" :
                            Children.Count > 0 ?
                            Children[0].GetDescription() : "[No Condition]";
        }
    }
}