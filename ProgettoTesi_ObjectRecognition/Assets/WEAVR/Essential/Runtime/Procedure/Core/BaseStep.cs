using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class BaseStep : GraphObject, IProcedureStep
    {
        [SerializeField]
        protected string m_number;
        [LongText]
        [SerializeField]
        protected LocalizedString m_description;
        [SerializeField]
        protected bool m_isMandatory = true;
        [SerializeField]
        protected List<BaseNode> m_nodes;

        public List<BaseNode> Nodes => m_nodes;
        
        public string Description
        {
            get => m_description;
            set
            {
                if(m_description != value)
                {
                    BeginChange();
                    m_description = value;
                    foreach (var node in m_nodes)
                    {
                        if (node is IProcedureStep step)
                        {
                            step.Description = value;
                        }
                    }
                    PropertyChanged(nameof(Description));
                }
            }
        }

        public bool IsMandatory
        {
            get => m_isMandatory;
            set
            {
                if (m_isMandatory != value)
                {
                    BeginChange();
                    m_isMandatory = value;
                    PropertyChanged(nameof(IsMandatory));
                }
            }
        }

        public string Number
        {
            get => m_number;
            set
            {
                if(m_number != value)
                {
                    BeginChange();
                    m_number = value;
                    foreach(var node in m_nodes)
                    {
                        if (node is IProcedureStep step)
                        {
                            step.Number = value;
                        }
                    }
                    PropertyChanged(nameof(Number));
                }
            }
        }

        public string StepGUID => Guid;

        public void SetDescription(LocalizedString description)
        {
            m_description = description.Clone();
            foreach(var node in m_nodes)
            {
                if(node is IProcedureStep step)
                {
                    step.SetDescription(description);
                }
            }
        }

        public void AddNode(BaseNode node, bool notifyChange)
        {
            if (!m_nodes.Contains(node))
            {
                BeginChange();
                m_nodes.Add(node);
                node.Step = this;
                if (node is IProcedureStep step)
                {
                    step.Number = m_number;
                    step.SetDescription(m_description);
                }
                if (notifyChange)
                {
                    PropertyChanged(nameof(Nodes));
                }
            }
        }

        public void AddNodes(IEnumerable<BaseNode> nodes, bool notifyChange)
        {
            bool changed = false;
            BeginChange();
            foreach(var node in nodes)
            {
                if (!m_nodes.Contains(node))
                {
                    changed = true;
                    m_nodes.Add(node);
                    node.Step = this;
                    if (node is IProcedureStep step)
                    {
                        step.Number = m_number;
                        step.SetDescription(m_description);
                    }
                }
            }

            if (changed && notifyChange)
            {
                PropertyChanged(nameof(Nodes));
            }
        }

        public void RemoveNode(BaseNode node, bool notifyChange)
        {
            if (m_nodes.Contains(node))
            {
                BeginChange();
                m_nodes.Remove(node);
                node.Step = null;
                if (notifyChange)
                {
                    PropertyChanged(nameof(Nodes));
                }
            }
        }

        public void SyncNode(BaseNode node)
        {
            if (node is IProcedureStep step)
            {
                step.Number = m_number;
                step.SetDescription(m_description);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeGUID();
            if(m_nodes == null)
            {
                m_nodes = new List<BaseNode>();
            }
            for (int i = 0; i < m_nodes.Count; i++)
            {
                if (m_nodes[i])
                {
                    m_nodes[i].Step = this;
                }
            }
        }

        public void RemoveAllNodes(bool notifyChange)
        {
            BeginChange();
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = m_nodes[i];
                m_nodes.RemoveAt(i--);
                node.Step = null;
            }
            if (notifyChange)
            {
                PropertyChanged(nameof(Nodes));
            }
        }
    }
}