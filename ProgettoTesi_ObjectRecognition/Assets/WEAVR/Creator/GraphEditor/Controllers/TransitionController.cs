using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace TXT.WEAVR.Procedure
{

    class TransitionController : GraphObjectController<BaseTransition>
    {
        public class Change
        {
            public const int FromControllerChanged = 1;
            public const int ToControllerChanged = 2;
            public const int IsMainFlow = 99;
        }

        private bool m_isMainFlow;
        private bool m_initializedCompletely;
        private List<BaseActionController> m_actionControllers;

        public ContextState CurrentState => Model.CurrentState;
        public IReadOnlyList<BaseActionController> ActionControllers => m_actionControllers;
        public IReadOnlyList<BaseAction> Actions => Model.Actions;

        public override bool HasPosition => true;

        public override bool IsSuperCollapsable => false;

        public override bool IsCollapsable => false;

        public float Priority
        {
            get => Model.Priority;
            set => Model.Priority = value;
        }

        public void ResetPriority() => Model.ResetPriority();

        public event Action<TransitionController, BaseAction> ActionAdded;
        public event Action<TransitionController, BaseAction> ActionRemoved;

        private GraphObjectController m_from;
        private GraphObjectController m_to;
        private Controller m_sourcePortController;

        public bool IsMainFlow
        {
            get => m_isMainFlow;
            set
            {
                if(m_isMainFlow != value)
                {
                    m_isMainFlow = value;
                    NotifyChange(Change.IsMainFlow);
                }
            }
        }

        public Controller Owner { get; private set; }

        public Controller SourcePort
        {
            get => m_sourcePortController;
            set
            {
                if(m_sourcePortController != value)
                {
                    m_sourcePortController = value;
                    Model.SourcePort = m_sourcePortController?.GetModel() as ITransitionOwner;
                }
            }
        }

        public virtual GraphObjectController From
        {
            get
            {
                if(m_from == null && FromModel)
                {
                    m_from = ViewController.GetController<GraphObjectController>(FromModel);
                }
                return m_from;
            }
            set
            {
                if(m_from != value)
                {
                    m_from = value;
                    Model.From = m_from.Model;
                    NotifyChange(Change.FromControllerChanged);
                }
            }
        }

        public virtual GraphObjectController To
        {
            get
            {
                if (m_to == null && ToModel)
                {
                    m_to = ViewController.GetController<GraphObjectController>(ToModel);
                }
                return m_to;
            }
            set
            {
                if(m_to != value)
                {
                    m_to = value;
                    Model.To = m_to.Model;
                    NotifyChange(Change.ToControllerChanged);
                }
            }
        }

        public virtual GraphObject FromModel
        {
            get => Model.From;
            set
            {
                Model.From = value;
            }
        }

        public virtual GraphObject ToModel
        {
            get => Model.To;
            set
            {
                Model.To = value;
            }
        }

        public bool InitializedCompletely => m_initializedCompletely;

        public void Add(BaseAction action)
        {
            Model.Add(action);
        }

        public void Remove(BaseAction action)
        {
            Model.Remove(action);
        }

        public override void ResetState()
        {
            base.ResetState();
            Model.MuteEvents = true;
            Model.CurrentState = ContextState.Standby;
            Model.MuteEvents = false;

            foreach(var action in ActionControllers)
            {
                action.ResetState();
            }

            NotifyChange(AnyThing);
        }


        public TransitionController(BaseTransition model, 
                                    Controller owner, 
                                    ProcedureController viewController) : base(viewController, model)
        {
            model.OnModified -= ModelModified;
            model.OnModified += ModelModified;
            m_actionControllers = new List<BaseActionController>();
            SyncActionsControllers();
            Owner = owner;
            if (model.SourcePort is ProcedureObject) {
                m_sourcePortController = viewController.GetPortController(model.SourcePort as ProcedureObject);
            }
            else if (owner is IPortController)
            {
                SourcePort = owner;
            }
            if (owner != null)
            {
                viewController?.RegisterTransitionToPort(this, owner);
            }
        }

        public TransitionController(BaseTransition model, 
                                    GraphObjectController from,
                                    GraphObjectController to,
                                    Controller owner, 
                                    ProcedureController viewController) : base(viewController, model)
        {
            model.OnModified -= ModelModified;
            model.OnModified += ModelModified;
            m_actionControllers = new List<BaseActionController>();
            SyncActionsControllers();
            Owner = owner;
            From = from;
            To = to;
            if (model.SourcePort is ProcedureObject)
            {
                m_sourcePortController = viewController.GetPortController(model.SourcePort as ProcedureObject);
            }
            else if(owner is IPortController)
            {
                SourcePort = owner;
            }
            if (owner != null)
            {
                viewController?.RegisterTransitionToPort(this, owner);
            }
        }

        public void CompleteInitialization()
        {
            if (!m_initializedCompletely)
            {
                m_initializedCompletely = true;
                From?.TransitionConnected(this);
                To?.TransitionConnected(this);
            }
        }
        
        private void SyncActionsControllers()
        {
            var newControllers = new List<BaseActionController>();
            foreach (var action in Actions)
            {
                var newController = m_actionControllers.Find(p => p.Model == action);
                if (newController == null) // If the controller does not exist for this model, create it
                {
                    if (action is BaseAction)
                    {
                        newController = new BaseActionController(action as BaseAction, this);
                    }
                    newController.ForceUpdate();
                }
                newControllers.Add(newController);
            }

            foreach (var deletedController in m_actionControllers.Except(newControllers))
            {
                deletedController.OnDisable();
            }
            m_actionControllers = newControllers;
        }

        public override void OnDisable()
        {
            From?.TransitionDisconnected(this);
            To?.TransitionDisconnected(this);
            (Owner as IPortController)?.OnTransitionDisabled(this);
            ViewController?.UnregisterTransitionFromPort(this);
            base.OnDisable();
        }

        protected override void ModelChanged(BaseTransition model)
        {
            //if (model == Model)
            //{
            //    NotifyChange(ModelHasChanges);
            //}
            SyncActionsControllers();
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