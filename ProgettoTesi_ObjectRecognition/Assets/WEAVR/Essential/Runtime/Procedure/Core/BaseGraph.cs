using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core.DataTypes;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class BaseGraph : GraphObject
    {

        [SerializeField]
        private ReferenceTable m_referencesTable;

        [SerializeField]
        private List<BaseNode> m_nodes;
        [SerializeField]
        private List<BaseStep> m_steps;
        [DoNotClone]
        [SerializeField]
        private List<BaseTransition> m_transitions;
        [SerializeField]
        private List<BaseNode> m_startingNodes;
        [SerializeField]
        private List<BaseNode> m_flowStartNodes;
        [SerializeField]
        private List<BaseNode> m_debugStartNodes;

        private DataGraph<GraphObject, BaseTransition> m_dataGraph;

        public DataGraph<GraphObject, BaseTransition> DataGraph
        {
            get
            {
                if(m_dataGraph == null)
                {
                    m_dataGraph = new DataGraph<GraphObject, BaseTransition>(n => Transitions.Where(t => t.From == n && t.To), t => t.To, t => t.Priority);
                    m_dataGraph.Build(StartingNodes.Concat(FlowStartNodes).ToArray());
                }
                return m_dataGraph;
            }
        }

        public new Vector2 UI_Position{
            get => base.UI_Position;
            set
            {
                if(base.UI_Position == Vector2.zero)
                {
                    base.UI_Position = value != Vector2.zero ? value : Vector2.one;
                }
            }
        }

        public ReferenceTable ReferencesTable
        {
            get => m_referencesTable;
            set
            {
                if(m_referencesTable != value)
                {
                    BeginChange();
                    m_referencesTable = value;
                    PropertyChanged(nameof(ReferencesTable));
                }
            }
        }
        public List<BaseNode> Nodes => m_nodes;
        public List<BaseStep> Steps => m_steps;
        public List<BaseTransition> Transitions => m_transitions;
        public List<BaseNode> StartingNodes => m_startingNodes;
        public List<BaseNode> FlowStartNodes => m_flowStartNodes;
        public List<BaseNode> DebugStartNodes => m_debugStartNodes;

        public virtual bool HasIssues => Nodes.Any(n => !n) || Steps.Any(s => !s || s.Nodes.Count == 0) || Transitions.Any(t => !t || !t.From || !t.To);

        public event Action<BaseGraph, BaseNode> NodeAdded;
        public event Action<BaseGraph, BaseNode> NodeRemoved;
        public event Action<BaseGraph, BaseStep> StepAdded;
        public event Action<BaseGraph, BaseStep> StepRemoved;
        public event Action<BaseGraph, BaseTransition> TransitionAdded;
        public event Action<BaseGraph, BaseTransition> TransitionRemoved;

        public event Action<BaseGraph> StartingNodesChanged;
        public event Action<BaseGraph> FlowStartNodesChanged;
        public event Action<BaseGraph> DebugStartNodesChanged;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_referencesTable == null)
            {
                m_referencesTable = ReferenceTable.Create();
            }
            if (m_nodes == null)
            {
                m_nodes = new List<BaseNode>();
            }
            if (m_steps == null)
            {
                m_steps = new List<BaseStep>();
            }
            CleanUpList(m_steps);
            for (int i = 0; i < m_steps.Count; i++)
            {
                if (m_steps[i].Nodes.Count == 0)
                {
                    m_steps.RemoveAt(i--);
                }
            }
            if (m_transitions == null)
            {
                m_transitions = new List<BaseTransition>();
            }
            CleanUpList(m_transitions);
            RefreshStartNodes();
            RefreshDebugStartNodes();

            RegisterTransitionsEvents();
        }

        public virtual void Sanitize()
        {
            bool notify = false;
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (!Nodes[i])
                {
                    Nodes.RemoveAt(i--);
                    notify = true;
                }
            }
            for (int i = 0; i < Steps.Count; i++)
            {
                if (!Steps[i] || Steps[i].Nodes.Count == 0)
                {
                    Steps.RemoveAt(i--);
                    notify = true;
                }
            }
            for (int i = 0; i < Transitions.Count; i++)
            {
                var transition = Transitions[i];
                if (!transition || !transition.From || !transition.To)
                {
                    Transitions.RemoveAt(i--);
                    notify = true;
                }
            }
            if (notify)
            {
                Modified();
            }
        }

        public override void CollectProcedureObjects(List<ProcedureObject> list)
        {
            //base.CollectProcedureObjects(list);
            foreach(var node in m_nodes)
            {
                node.CollectProcedureObjects(list);
            }
            foreach(var group in m_steps)
            {
                group.CollectProcedureObjects(list);
            }
        }

        public void RefreshStartNodes()
        {
            if(m_flowStartNodes == null)
            {
                m_flowStartNodes = new List<BaseNode>();
            }
            if (m_startingNodes == null)
            {
                m_startingNodes = new List<BaseNode>();
                return;
            }
            for (int i = 0; i < m_flowStartNodes.Count; i++)
            {
                if (m_flowStartNodes[i] == null || !m_nodes.Contains(m_flowStartNodes[i]))
                {
                    m_flowStartNodes.RemoveAt(i--);
                }
            }
            for (int i = 0; i < m_startingNodes.Count; i++)
            {
                if (m_startingNodes[i] == null || !m_nodes.Contains(m_startingNodes[i]))
                {
                    m_startingNodes.RemoveAt(i--);
                }
            }
            if (m_startingNodes.Count == 0 && m_nodes.Count > 0)
            {
                m_startingNodes.Add(m_nodes[0]);
            }
        }

        public void RefreshDebugStartNodes()
        {
            if (!Application.isEditor) { return; }

            if (m_debugStartNodes == null)
            {
                m_debugStartNodes = new List<BaseNode>();
                return;
            }

            bool resetPriorities = false;
            for (int i = 0; i < m_debugStartNodes.Count; i++)
            {
                if (m_debugStartNodes[i] == null || !m_nodes.Contains(m_debugStartNodes[i]))
                {
                    m_debugStartNodes.RemoveAt(i--);
                    resetPriorities = true;
                }
            }
            if(m_debugStartNodes.Count == 1 && m_startingNodes.Contains(m_debugStartNodes[0]))
            {
                m_debugStartNodes.Clear();
                resetPriorities = true;
            }
            if (resetPriorities)
            {
                ResetPriorities();
            }
        }

        private void ResetPriorities()
        {
            foreach(var t in Transitions)
            {
                if (t) { t.ResetPriority(); }
            }
        }

        public void ResetPathsPriorities()
        {
            MarkDirty();
            ResetPriorities();
            Modified();
        }

        private void CleanUpList<T>(List<T> list) where T : UnityEngine.Object
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    list.RemoveAt(i--);
                }
            }
        }

        public virtual void Add(BaseNode node)
        {
            if (m_nodes.Contains(node)) { return; }

            BeginChange();
            m_nodes.Add(node);
            NodeAdded?.Invoke(this, node);
            PropertyChanged(nameof(Nodes));
        }

        public virtual void Remove(BaseNode node)
        {
            BeginChange();
            if (m_nodes.Remove(node))
            {
                NodeRemoved?.Invoke(this, node);
                PropertyChanged(nameof(Nodes));
            }
        }

        public virtual void Add(BaseStep step)
        {
            if (m_steps.Contains(step)) { return; }

            BeginChange();
            m_steps.Add(step);
            StepAdded?.Invoke(this, step);
            PropertyChanged(nameof(Steps));
        }

        public virtual void Remove(BaseStep step)
        {
            BeginChange();
            if (m_steps.Remove(step))
            {
                StepRemoved?.Invoke(this, step);
                PropertyChanged(nameof(Steps));
            }
        }

        public virtual void Add(BaseTransition transition)
        {
            if (m_transitions.Contains(transition)) { return; }

            BeginChange();
            m_transitions.Add(transition);
            TransitionAdded?.Invoke(this, transition);
            PropertyChanged(nameof(Transitions));
        }

        public virtual void Remove(BaseTransition transition)
        {
            BeginChange();
            if (m_transitions.Remove(transition))
            {
                TransitionRemoved?.Invoke(this, transition);
                PropertyChanged(nameof(Transitions));
            }
        }

        public virtual void SetStartNode(BaseNode node)
        {
            if(!m_nodes.Contains(node) || m_startingNodes.Contains(node)) { return; }

            BeginChange();
            m_startingNodes.Add(node);
            StartingNodesChanged?.Invoke(this);
            PropertyChanged(nameof(StartingNodes));
        }

        public virtual void RemoveStartNode(BaseNode node)
        {
            if (!m_nodes.Contains(node) || m_startingNodes.Count <= 1 || !m_startingNodes.Contains(node)) { return; }

            BeginChange();
            if (m_startingNodes.Remove(node))
            {
                StartingNodesChanged?.Invoke(this);
                PropertyChanged(nameof(StartingNodes));
            }
        }

        public virtual void SetFlowStartNode(BaseNode node)
        {
            if (!m_nodes.Contains(node) || m_flowStartNodes.Contains(node)) { return; }

            BeginChange();
            m_flowStartNodes.Add(node);
            FlowStartNodesChanged?.Invoke(this);
            PropertyChanged(nameof(FlowStartNodes));
        }

        public virtual void RemoveFlowStartNode(BaseNode node)
        {
            if (!m_flowStartNodes.Contains(node)) { return; }

            BeginChange();
            if (m_flowStartNodes.Remove(node))
            {
                FlowStartNodesChanged?.Invoke(this);
                PropertyChanged(nameof(FlowStartNodes));
            }
        }

        public virtual void SetAsOnlyStartNode(BaseNode node)
        {
            if (!m_nodes.Contains(node) || m_startingNodes.Contains(node)) { return; }

            BeginChange();
            m_startingNodes.Clear();
            m_startingNodes.Add(node);
            StartingNodesChanged?.Invoke(this);
            PropertyChanged(nameof(StartingNodes));
        }

        public virtual void RemoveDebugStartNode(BaseNode node)
        {
            if (!m_nodes.Contains(node) || !m_debugStartNodes.Contains(node)) { return; }

            BeginChange();
            if (m_debugStartNodes.Remove(node))
            {
                ResetPriorities();
                DebugStartNodesChanged?.Invoke(this);
                PropertyChanged(nameof(DebugStartNodes));
            }
        }

        public virtual void SetDebugStartNode(BaseNode node)
        {
            if (!m_nodes.Contains(node) || m_debugStartNodes.Contains(node)) { return; }

            BeginChange();
            m_debugStartNodes.Clear();
            if (!m_startingNodes.Contains(node))
            {
                m_debugStartNodes.Add(node);
            }
            ResetPriorities();
            DebugStartNodesChanged?.Invoke(this);
            PropertyChanged(nameof(DebugStartNodes));
        }

        public virtual void Merge(BaseGraph other)
        {

        }

        public List<GraphObject> ShortestPath(GraphObject from, GraphObject to)
        {
            var path = DataGraph.ShortestPath(from, to);
            List<GraphObject> fullPath = new List<GraphObject>();
            if (path != null)
            {
                foreach (var link in path.Links)
                {
                    fullPath.Add(link.Data);
                    if (link.Edge.edge) { fullPath.Add(link.Edge.edge); }
                }
            }
            return fullPath;
        }

        public float GetPathWeight(GraphObject from, GraphObject to)
        {
            return DataGraph.ShortestPath(from, to)?.TotalWeight ?? float.MaxValue;
        }

        public float GetPathWeightToDebugNode(GraphObject from)
        {
            return DebugStartNodes.Count > 0 ? GetPathWeight(from, DebugStartNodes[0]) : float.MaxValue;
        }

        public float GetPathWeightToNode(GraphObject to)
        {
            return StartingNodes.Select(n => DataGraph.ShortestPath(n, to)).Min(p => p?.TotalWeight) ?? float.MaxValue;
        }

        public DataGraph<GraphObject, BaseTransition>.Path ShortestDebugPath()
        {
            return DebugStartNodes.Count > 0 ? 
                StartingNodes.Select(n => DataGraph.ShortestPath(n, DebugStartNodes[0])).OrderBy(p => p?.TotalWeight).FirstOrDefault() : 
                null;
        }

        public DataGraph<GraphObject, BaseTransition>.Path ShortestDebugPath(BaseNode startNode)
        {
            return DebugStartNodes.Count > 0 ?
                DataGraph.ShortestPath(startNode, DebugStartNodes[0]) :
                null;
        }

        public void InvalidateDebugPaths()
        {
            if(m_dataGraph == null) { return; }

            foreach(var debugNode in DebugStartNodes)
            {
                foreach(var startNode in StartingNodes)
                {
                    DataGraph.InvalidatePath(startNode, debugNode);
                }
            }

            Modified();
        }

        public bool IsReacheableFromStartPoints(GraphObject node)
        {
            return m_startingNodes.Contains(node) || m_startingNodes.Any(n => DataGraph.ShortestPath(n, node)?.Destination == node);
        }

        public bool IsReacheableFromFlowStartPoints(GraphObject node)
        {
            return m_flowStartNodes.Contains(node) || m_flowStartNodes.Any(n => DataGraph.ShortestPath(n, node)?.Destination == node);
        }

        public bool IsReacheableFromDebugStartPoints(GraphObject node)
        {
            return m_debugStartNodes.Contains(node) || m_debugStartNodes.Any(n => DataGraph.ShortestPath(node, n)?.Destination == n);
        }

        public bool AreConnected(GraphObject from, GraphObject to)
        {
            return DataGraph.ShortestPath(from, to)?.Destination == to;
        }

        public DataGraph<GraphObject, BaseTransition> GetDataGraph()
        {
            return new DataGraph<GraphObject, BaseTransition>(n => Transitions.Where(t => t.From == n && t.To), t => t.To, t => t.Priority);
        }

        public virtual void MarkDirty()
        {
            //if(m_dataGraph != null)
            //{
            //}
            RegisterTransitionsEvents();
            m_dataGraph = null;
        }

        private void RegisterTransitionsEvents()
        {
            if (!Application.isEditor) { return; }

            foreach (var t in m_transitions)
            {
                t.PriorityChanged -= Transition_PriorityChanged;
                t.PriorityChanged += Transition_PriorityChanged;
                t.OnPropertyChanged -= Transition_OnPropertyChanged;
                t.OnPropertyChanged += Transition_OnPropertyChanged;
            }
        }

        private void Transition_PriorityChanged(BaseTransition transition, float newPriority)
        {
            MarkDirty();
            Modified();

            //if (m_dataGraph != null)
            //{
            //    m_dataGraph.UpdateWeights();
            //    Modified();
            //}

            //InvalidateDebugPaths();
        }

        private void Transition_OnPropertyChanged(ProcedureObject transition, string property)
        {
            if(property == nameof(BaseTransition.To) || property == nameof(BaseTransition.From))
            {
                MarkDirty();
            }
        }

        protected override void OnAssignedToProcedure(Procedure value)
        {
            base.OnAssignedToProcedure(value);
            foreach(var node in Nodes)
            {
                if (node) { node.Procedure = value; }
            }
            foreach (var step in Steps)
            {
                if (step) { step.Procedure = value; }
            }
            foreach (var transition in Transitions)
            {
                if (transition) { transition.Procedure = value; }
            }
            if (ReferencesTable) { ReferencesTable.Procedure = value; }
        }
    }
}

