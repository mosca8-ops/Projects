using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class ConditionsController : GraphObjectController<FlowConditionsContainer>
    {
        public class Change
        {
            public const int ConditionAdded = 1;
            public const int ConditionRemoved = 2;
            public const int ConditionsChanged = 3;
            public const int HasMainFlow = 99;
        }

        private bool m_hasMainFlow;
        public bool HasMainFlow
        {
            get => m_hasMainFlow;
            set
            {
                if(m_hasMainFlow != value)
                {
                    m_hasMainFlow = value;
                    ApplyMainFlow(value);
                    NotifyChange(Change.HasMainFlow);
                }
            }
        }

        private void ApplyMainFlow(bool value)
        {
            if (m_flowConditionControllers.Count > 0)
            {
                foreach (var controller in m_flowConditionControllers)
                {
                    controller.IsMainFlow = false;
                }
                if (value)
                {
                    foreach (var controller in m_flowConditionControllers)
                    {
                        if (controller.PortController.Transition)
                        {
                            controller.IsMainFlow = value;
                            return;
                        }
                    }
                }
            }
        }

        public void ApplyMainFlow()
        {
            ApplyMainFlow(HasMainFlow);
        }

        public override bool HasPosition => false;

        public override bool IsSuperCollapsable => false;

        public override bool IsCollapsable => false;

        public string ErrorMessage => Model.ErrorMessage;
        public bool HasErrors => !string.IsNullOrEmpty(Model.ErrorMessage);

        private List<FlowConditionController> m_flowConditionControllers;
        public IReadOnlyList<FlowConditionController> FlowConditionsControllers => m_flowConditionControllers;
        public IReadOnlyList<FlowCondition> FlowConditions => Model.Conditions;

        public ConditionsController(FlowConditionsContainer model, GraphObjectController owner) : base(owner.ViewController, model)
        {
            model.OnModified -= Model_OnModified;
            model.OnModified += Model_OnModified;
            m_flowConditionControllers = new List<FlowConditionController>();
            SyncControllers();
        }

        private void SyncControllers()
        {
            var newControllers = new List<FlowConditionController>();
            int conditionIndex = 0;
            foreach (var flowCondition in FlowConditions)
            {
                if (flowCondition)
                {

                    var newController = m_flowConditionControllers.Find(p => p.Model == flowCondition);
                    if (newController == null) // If the controller does not exist for this model, create it
                    {
                        newController = new FlowConditionController(flowCondition, this);
                        //newController.ForceUpdate();
                    }
                    newControllers.Add(newController);
                }
                else
                {
                    WeavrDebug.LogError(this, $"\nFlow condition {conditionIndex} is null");
                    var eventsMuted = Model.MuteEvents;
                    Model.MuteEvents = true;
                    Model.ErrorMessage += $"\nFlow condition {conditionIndex} is null";
                    Model.MuteEvents = eventsMuted;
                }
                conditionIndex++;
            }

            foreach (var deletedController in m_flowConditionControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_flowConditionControllers = newControllers;

            if(m_flowConditionControllers.Count == 0)
            {
                AddCondition();
            }

            ApplyMainFlow();
        }

        public void Add(FlowConditionController controller)
        {
            if (m_flowConditionControllers.Contains(controller))
            {
                WeavrDebug.LogError(this, $"Cannot add controller {controller}, it is already present");
                return;
            }

            m_flowConditionControllers.Add(controller);
            if (!Model.Conditions.Contains(controller.Model))
            {

                Model.Conditions.Add(controller.Model);
            }
            NotifyChange(Change.ConditionAdded);
        }

        public FlowConditionController AddCondition()
        {
            return Add(ProcedureObject.Create<FlowCondition>(ViewController.Model));
        }

        public FlowConditionController Add(FlowCondition condition)
        {
            if (Model.Conditions.Contains(condition))
            {
                WeavrDebug.LogError(this, $"Cannot add condition {condition}, it is already present");
                return m_flowConditionControllers.Find(c => c.Model == condition);
            }

            Model.Conditions.Add(condition);
            var newController = new FlowConditionController(condition, this);
            m_flowConditionControllers.Add(newController);
            NotifyChange(Change.ConditionAdded);

            return newController;
        }

        public bool Remove(FlowConditionController controller)
        {
            if (m_flowConditionControllers.Remove(controller))
            {
                Model.Conditions.Remove(controller.Model);
                NotifyChange(Change.ConditionRemoved);

                return true;
            }

            return false;
        }

        public override void ResetState()
        {
            base.ResetState();
            foreach(var condition in FlowConditionsControllers)
            {
                condition.ResetState();
            }
            Model.MuteEvents = true;
            Model.CurrentState = ExecutionState.NotStarted;
            Model.ErrorMessage = string.Empty;
            Model.Exception = null;
            Model.MuteEvents = false;
            NotifyChange(AnyThing);
        }

        private void Model_OnModified(ProcedureObject obj)
        {
            if (obj == Model)
            {
                ApplyChanges();
                NotifyChange(ModelHasChanges);
            }
        }

        protected override void ModelChanged(FlowConditionsContainer obj)
        {
            if (obj && obj.Exception != null && WeavrEditor.Settings.GetValue("LogErrors", false))
            {
                WeavrDebug.LogException(obj, obj.Exception);
                obj.Exception = null;
            }
            SyncControllers();
        }
    }
}
