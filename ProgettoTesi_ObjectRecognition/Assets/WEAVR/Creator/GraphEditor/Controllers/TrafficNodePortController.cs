using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class TrafficNodePortController : ProcedureObjectController<TrafficNode>, IPortController
    {
        public UnityEngine.Object PortModel => Model;

        public ContextState CurrentState => Model.CurrentState;

        private List<TransitionController> m_transitionsControllers;
        public GraphObjectController Owner { get; private set; }
        public IReadOnlyList<TransitionController> TransitionsControllers => m_transitionsControllers;
        public IReadOnlyList<BaseTransition> Transitions => Model.OutputTransitions;

        public TrafficNodePortController(GraphObjectController owner, TrafficNode model) : base(owner.ViewController, model)
        {
            Owner = owner;
            model.OnModified -= Model_OnModified;
            model.OnModified += Model_OnModified;
            
            m_transitionsControllers = new List<TransitionController>();
            SyncTransitions();
        }

        public override void ResetState()
        {
            base.ResetState();
            Model.CurrentState = ContextState.Standby;
        }

        private void SyncTransitions()
        {
            var newControllers = new List<TransitionController>();
            foreach (var transition in Transitions)
            {
                var newController = m_transitionsControllers.Find(p => p.Model == transition);
                if (newController == null) // If the controller does not exist for this model, create it
                {
                    //if (transition is BaseTransition)
                    //{
                    //    newController = new TransitionController(transition as BaseTransition, this, ViewController);
                    //}
                    //else
                    {
                        newController = new TransitionController(transition, this, ViewController);
                    }
                    newController.ForceUpdate();
                }
                newControllers.Add(newController);
            }

            foreach (var deletedController in m_transitionsControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_transitionsControllers = newControllers;
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            SyncTransitions();
        }

        private void Model_OnModified(ProcedureObject obj)
        {
            if(obj == Model)
            {
                ApplyChanges();
                NotifyChange(AnyThing);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            foreach(var transition in TransitionsControllers)
            {
                DestroyTransition(transition);
            }
            m_transitionsControllers.Clear();
        }

        private void DestroyTransition(TransitionController transitionController)
        {
            transitionController.OnDisable();
            transitionController.Dispose();
            transitionController = null;
        }

        public void CreateTransition(GraphObjectController controllerA, GraphObjectController controllerB)
        {
            var newTransition = ProcedureObject.Create<LocalTransition>(ViewController.Model);
            GraphUndo.RegisterCreatedObjectUndo(newTransition, "Created Transition");
            newTransition.From = controllerA.Model;
            newTransition.To = controllerB.Model;
            newTransition.SourcePort = Model;

            GraphUndo.RegisterCompleteObjectUndo(Model, "Added Transition");
            Model.OutputTransitions.Add(newTransition);
            Model.Modified();
        }

        public void DeleteTransition(TransitionController controller)
        {
            if (Model.OutputTransitions.Remove(controller.Model))
            {
                Model.Modified();
            }
        }

        public void OnTransitionDisabled(TransitionController transition)
        {
            for (int i = 0; i < m_transitionsControllers.Count; i++)
            {
                if(m_transitionsControllers[i].Model == transition.Model && Model.OutputTransitions.Remove(transition.Model))
                {
                    m_transitionsControllers.RemoveAt(i--);
                    Model.Modified();
                    return;
                }
            }
        }
    }
}
