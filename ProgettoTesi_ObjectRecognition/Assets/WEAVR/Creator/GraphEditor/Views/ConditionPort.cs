using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Procedure
{
    class ConditionPort : FlowPort, IEdgeConnectorListener, IControlledElement, ISettableControlledElement<FlowConditionPortController>, IMasterPort
    {
        private FlowConditionPortController m_controller;

        public FlowConditionPortController Controller
        {
            get => m_controller;
            set
            {
                if (m_controller != value)
                {
                    if (m_controller != null)
                    {
                        m_controller.UnregisterHandler(this);
                    }
                    m_controller = value;
                    OnNewController();
                    if (m_controller != null)
                    {
                        m_controller.RegisterHandler(this);
                    }
                }
            }
        }

        Controller IControlledElement.Controller => m_controller;

        protected ConditionPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity) : base(portOrientation, portDirection, portCapacity)
        {
            AddToClassList("conditionPort");
        }

        public static ConditionPort Create(FlowConditionPortController controller, Orientation orientation = Orientation.Horizontal, Capacity capacity = Capacity.Single)
        {
            var flowPort = new ConditionPort(orientation, Direction.Output, capacity);
            flowPort.m_EdgeConnector = new EdgeConnector<FlowEdge>(flowPort);
            flowPort.AddManipulator(flowPort.m_EdgeConnector);
            flowPort.Controller = controller;
            return flowPort;
        }

        protected virtual void OnNewController()
        {
            if (Controller != null)
            {
                viewDataKey = ComputePersistenceKey();
            }
        }

        public string ComputePersistenceKey()
        {
            return Controller != null ? $"GraphObject-{Controller.Model?.GetType().Name}-{Controller.Model.GetInstanceID()}" : null;
        }

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == m_controller)
            {
                SelfChange();
            }
        }

        private void SelfChange()
        {
            EnableInClassList("mandatory", Controller?.IsMainFlow ?? false);
        }

        //public static VFXFlowAnchor Create(VFXFlowAnchorController controller)
        //{
        //    var anchor = new VFXFlowAnchor(controller.orientation, controller.direction, typeof(int));
        //    anchor.m_EdgeConnector = new EdgeConnector<VFXFlowEdge>(anchor);
        //    anchor.AddManipulator(anchor.m_EdgeConnector);
        //    anchor.controller = controller;
        //    return anchor;
        //}

        //protected VFXFlowAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, Capacity.Multi, type)
        //{
        //    this.AddStyleSheetPath("VFXFlow");
        //    AddToClassList("EdgeConnector");
        //}

        public override void EdgesCreated(GraphView graphView, Port input, Port output, List<Edge> edgesToCreate)
        {
            var procedureView = GetFirstAncestorOfType<ProcedureView>();
            foreach(var edge in edgesToCreate)
            {
                if(edge is FlowEdge)
                {
                    Controller.CreateTransition((output.node as GraphObjectView).Controller, (input.node as GraphObjectView).Controller);
                }
            }
        }

        public override void EdgesDeleted(GraphView graphView, List<GraphElement> deletedEdges)
        {
            base.EdgesDeleted(graphView, deletedEdges);
            foreach (var edge in deletedEdges)
            {
                if (edge is FlowEdge)
                {
                    Controller.DeleteTransition((edge as FlowEdge).Controller);
                }
            }
        }

        public bool DeleteEdge(GraphView graphView, GraphElement edge)
        {
            if (edge is FlowEdge)
            {
                Controller.DeleteTransition((edge as FlowEdge).Controller);
                return true;
            }
            return false;
        }
    }
}
