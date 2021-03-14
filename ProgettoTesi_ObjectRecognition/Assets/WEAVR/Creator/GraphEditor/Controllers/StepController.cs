using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class StepController : GraphObjectController<BaseStep>
    {
        public class Change
        {
            public const int NodeAdded = 1;
            public const int NodeRemoved = 2;
        }


        protected List<GraphObjectController> m_nodesControllers;
        
        public IReadOnlyList<BaseNode> Nodes => Model.Nodes;
        public IReadOnlyList<GraphObjectController> NodesControllers => m_nodesControllers;

        public override bool HasPosition => true;

        public override bool IsSuperCollapsable => true;

        public override bool IsCollapsable => true;

        public string Number { get => Model.Number; set => Model.Number = value; }
        public string Description { get => Model.Description; set => Model.Description = value; }
        public bool IsMandatory { get => Model.IsMandatory; private set => Model.IsMandatory = value; }

        public StepController(ProcedureController viewController, BaseStep model) : base(viewController, model)
        {
            model.OnModified -= ModelModified;
            model.OnModified += ModelModified;

            m_nodesControllers = new List<GraphObjectController>();
            SyncControllers();
        }

        private void SyncControllers()
        {
            var newControllers = new List<GraphObjectController>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node)
                {
                    try
                    {
                        var newController = m_nodesControllers.Find(p => p.Model == node) ?? ViewController.GetController<GraphObjectController>(node);
                        newControllers.Add(newController);
                    }
                    catch(System.Exception ex)
                    {
                        WeavrDebug.LogException(this, $"Unable to create NodeController for {node}", ex);
                    }
                }
                else
                {
                    Model.Nodes.RemoveAt(i--);
                }
            }

            foreach (var deletedController in m_nodesControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_nodesControllers = newControllers;
        }

        public virtual void AddNode(GraphObjectController nodeController)
        {
            if (nodeController != null && !m_nodesControllers.Contains(nodeController) && nodeController.Model is BaseNode)
            {
                m_nodesControllers.Add(nodeController);
                if (!Model.Nodes.Contains(nodeController.Model as BaseNode))
                {
                    Model.Nodes.Add(nodeController.Model as BaseNode);
                }
                NotifyChange(Change.NodeAdded);
            }
        }

        public virtual void AddNodes(IEnumerable<GraphObjectController> nodesControllers)
        {
            bool shouldNotify = false;
            foreach(var elem in nodesControllers)
            {
                if(elem != null && elem.Model is BaseNode node && !m_nodesControllers.Contains(elem))
                {
                    shouldNotify = true;
                    m_nodesControllers.Add(elem);
                    if (!Model.Nodes.Contains(node))
                    {
                        Model.Nodes.Add(node);
                        node.Step = Model;
                    }
                }
            }
            if (shouldNotify)
            {
                NotifyChange(Change.NodeAdded);
            }
        }

        public virtual void RemoveNodes(IEnumerable<GraphObjectController> nodesControllers)
        {
            bool shouldNotify = false;
            foreach (var node in nodesControllers)
            {
                if (m_nodesControllers.Remove(node))
                {
                    shouldNotify = true;
                    if (node.Model is BaseNode baseNode)
                    {
                        Model.Nodes.Remove(baseNode);
                        baseNode.Step = null;
                    }
                }
            }
            if (shouldNotify)
            {
                NotifyChange(Change.NodeRemoved);
            }
        }

        public virtual void RemoveNode(GraphObjectController nodeController)
        {
            if (m_nodesControllers.Remove(nodeController))
            {
                Model.Nodes.Remove(nodeController.Model as BaseNode);
                NotifyChange(Change.NodeRemoved);
            }
        }

        public override void ResetState()
        {
            base.ResetState();
        }

        protected override void ModelChanged(BaseStep obj)
        {
            SyncControllers();
            NotifyChange(ModelHasChanges);
        }

        private void ModelModified(ProcedureObject obj)
        {
            if (obj == Model)
            {
                ApplyChanges();

            }
        }
    }
}
