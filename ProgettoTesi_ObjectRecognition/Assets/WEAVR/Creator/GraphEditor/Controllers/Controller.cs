using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TXT.WEAVR.Procedure
{
    class ControllerEvent
    {
        public IControlledElement target = null;
    }

    abstract class Controller
    {
        public bool DisableCalled { get; set; } = false;

        public virtual void OnDisable()
        {
            if (DisableCalled)
                Debug.Log(GetType().Name + ".Disable called twice");

            DisableCalled = true;
            foreach (var element in AllChildren)
            {
                element.OnDisable();
            }
        }

        protected bool m_highlighted;

        public virtual bool IsHighlighted
        {
            get => m_highlighted;
            set
            {
                if(m_highlighted != value)
                {
                    m_highlighted = value;
                    NotifyChange(Highlight);
                }
            }
        }

        public abstract UnityEngine.Object GetModel();

        public void RegisterHandler(IControlledElement handler)
        {
            //Debug.Log("RegisterHandler  of " + handler.GetType().Name + " on " + GetType().Name );

            if (m_eventHandlers.Contains(handler))
                Debug.LogError("Handler registered twice");
            else
            {
                m_eventHandlers.Add(handler);

                NotifyEventHandler(handler, AnyThing);
            }
        }

        public void UnregisterHandler(IControlledElement handler)
        {
            m_eventHandlers.Remove(handler);
        }

        public const int AnyThing = -1;
        public const int ModelHasChanges = 0;
        public const int Highlight = 10000;

        public bool MuteNotifications { get; protected set; }

        protected void NotifyChange(int eventID)
        {
            if (MuteNotifications) { return; }
            var eventHandlers = m_eventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers)
            {
                NotifyEventHandler(eventHandler, eventID);
            }
        }

        void NotifyEventHandler(IControlledElement eventHandler, int eventID)
        {
            ControllerChangedEvent e = new ControllerChangedEvent();
            e.controller = this;
            e.target = eventHandler;
            e.change = eventID;
            eventHandler.OnControllerChanged(ref e);
            if (e.IsPropagationStopped)
            {
                return;
            }
            if (eventHandler is VisualElement)
            {
                var element = eventHandler as VisualElement;
                eventHandler = element.GetFirstAncestorOfType<IControlledElement>();
                while (eventHandler != null)
                {
                    eventHandler.OnControllerChanged(ref e);
                    if (e.IsPropagationStopped)
                    {
                        break;
                    }
                    eventHandler = (eventHandler as VisualElement).GetFirstAncestorOfType<IControlledElement>();
                }
            }
        }

        public void SendEvent(ControllerEvent e)
        {
            var eventHandlers = m_eventHandlers.ToArray(); // Some notification may trigger Register/Unregister so duplicate the collection.

            foreach (var eventHandler in eventHandlers.OfType<IControllerListener>())
            {
                eventHandler.OnControllerEvent(e);
            }
        }

        public abstract void ApplyChanges();


        public virtual  IEnumerable<Controller> AllChildren
        {
            get { return Enumerable.Empty<Controller>(); }
        }

        private List<IControlledElement> m_eventHandlers = new List<IControlledElement>();
    }

    abstract class Controller<T> : Controller 
        where T : UnityEngine.Object
    {
        T m_model;

        public Controller(T model)
        {
            m_model = model;
        }

        protected abstract void ModelChanged(UnityEngine.Object obj);

        public override void ApplyChanges()
        {
            ModelChanged(Model);

            foreach (var controller in AllChildren)
            {
                controller.ApplyChanges();
            }
        }

        public T Model { get { return m_model; } }

        public override UnityEngine.Object GetModel()
        {
            return m_model as UnityEngine.Object;
        }
    }

    abstract class ProcedureObjectController<T> : Controller<T>, IDisposable where T : ProcedureObject
    {
        ProcedureController m_viewController;

        public ProcedureObjectController(ProcedureController viewController, T model) : base(model)
        {
            m_viewController = viewController;
            m_viewController?.RegisterController(model, this);
            m_viewController?.RegisterNotification(model, OnModelChanged);
        }

        public ProcedureController ViewController { get { return m_viewController; } }

        public override void OnDisable()
        {
            m_viewController?.UnregisterNotification(Model, OnModelChanged);
            m_viewController?.UnregisterController(this);
            base.OnDisable();
        }

        void OnModelChanged()
        {
            ModelChanged(Model);
        }

        public virtual string Name
        {
            get
            {
                return Model.name;
            }
        }

        public override UnityEngine.Object GetModel()
        {
            return Model;
        }

        public void Dispose()
        {
            //Debug.Log($"Disposing {this}");
            m_viewController?.UnregisterController(this);
        }

        public virtual void ResetState()
        {
            
        }


        //~ProcedureObjectController()
        //{
        //    Dispose();
        //}
    }

    struct ControllerChangedEvent
    {
        public IControlledElement target;
        public Controller controller;
        public int change;

        bool m_propagationStopped;
        public void StopPropagation()
        {
            m_propagationStopped = true;
        }

        public bool IsPropagationStopped
        { get { return m_propagationStopped; } }
    }
}
