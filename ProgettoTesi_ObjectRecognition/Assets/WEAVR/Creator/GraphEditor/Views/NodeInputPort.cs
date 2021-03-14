using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{
    class NodeInputPort : FlowPort
    {
        protected NodeInputPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity) : base(portOrientation, portDirection, portCapacity)
        {
            AddToClassList("inputFlowPort");
        }

        public static NodeInputPort Create(Orientation orientation, Direction direction)
        {
            var flowPort = new NodeInputPort(orientation, direction, Capacity.Multi);
            flowPort.m_EdgeConnector = new EdgeConnector<FlowEdge>(flowPort);
            flowPort.AddManipulator(flowPort.m_EdgeConnector);
            //flowPort.controller = controller;
            return flowPort;
        }

        //public override void EdgesCreated(GraphView graphView, Port input, Port output, List<Edge> edgesToCreate)
        //{
        //    if (output is ConditionPort)
        //    {
        //        (output as ConditionPort).EdgesCreated(graphView, input, output, edgesToCreate);
        //    }
        //    else
        //    {
        //        base.EdgesCreated(graphView, input, output, edgesToCreate);
        //    }
        //}

        //public override void EdgesDeleted(GraphView graphView, List<GraphElement> deletedEdges)
        //{
        //    for (int i = 0; i < deletedEdges.Count; i++)
        //    {
        //        var edge = deletedEdges[i] as FlowEdge;
        //        if(edge != null && edge.output is ConditionPort && (edge.output as ConditionPort).DeleteEdge(graphView, edge))
        //        {
        //            deletedEdges.RemoveAt(i--);
        //        }
        //    }
        //    //if (output is ConditionPort)
        //    //{
        //    //    (output as ConditionPort).EdgesCreated(graphView, input, output, edgesToCreate);
        //    //}
        //    //else
        //    {
        //        base.EdgesDeleted(graphView, deletedEdges);
        //    }
        //}
    }
}
