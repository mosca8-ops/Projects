using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{
    class FlowPort : Port, IEdgeConnectorListener
    {

        protected FlowPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity) : base(portOrientation, portDirection, portCapacity, typeof(int))
        {
            this.AddStyleSheetPath("FlowConnector");
            AddToClassList("flowPort");

            m_ConnectorText.text = portDirection == Direction.Input ? "Input" : "Output";

            m_edgesToCreate = new List<Edge>();
            m_edgesToDelete = new List<GraphElement>();

            m_graphViewChange.edgesToCreate = m_edgesToCreate;
        }

        public static FlowPort Create(Orientation orientation, Direction direction, Capacity capacity)
        {
            var flowPort = new FlowPort(orientation, direction, capacity);
            flowPort.m_EdgeConnector = new EdgeConnector<FlowEdge>(flowPort);
            flowPort.AddManipulator(flowPort.m_EdgeConnector);
            //flowPort.controller = controller;
            return flowPort;
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return (new Rect(0.0f, 0.0f, layout.width, layout.height)).Contains(localPoint);
        }

        private GraphViewChange m_graphViewChange;
        private List<Edge> m_edgesToCreate;
        private List<GraphElement> m_edgesToDelete;
        
        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            var graphView = GetFirstAncestorOfType<GraphView>();
            var nodes = graphView.Query<BaseNodeView>().ToList();
            var potentialNode = nodes.FirstOrDefault(n => n.worldBound.Contains(position));
            if (potentialNode != null)
            {
                if (edge.input == null)
                {
                    edge.input = potentialNode.Query<FlowPort>().ToList().FirstOrDefault(p => p != this && p.direction == Direction.Input);
                    if (edge.input != null)
                    {
                        edge.candidatePosition = edge.input.worldBound.position;
                        OnDrop(graphView, edge);
                    }
                }
                else if(edge.output == null)
                {
                    edge.output = potentialNode.Query<FlowPort>().ToList().FirstOrDefault(p => p != this && p.direction == Direction.Output);
                    if (edge.output != null)
                    {
                        edge.candidatePosition = edge.output.worldBound.position;
                        OnDrop(graphView, edge);
                    }
                }
            }
        }

        private static Vector2 ConvertToGraphSpace(Vector2 position, GraphView graphView)
        {
            return graphView.contentViewContainer.WorldToLocal(graphView.panel.ScreenToViewPosition(position));
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            m_edgesToCreate.Clear();
            m_edgesToCreate.Add(edge);

            // We can't just add these edges to delete to the m_GraphViewChange
            // because we want the proper deletion code in GraphView to also
            // be called. Of course, that code (in DeleteElements) also
            // sends a GraphViewChange.
            m_edgesToDelete.Clear();
            if (edge.input.capacity == Capacity.Single)
            {
                foreach (Edge edgeToDelete in edge.input.connections)
                {
                    if (edgeToDelete != edge)
                    {
                        m_edgesToDelete.Add(edgeToDelete);
                    }
                }
            }
            if (edge.output.capacity == Capacity.Single)
            {
                foreach (Edge edgeToDelete in edge.output.connections)
                {
                    if (edgeToDelete != edge)
                    {
                        m_edgesToDelete.Add(edgeToDelete);
                    }
                }
            }
            if (m_edgesToDelete.Count > 0)
            {
                EdgesDeleted(graphView, m_edgesToDelete);
            }

            var edgesToCreate = m_edgesToCreate;
            if (graphView.graphViewChanged != null)
            {
                edgesToCreate = graphView.graphViewChanged(m_graphViewChange).edgesToCreate;
            }

            EdgesCreated(graphView, edge.input, edge.output, edgesToCreate);

            graphView.RemoveElement(edge);
        }

        public virtual void EdgesCreated(GraphView graphView, Port input, Port output, List<Edge> edgesToCreate)
        {
            if (input == this && output is IMasterPort)
            {
                (output as IMasterPort).EdgesCreated(graphView, input, output, edgesToCreate);
            }
            else if(output == this && input is IMasterPort)
            {
                (input as IMasterPort).EdgesCreated(graphView, input, output, edgesToCreate);
            }
            else
            {
                foreach (Edge e in edgesToCreate)
                {
                    graphView.AddElement(e);
                    input.Connect(e);
                    output.Connect(e);
                }
            }
        }

        public virtual void EdgesDeleted(GraphView graphView, List<GraphElement> deletedEdges)
        {
            for (int i = 0; i < deletedEdges.Count; i++)
            {
                var edge = deletedEdges[i] as FlowEdge;
                if(edge == null) { continue; }
                var masterPort = edge.input == this ? edge.output as IMasterPort : (edge.output == this ? edge.input as IMasterPort : null);
                if (masterPort != null && masterPort.DeleteEdge(graphView, edge))
                {
                    deletedEdges.RemoveAt(i--);
                }
            }
            graphView.DeleteElements(m_edgesToDelete);
        }
    }
}
