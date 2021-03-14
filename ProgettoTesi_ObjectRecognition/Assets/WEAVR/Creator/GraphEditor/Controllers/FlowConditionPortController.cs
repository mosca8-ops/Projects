using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    class FlowConditionPortController : ProcedureObjectController<FlowCondition>, ISinglePortController
    {
        private bool m_isMainFlow;
        public bool IsMainFlow
        {
            get => m_isMainFlow;
            set
            {
                if (TransitionController != null)
                {
                    TransitionController.IsMainFlow = value;
                }
                if (m_isMainFlow != value)
                {
                    m_isMainFlow = value;
                    NotifyChange(AnyThing);
                }
            }
        }

        public Controller Owner { get; private set; }
        private TransitionController m_transitionController;

        public TransitionController TransitionController
        {
            get => m_transitionController;
            set
            {
                if(m_transitionController != value)
                {
                    if (m_transitionController != null)
                    {
                        if (m_transitionController.Model != value?.Model)
                        {
                            ViewController.UnregisterTransitionFromPort(m_transitionController);
                            value?.Model.DestroyAsset();
                        }
                        m_transitionController.OnDisable();
                        m_transitionController?.Dispose();
                        m_transitionController = null;
                    }
                    m_transitionController = value;
                    if (m_transitionController != null && m_transitionController.Model != Transition)
                    {
                        Model.Transition = m_transitionController.Model;
                    }
                    ApplyMainFlow();
                    NotifyChange(AnyThing);
                }
            }
        }

        private void ApplyMainFlow()
        {
            var container = (Owner as FlowConditionController)?.Container;
            if (container != null)
            {
                container.ApplyMainFlow();
            }
            else
            {
                TransitionController.IsMainFlow = IsMainFlow;
            }
        }

        public BaseTransition Transition => Model.Transition;

        public Object PortModel => Model;

        public FlowConditionPortController(FlowCondition model, Controller owner, ProcedureController viewController) : base(viewController, model)
        {
            Owner = owner;
            
            SyncTransitionController();

            UnityEditor.Undo.undoRedoPerformed += () =>
            {
                SyncTransitionController();
                NotifyChange(AnyThing);
            };
        }

        private void SyncTransitionController()
        {
            if(m_transitionController != null && m_transitionController.Model != Transition)
            {
                DestroyTransition();
            }
            if (m_transitionController == null && Transition)
            {
                m_transitionController = new TransitionController(Transition, this, ViewController);
                m_transitionController.IsMainFlow = IsMainFlow;
            }
        }

        private void DestroyTransition()
        {
            m_transitionController.OnDisable();
            m_transitionController?.Dispose();
            m_transitionController = null;
        }

        public void CreateTransition(GraphObjectController nodeA, GraphObjectController nodeB)
        {
            var newTransition = ProcedureObject.Create<LocalTransition>(ViewController.Model);
            GraphUndo.RegisterCreatedObjectUndo(newTransition, "Created Transition");
            TransitionController = new TransitionController(newTransition, nodeA, nodeB, this, ViewController);
            NotifyChange(AnyThing);
            //ViewController.AddTransition(transition);
        }

        public void DeleteTransition(TransitionController transition)
        {
            if (m_transitionController == transition || m_transitionController?.Model == transition?.Model)
            {
                TransitionController = null;
                //ViewController.RemoveTransition(transition);
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (m_transitionController != null)
            {
                DestroyTransition();
            }
        }

        protected override void ModelChanged(Object obj)
        {

        }

        protected void ModelModified(ProcedureObject model)
        {
            if (model == Model)
            {
                ApplyChanges();
                NotifyChange(ModelHasChanges);
            }
        }

        public void OnTransitionDisabled(TransitionController transition)
        {
            if(transition != null && transition.Model == m_transitionController.Model)
            {
                if (m_transitionController.Model != transition?.Model)
                {
                    ViewController.UnregisterTransitionFromPort(m_transitionController);
                    transition?.Model.DestroyAsset();
                }
                //m_transitionController.OnDisable();
                //m_transitionController.Dispose();
                Model.Transition = null;
                m_transitionController = null;
                ApplyMainFlow();
            }
        }
    }
}
