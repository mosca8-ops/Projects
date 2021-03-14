using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class GenericNodeController : BaseNodeController<GenericNode>, IOutputPortsProvider
    {
        public override bool IsSuperCollapsable => true;

        public override bool IsCollapsable => true;

        public string Number { get => Model.Number; private set => Model.Number = value; }
        public string Description { get => Model.Description; private set => Model.Description = value; }
        public bool IsMandatory { get => Model.IsMandatory; set => Model.IsMandatory = value; }
        public bool CanBeMandatory => ViewController.Model.Configuration.CanSkipSteps || ViewController.Model.ExecutionModes.Any(m => m.UsesStepPrevNext);
        public bool IsPartOfStep => Model.Step;

        protected List<GraphObjectController> m_elementsControllers;

        public ConditionsController ConditionsContainer { get; private set; }
        public IReadOnlyList<GraphObject> FlowElements => Model.FlowElements;
        public IReadOnlyList<GraphObjectController> FlowElementsControllers => m_elementsControllers;

        public IEnumerable<IPortController> OutputPorts => ConditionsContainer?.FlowConditionsControllers.Select(c => c.PortController);

        public Object PortModel => Model;

        //public IEnumerable<TransitionController> Transitions => throw new System.NotImplementedException();

        public GenericNodeController(ProcedureController viewController, GenericNode model) : base(viewController, model)
        {
            model.OnModified -= ModelModified;
            model.OnModified += ModelModified;

            m_elementsControllers = new List<GraphObjectController>();
            SyncControllers();
        }

        private void SyncControllers()
        {
            var newControllers = new List<GraphObjectController>();
            var elementsToRemove = new List<GraphObject>();
            foreach (var graphElement in FlowElements)
            {
                var newController = m_elementsControllers.Find(p => p.Model == graphElement);
                if (newController == null) // If the controller does not exist for this model, create it
                {
                    if (graphElement is BaseAction)
                    {
                        newController = new BaseActionController(graphElement as BaseAction, this);
                    }
                    else if(graphElement is FlowConditionsContainer)
                    {
                        newController = new ConditionsController(graphElement as FlowConditionsContainer, this);
                        ConditionsContainer = newController as ConditionsController;
                    }
                    else if(graphElement)
                    {
                        Debug.LogError($"[{nameof(GenericNodeController)}]: cannot find controller for {graphElement?.name}");
                    }
                    else
                    {
                        Debug.LogError($"[{nameof(GenericNodeController)}]: flow element is null, eliminating");
                        elementsToRemove.Add(graphElement);
                    }
                    newController?.ForceUpdate();
                }
                if (newController != null)
                {
                    newControllers.Add(newController);
                }
            }

            foreach(var elem in elementsToRemove)
            {
                Model.FlowElements.Remove(elem);
            }

            foreach (var deletedController in m_elementsControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_elementsControllers = newControllers;

            if(ConditionsContainer == null)
            {
                ConditionsContainer = new ConditionsController(ProcedureObject.Create<FlowConditionsContainer>(ViewController.Model), this);
                m_elementsControllers.Add(ConditionsContainer);
                Model.FlowElements.Add(ConditionsContainer.Model);
            }

            if (!CanBeMandatory)
            {
                Model.MuteEvents = true;
                IsMandatory = true;
                Model.MuteEvents = false;
            }

            if (ConditionsContainer != null)
            {
                ConditionsContainer.HasMainFlow = !IsMandatory;
            }
        }

        public virtual void AddAction(BaseActionController action)
        {
            if (FlowElements.Contains(action.Model)) { return; }
            m_elementsControllers.Add(action);
            Model.FlowElements.Add(action.Model);
        }

        public virtual void AddAction(BaseActionController action, int index)
        {

        }

        public virtual void RemoveAction(BaseActionController action)
        {

        }

        public override void ResetState()
        {
            base.ResetState();
            foreach(var obj in FlowElementsControllers)
            {
                obj.ResetState();
            }

            Model.MuteEvents = true;
            (Model.FlowElements.FirstOrDefault(v => v is FlowConditionsContainer) as FlowConditionsContainer)?.ResetEvaluations();
            Model.CurrentState = ContextState.Standby;
            Model.MuteEvents = false;
            NotifyChange(AnyThing);
        }

        protected override void ModelChanged(GenericNode obj)
        {
            SyncControllers();
            NotifyChange(ModelHasChanges);
        }

        private void ModelModified(ProcedureObject obj)
        {
            if(obj == Model)
            {
                ApplyChanges();
            }
        }
    }
}
