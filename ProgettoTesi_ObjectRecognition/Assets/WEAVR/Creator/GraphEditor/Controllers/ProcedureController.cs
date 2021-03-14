using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TXT.WEAVR.Procedure
{

    class ProcedureController : Controller<Procedure>
    {
        private Dictionary<ProcedureObject, List<Controller>> m_modelControllers;

        private List<BaseNodeController> m_nodesControllers;
        private List<BaseNodeController> m_startNodesControllers;
        private List<BaseNodeController> m_flowStartNodesControllers;
        private List<BaseNodeController> m_debugStartNodesControllers;
        private List<TransitionController> m_transitionControllers;
        private List<StepController> m_stepControllers;
        private List<Controller> m_portsControllers;

        private Dictionary<BaseTransition, Controller> m_transitionPorts;

        private bool m_firstLoadDone;

        public class Change
        {
            public const int Step = 1;

            public const int Node = 11;

            public const int Transition = 21;

            public const int DataEdge = 31;
        }

        public IReadOnlyList<BaseNodeController> NodesControllers => m_nodesControllers;
        public IReadOnlyList<BaseNodeController> StartNodesControllers => m_startNodesControllers;
        public IReadOnlyList<BaseNodeController> FlowStartNodesControllers => m_flowStartNodesControllers;
        public IReadOnlyList<BaseNodeController> DebugStartNodesControllers => m_debugStartNodesControllers;
        public IReadOnlyList<StepController> StepsControllers => m_stepControllers;
        public IReadOnlyList<TransitionController> TransitionsControllers => m_transitionControllers;
        public IReadOnlyList<Controller> PortsControllers => m_portsControllers;

        public LocalizationTable LocalizationTable => Model.LocalizationTable;
        public ReferenceTable ReferenceTable => Model.Graph.ReferencesTable;

        public BaseGraph Graph => Model.Graph;
        public IReadOnlyList<BaseNode> Nodes => Model.Graph.Nodes;
        public IReadOnlyList<BaseNode> StartNodes => Model.Graph.StartingNodes;
        public IReadOnlyList<BaseNode> FlowStartNodes => Model.Graph.FlowStartNodes;
        public IReadOnlyList<BaseNode> DebugStartNodes => Model.Graph.DebugStartNodes;
        public IReadOnlyList<BaseTransition> Transitions => Model.Graph.Transitions;
        public IReadOnlyList<BaseStep> Steps => Model.Graph.Steps;

        public bool FirstLoadDone => m_firstLoadDone;

        public ProcedureController(Procedure model) : base(model)
        {
            m_modelControllers = new Dictionary<ProcedureObject, List<Controller>>();

            m_nodesControllers = new List<BaseNodeController>();
            m_startNodesControllers = new List<BaseNodeController>();
            m_flowStartNodesControllers = new List<BaseNodeController>();
            m_debugStartNodesControllers = new List<BaseNodeController>();
            m_transitionControllers = new List<TransitionController>();
            m_stepControllers = new List<StepController>();

            m_transitionPorts = new Dictionary<BaseTransition, Controller>();
            m_portsControllers = new List<Controller>();

            model.OnModified -= Procedure_OnModified;
            model.OnModified += Procedure_OnModified;
            model.Graph.OnModified -= Graph_OnModified;
            model.Graph.OnModified += Graph_OnModified;

            model.Graph.OnPropertyChanged -= Graph_OnPropertyChanged;
            model.Graph.OnPropertyChanged += Graph_OnPropertyChanged;

            Undo.undoRedoPerformed += OnUndoRedo;
            //GraphUndo.UndoRedoCallback += OnUndoRedo;
            //SyncAll();

            SearchHub.Current.RegisterSearchCallback<IProcedureStep>(FindInStep);
        }

        private bool FindInStep(IProcedureStep step, string value)
        {
            value = value.ToLower();
            if(step.Title.ToLower().Contains(value) || step.Description.ToLower().Contains(value) || step.Number.ToLower().Contains(value))
            {
                return true;
            }
            if(step.Title.Replace(" ", "").ToLower().Contains(value) || step.Description.Replace(" ", "").ToLower().Contains(value) || step.Number.Replace(" ", "").ToLower().Contains(value))
            {
                return true;
            }
            return false;
        }

        private void OnUndoRedo()
        {
            SyncAll();
            foreach (var node in NodesControllers)
            {
                node?.ForceUpdate();
            }
            foreach (var step in StepsControllers)
            {
                step?.ForceUpdate();
            }
            foreach (var transition in TransitionsControllers)
            {
                transition?.ForceUpdate();
            }
            NotifyChange(AnyThing);
        }

        internal void AddTransition(TransitionController transition)
        {
            if (!m_transitionControllers.Contains(transition))
            {
                m_transitionControllers.Add(transition);
            }
        }

        private void Graph_OnPropertyChanged(ProcedureObject graph, string property)
        {
            switch (property)
            {
                case nameof(BaseGraph.Nodes):
                    SyncAll();
                    break;

                case nameof(BaseGraph.Transitions):
                    SyncTransitions();
                    break;
                case nameof(BaseGraph.Steps):
                    SyncSteps();
                    SyncNodes();
                    break;
            }
        }

        private void Graph_OnModified(ProcedureObject obj)
        {
            SyncAll();
            NotifyChange(AnyThing);
        }

        private void Procedure_OnModified(ProcedureObject obj)
        {

        }

        public void ResetStates()
        {
            foreach (var node in NodesControllers)
            {
                node.ResetState();
            }
            foreach (var step in StepsControllers)
            {
                step.ResetState();
            }
            foreach (var transition in TransitionsControllers)
            {
                transition.ResetState();
            }
            NotifyChange(AnyThing);
        }

        public void RegisterTransitionToPort(TransitionController transition, Controller portController, bool notify = true)
        {
            if (!m_transitionControllers.Any(c => c == transition || c.Model == transition.Model))
            {
                if (FirstLoadDone && !transition.InitializedCompletely)
                {
                    transition.CompleteInitialization();
                }

                m_transitionControllers.Add(transition);
                m_transitionPorts[transition.Model] = portController;
                if (!Graph.Transitions.Contains(transition.Model))
                {
                    Undo.RegisterCompleteObjectUndo(Graph, "Added Transition to Graph");
                    Graph.Transitions.Add(transition.Model);
                    Graph.MarkDirty();
                }
                if (!m_portsControllers.Contains(portController))
                {
                    m_portsControllers.Add(portController);
                }

                if (notify)
                {
                    NotifyChange(Change.Transition);
                }
            }
        }

        public void UnregisterTransitionFromPort(TransitionController transition, bool notify = true)
        {
            if (m_transitionPorts.Remove(transition.Model))
            {
                m_transitionControllers.Remove(transition);
                Undo.RegisterCompleteObjectUndo(Graph, "Remove Transition from Graph");
                Graph.Transitions.Remove(transition.Model);
                Graph.MarkDirty();
                if (notify)
                {
                    NotifyChange(Change.Transition);
                }
            }
        }

        public Controller GetTransitionPort(TransitionController transition)
        {
            return GetTransitionPort(transition.Model);
        }

        public Controller GetTransitionPort(BaseTransition transition)
        {
            return m_transitionPorts.TryGetValue(transition, out Controller port) ? port : null;
        }

        public void AddNode(GraphObjectController controller)
        {


            NotifyChange(Change.Node);
        }

        public GraphObjectController CreateNode<TNode>(Vector2 position) where TNode : BaseNode
        {
            var node = ProcedureObject.Create<TNode>(Model);
            GraphUndo.RegisterCreatedObjectUndo(node, "Created node");
            GraphUndo.RegisterCompleteObjectUndo(Graph, "Added Node");
            Graph.Nodes.Add(node);
            Graph.MarkDirty();
            BaseNodeController nodeController = null;
            if (node is GenericNode)
            {
                nodeController = new GenericNodeController(this, node as GenericNode);
            }
            else if (node is TrafficNode)
            {
                nodeController = new TrafficNodeController(this, node as TrafficNode);
            }

            else
            {
                Debug.LogError($"[{nameof(ProcedureController)}]: No suitable controller found for {typeof(TNode).Name}");
                return null;
            }

            if (node is IProcedureStep step)
            {
                step.Number = (Steps.Count + Nodes.Count(n => n is IProcedureStep && !n.Step)).ToString();
            }
            m_nodesControllers.Add(nodeController);
            nodeController.Position = position;

            NotifyChange(Change.Node);
            return nodeController;
        }

        internal void AddStep<T>(IEnumerable<GraphObjectController> nodesControllers) where T : BaseStep
        {
            var newStep = ProcedureObject.Create<T>(Model);
            GraphUndo.RegisterCreatedObjectUndo(newStep, "Create Step");
            newStep.Title = "Super Step";
            GraphUndo.RegisterCompleteObjectUndo(Graph, "Added Step");
            Graph.Steps.Add(newStep);
            //newStep.AddNodes(nodesControllers.Where(n => n.Model is BaseNode && !(n.Model as BaseNode).Step)
            //                                 .Select(n => n.Model as BaseNode), false);
            foreach (var nodeController in nodesControllers)
            {
                if (nodeController.Model is BaseNode && !(nodeController.Model as BaseNode).Step)
                {
                    newStep.AddNode(nodeController.Model as BaseNode, false);
                }
            }

            newStep.Number = (Steps.Count + Nodes.Count(n => n is IProcedureStep && !n.Step)).ToString();
            SyncSteps();
            NotifyChange(Change.Step);
        }

        private void SyncAll()
        {
            CleanUpZombies();
            SyncNodes();
            SyncSteps();
            SyncPorts();
            SyncTransitions();

            SyncStartNodes();
            SyncDebugStartNodes();
            m_firstLoadDone = true;

            //if(Graph.ReferencesTable.SceneData != null && Graph.ReferencesTable.SceneData.IsEmpty && SceneManager.GetActiveScene().isLoaded)
            //{
            //    Graph.ReferencesTable.AdaptToCurrentScene();
            //}
        }

        private void SyncNodes()
        {
            var newControllers = new List<BaseNodeController>();
            foreach (var node in Nodes)
            {
                var newController = m_nodesControllers.Find(p => p.Model == node);
                if (newController == null) // If the controller does not exist for this model, create it
                {
                    if (node is SimpleNode)
                    {
                        //newController = new BaseNodeController(this, node as SimpleNode);
                    }
                    else if (node is GenericNode)
                    {
                        newController = new GenericNodeController(this, node as GenericNode);
                    }
                    else if (node is TrafficNode)
                    {
                        newController = new TrafficNodeController(this, node as TrafficNode);
                    }
                    newController?.ForceUpdate();
                }
                if (newController != null)
                {
                    newControllers.Add(newController);
                }
            }

            foreach (var deletedController in m_nodesControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_nodesControllers = newControllers;
        }

        private void SyncSteps()
        {
            var newControllers = new List<StepController>();
            foreach (var step in Steps)
            {
                var newController = m_stepControllers.Find(p => p.Model == step);
                if (newController == null) // If the controller does not exist for this model, create it
                {

                    if (step is BaseStep)
                    {
                        newController = new StepController(this, step);
                    }

                    else
                    {
                        Debug.LogError($"[{nameof(ProcedureController)}]: No suitable controller found for {step.GetType().Name}");
                    }
                    newController?.ForceUpdate();
                }
                if (newController != null)
                {
                    newControllers.Add(newController);
                }
            }

            foreach (var deletedController in m_stepControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_stepControllers = newControllers;
        }

        private void SyncPorts()
        {
            var newControllers = new List<Controller>();
            foreach (var port in NodesControllers.Where(n => n is IOutputPortsProvider).SelectMany(n => (n as IOutputPortsProvider).OutputPorts))
            {
                var newController = m_portsControllers.Find(p => p.GetModel() == port.PortModel);
                if (newController == null) // If the controller does not exist for this model, create it
                {
                    if (port.PortModel is FlowCondition)
                    {
                        newController = GetController<FlowConditionController>(port.PortModel as ProcedureObject).PortController;
                        //newController.ForceUpdate();
                    }
                }
                if (newController != null)
                {
                    newControllers.Add(newController);
                }
            }

            foreach (var deletedController in m_portsControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_portsControllers = newControllers;
        }

        private void SyncTransitions()
        {
            var newControllers = new List<TransitionController>();
            foreach (var transition in Transitions)
            {
                var newController = m_transitionControllers.Find(p => p.Model == transition);
                if (newController == null && transition) // If the controller does not exist for this model, create it
                {
                    newController = new TransitionController(transition, GetTransitionPort(transition), this);
                    newController.ForceUpdate();
                }

                if (newController == null)
                {
                    continue;
                }

                if (!newController.InitializedCompletely)
                {
                    newController.CompleteInitialization();
                }

                if (Graph.Nodes.Contains(newController.ToModel))
                {
                    newControllers.Add(newController);
                }
            }

            foreach (var deletedController in m_transitionControllers.Except(newControllers).ToArray())
            {
                deletedController.OnDisable();
            }

            //for (int i = 0; i < newControllers.Count; i++)
            //{
            //    var transition = newControllers[i];
            //    if(transition.From == null || transition.To == null)
            //    {
            //        newControllers.RemoveAt(i--);
            //        transition.OnDisable();
            //    }
            //}
            m_transitionControllers = newControllers;
        }

        private void SyncStartNodes()
        {
            Graph.RefreshStartNodes();
            m_startNodesControllers.Clear();
            m_flowStartNodesControllers.Clear();
            foreach (var node in StartNodes)
            {
                var controller = m_nodesControllers.FirstOrDefault(c => c.Model == node);
                if (controller != null)
                {
                    m_startNodesControllers.Add(controller);
                }
            }
            foreach (var node in FlowStartNodes)
            {
                var controller = m_nodesControllers.FirstOrDefault(c => c.Model == node);
                if (controller != null)
                {
                    m_flowStartNodesControllers.Add(controller);
                }
            }
        }

        private void SyncDebugStartNodes()
        {
            Graph.RefreshDebugStartNodes();
            m_debugStartNodesControllers.Clear();
            foreach (var node in DebugStartNodes)
            {
                var controller = m_nodesControllers.FirstOrDefault(c => c.Model == node);
                if (controller != null)
                {
                    m_debugStartNodesControllers.Add(controller);
                }
            }
        }


        public void UnregisterNotification<T>(T model, Action onModelChanged) where T : ProcedureObject
        {

        }

        public void RegisterController(ProcedureObject model, Controller controller)
        {
            CleanUpZombies();
            if (!model)
            {
                Debug.LogError($"[<b>Procedure Controller</b>]: unable to register null model");
                return;
            }
            if (!m_modelControllers.TryGetValue(model, out List<Controller> controllers))
            {
                controllers = new List<Controller>();
                m_modelControllers[model] = controllers;
            }
            if (!controllers.Contains(controller))
            {
                var controllerType = controller.GetType();
                for (int i = 0; i < controllers.Count; i++)
                {
                    if (controllers[i].GetType() == controllerType)
                    {
                        controllers[i] = controller;
                        return;
                    }
                }
                controllers.Add(controller);

                // If it is a port controller, then add it
                if (controller is IPortController && !m_portsControllers.Contains(controller))
                {
                    m_portsControllers.Add(controller);
                }
            }
        }

        #region [  CLEANUP PART  ]

        private void CleanUpZombies()
        {
            foreach (var modelToRemove in m_modelControllers.Keys.Where(k => !k || k == null).ToArray())
            {
                m_modelControllers.Remove(modelToRemove);
            }
            // Transitions
            for (int i = 0; i < Model.Graph.Transitions.Count; i++)
            {
                if (!Transitions[i])
                {
                    Model.Graph.Transitions.RemoveAt(i--);
                    Model.Graph.MarkDirty();
                }
            }
        }

        public void Cleanup()
        {
            for (int i = 0; i < Model.Graph.Steps.Count; i++)
            {
                var step = Model.Graph.Steps[i];
                if (step)
                {
                    Cleanup(step);
                }
                else
                {
                    Model.Graph.Steps.RemoveAt(i--);
                }
            }
            for (int i = 0; i < Model.Graph.Nodes.Count; i++)
            {
                var node = Model.Graph.Nodes[i];
                if (node)
                {
                    Cleanup(node);
                }
                else
                {
                    Model.Graph.Nodes.RemoveAt(i--);
                    Model.Graph.MarkDirty();
                }
            }
            for (int i = 0; i < Model.Graph.Transitions.Count; i++)
            {
                var transition = Model.Graph.Transitions[i];
                if (transition)
                {
                    Cleanup(transition);
                }
                else
                {
                    Model.Graph.Transitions.RemoveAt(i--);
                    Model.Graph.MarkDirty();
                }
            }
            SyncAll();
        }

        private void Cleanup(BaseStep step)
        {
            for (int i = 0; i < step.Nodes.Count; i++)
            {
                var node = step.Nodes[i];
                if (node)
                {
                    Cleanup(node);
                }
                else
                {
                    step.Nodes.RemoveAt(i--);
                }
            }
        }

        private void Cleanup(BaseNode node)
        {
            if (node is GenericNode gNode)
            {
                for (int i = 0; i < gNode.FlowElements.Count; i++)
                {
                    var flowElem = gNode.FlowElements[i];
                    if (flowElem)
                    {
                        if (flowElem is BaseAction action)
                        {
                            Cleanup(action);
                        }
                        else if (flowElem is FlowConditionsContainer container)
                        {
                            Cleanup(container);
                        }
                    }
                    else
                    {
                        gNode.FlowElements.RemoveAt(i--);
                    }
                }
            }
        }

        private void Cleanup(BaseTransition transition)
        {
            for (int i = 0; i < transition.Actions.Count; i++)
            {
                var action = transition.Actions[i];
                if (action)
                {
                    Cleanup(action);
                }
                else
                {
                    transition.Actions.RemoveAt(i--);
                }
            }
        }

        private void Cleanup(FlowConditionsContainer flowContainer)
        {
            for (int i = 0; i < flowContainer.Conditions.Count; i++)
            {
                var condition = flowContainer.Conditions[i];
                if (condition)
                {
                    Cleanup(condition);
                }
                else
                {
                    flowContainer.Conditions.RemoveAt(i--);
                }
            }
        }

        private void Cleanup(BaseCondition condition)
        {
            if (condition is IConditionsContainer parent)
            {
                for (int i = 0; i < parent.Children.Count; i++)
                {
                    var child = parent.Children[i];
                    if (child)
                    {
                        Cleanup(child);
                    }
                    else
                    {
                        parent.Children.RemoveAt(i--);
                    }
                }
            }
        }

        private void Cleanup(BaseAction action)
        {
            if (action is AnimationComposer composer)
            {
                for (int i = 0; i < composer.AnimationBlocks.Count; i++)
                {
                    var block = composer.AnimationBlocks[i];
                    if (block)
                    {

                    }
                    else
                    {
                        composer.AnimationBlocks.RemoveAt(i--);
                    }
                }
            }
        }

        #endregion


        public void Search(string value)
        {
            SearchHub.Current.CurrentSearchValue = value;
        }

        public void ResetSearch()
        {
            SearchHub.Current.Reset();
        }

        public void SetAsStartNode(GraphObjectController controller)
        {
            if (controller.Model is BaseNode)
            {
                Graph.SetStartNode(controller.Model as BaseNode);
            }
        }

        public void RemoveFromStartNodes(GraphObjectController controller)
        {
            if (controller.Model is BaseNode)
            {
                Graph.RemoveStartNode(controller.Model as BaseNode);
            }
        }

        public void SetAsFlowStartNode(GraphObjectController controller)
        {
            if (controller.Model is BaseNode)
            {
                Graph.SetFlowStartNode(controller.Model as BaseNode);
            }
        }

        public void RemoveFromFlowStartNodes(GraphObjectController controller)
        {
            if (controller.Model is BaseNode)
            {
                Graph.RemoveFlowStartNode(controller.Model as BaseNode);
            }
        }

        public void SetAsDebugStartNode(GraphObjectController controller)
        {
            if (controller.Model is BaseNode node)
            {
                Graph.SetDebugStartNode(node);
            }
        }

        public void RemoveFromDebugStartNode(GraphObjectController controller)
        {
            if (controller.Model is BaseNode node)
            {
                Graph.RemoveDebugStartNode(node);
            }
        }

        public void UnregisterController(Controller controller)
        {
            var model = controller.GetModel();
            if (model is ProcedureObject && m_modelControllers.TryGetValue(model as ProcedureObject, out List<Controller> controllers))
            {
                controllers.Remove(controller);
                return;
            }

            m_modelControllers.Values.FirstOrDefault(c => c.Contains(controller))?.Remove(controller);
        }

        public IReadOnlyList<Controller> GetControllers(ProcedureObject model)
        {
            return m_modelControllers.TryGetValue(model, out List<Controller> controllers) ? controllers : null;
        }

        public T GetController<T>(ProcedureObject model) where T : Controller
        {
            return m_modelControllers.TryGetValue(model, out List<Controller> controllers) ? controllers.FirstOrDefault(c => c is T) as T : null;
        }

        public Controller GetPortController(ProcedureObject model)
        {
            return model && m_modelControllers.TryGetValue(model, out List<Controller> controllers) ? controllers.FirstOrDefault(c => c is IPortController) : null;
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            if (!(obj is Procedure))
            {
                return;
            }

            SyncAll();
        }

        public void RegisterNotification<T>(T model, Action action) where T : ProcedureObject
        {

        }

        public void RemoveNode(GraphObjectController controller)
        {
            if (controller.Model is BaseNode node && Graph.Nodes.Contains(node))
            {
                Undo.RegisterCompleteObjectUndo(Graph, "Removed node");
                if (Graph.Nodes.Remove(node))
                {
                    Graph.MarkDirty();
                    for (int i = 0; i < TransitionsControllers.Count; i++)
                    {
                        int count = TransitionsControllers.Count;
                        var transition = TransitionsControllers[i];
                        if (transition.FromModel == controller.Model || transition.ToModel == controller.Model)
                        {
                            UnregisterTransitionFromPort(transition, false);
                            transition.OnDisable();
                            if (count == TransitionsControllers.Count)
                            {
                                m_transitionControllers.RemoveAt(i);
                            }
                            i--;
                        }
                    }
                    if (node.Step)
                    {
                        Undo.RegisterCompleteObjectUndo(node.Step, "Remove node from Step");
                        node.Step = null;
                    }
                    UnregisterController(controller);

                    ProcedureObjectInspector.ResetSelectionFor(node);
                }
                SyncAll();
                NotifyChange(AnyThing);
            }
        }

        public string SerializeData(IEnumerable<Controller> elements)
        {
            SerializationNodesList nodes = new SerializationNodesList();
            foreach (var elem in elements)
            {
                var model = elem.GetModel();
                // Nodes inside Steps should not be serialized
                if ((model as BaseNode)?.Step != null && elements.Any(e => e.GetModel() == (model as BaseNode)?.Step))
                {
                    continue;
                }
                if (model is ScriptableObject)
                {
                    nodes.Append(model as ScriptableObject);
                }
            }

            return nodes.Seal().Serialize();
        }

        public bool CanDeserializeData(string data)
        {
            return SerializationNodesList.CanDeserialize(data);
        }

        public IEnumerable<object> UnserializeData(string operationName, string data)
        {
            List<object> deserializedObjects = new List<object>();
            var nodes = SerializationNodesList.Deserialize(data);
            if (nodes == null)
            {
                return deserializedObjects;
            }

            List<ScriptableObject> deserializedModels = nodes.DeserializeAll();

            List<GraphObject> objectsToMove = new List<GraphObject>();
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            foreach (var model in deserializedModels)
            {
                if (model is ProcedureObject pObj)
                {
                    // Set the current procedure
                    pObj.Procedure = null;
                    pObj.AssignProcedureToTree(Model, silently: false);
                    pObj.SaveToProcedure(Model);
                }
                if (model is BaseNode)
                {
                    var nodeModel = model as BaseNode;
                    Graph.Nodes.Add(model as BaseNode);
                    if (nodeModel.UI_Position.x < minX)
                    {
                        minX = nodeModel.UI_Position.x;
                    }
                    if (nodeModel.UI_Position.x > maxX)
                    {
                        maxX = nodeModel.UI_Position.x;
                    }
                    objectsToMove.Add(nodeModel);
                }
                else if (model is BaseStep)
                {

                    foreach (var nodeModel in (model as BaseStep).Nodes)
                    {
                        if (nodeModel.UI_Position.x < minX)
                        {
                            minX = nodeModel.UI_Position.x;
                        }
                        if (nodeModel.UI_Position.x > maxX)
                        {
                            maxX = nodeModel.UI_Position.x;
                        }
                        objectsToMove.Add(nodeModel);
                        Graph.Nodes.Add(nodeModel);
                    }
                    Graph.Steps.Add(model as BaseStep);
                }
                else if (model is BaseTransition)
                {
                    //Debug.Log($"[Transition]: {(model as BaseTransition).From} -> {(model as BaseTransition).To}");
                }
            }

            Graph.MarkDirty();

            Vector2 deltaMove = new Vector2(maxX - minX + 350, 0);
            foreach (var graphObject in objectsToMove)
            {
                graphObject.UI_Position += deltaMove;
            }

            bool wereMuted = MuteNotifications;
            MuteNotifications = true;
            Graph.Modified();
            MuteNotifications = wereMuted;
            NotifyChange(AnyThing);

            foreach (var model in deserializedModels)
            {
                if (model is ProcedureObject && m_modelControllers.TryGetValue(model as ProcedureObject, out List<Controller> controllers))
                {
                    deserializedObjects.AddRange(controllers);
                }
            }

            return deserializedObjects;
        }

        public void RemoveTransition(Controller controller)
        {
            if (controller != null)
            {
                controller.OnDisable();
                if (controller.GetModel() is ProcedureObject transition)
                {
                    transition.DestroyAsset();
                    ProcedureObjectInspector.ResetSelectionFor(transition);
                }
            }
        }

        public void RemoveStep(GraphObjectController controller)
        {
            if (controller.Model is BaseStep && Graph.Steps.Contains(controller.Model as BaseStep))
            {
                var step = controller.Model as BaseStep;
                Undo.RegisterCompleteObjectUndo(Graph, "Remove step");
                if (Graph.Steps.Remove(step))
                {
                    step.RemoveAllNodes(false);
                    UnregisterController(controller);
                    ProcedureObjectInspector.ResetSelectionFor(step);
                }
                SyncAll();
                NotifyChange(AnyThing);
            }
        }

        public void UpdateReacheability()
        {
            if (!WeavrEditor.Settings.GetValue("DataGraphLogic", true))
            {
                foreach (var node in NodesControllers)
                {
                    node.Reacheability = Reachability.Always;
                }
            }
            else
            {
                var graph = TransitionsControllers.Where(t => t?.From is BaseNodeController).ToLookup(t => t.From as BaseNodeController);

                HashSet<BaseNodeController> secondaryVisitedNodes = new HashSet<BaseNodeController>();
                foreach (var startNode in FlowStartNodesControllers)
                {
                    if (graph.Contains(startNode) && !secondaryVisitedNodes.Contains(startNode))
                    {
                        secondaryVisitedNodes.Add(startNode);
                        UpdateReacheability(Reachability.SecondaryFlow, startNode, graph, secondaryVisitedNodes);
                        startNode.Reacheability = Reachability.SecondaryFlow;
                    }
                }

                HashSet<BaseNodeController> primaryVisitedNodes = new HashSet<BaseNodeController>();
                foreach (var startNode in StartNodesControllers)
                {
                    if (graph.Contains(startNode) && !primaryVisitedNodes.Contains(startNode))
                    {
                        primaryVisitedNodes.Add(startNode);
                        UpdateReacheability(Reachability.PrimaryFlow, startNode, graph, primaryVisitedNodes);
                        startNode.Reacheability = Reachability.PrimaryFlow;
                    }
                }
                
                foreach (var unreacheableNode in NodesControllers.Except(primaryVisitedNodes).Except(secondaryVisitedNodes))
                {
                    unreacheableNode.Reacheability = Reachability.NotReacheable;
                }
            }
        }

        private void UpdateReacheability(Reachability reachability, BaseNodeController node, ILookup<BaseNodeController, TransitionController> graph, HashSet<BaseNodeController> visitedNodes)
        {
            foreach(var edge in graph[node])
            {
                if(edge.To is BaseNodeController nextNode && !visitedNodes.Contains(nextNode))
                {
                    visitedNodes.Add(nextNode);
                    UpdateReacheability(reachability, nextNode, graph, visitedNodes);
                    nextNode.Reacheability = reachability;
                }
            }
        }
    }
}
