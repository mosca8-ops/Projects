using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    abstract class GraphObjectController<T> : GraphObjectController where T : GraphObject
    {
        public GraphObjectController(ProcedureController viewController, T model) : base(viewController, model)
        {
            m_specificModel = model;
        }

        private T m_specificModel;
        public new T Model { get => m_specificModel; }

        protected override void ModelChanged(Object obj)
        {
            if(obj is T)
            {
                ModelChanged(obj as T);
            }
        }

        protected abstract void ModelChanged(T obj);
    }

    abstract class GraphObjectController : ProcedureObjectController<GraphObject>
    {
        public GraphObjectController(ProcedureController viewController, GraphObject model) : base(viewController, model)
        {
        }
        
        public abstract bool HasPosition { get; }
        public abstract bool IsSuperCollapsable { get; }
        public abstract bool IsCollapsable { get; }


        public virtual string Title { get => Model.Title; set => Model.Title = value; }
        public virtual Vector2 Position {
            get => Model.UI_Position;
            set {
                bool wereMuted = Model.MuteEvents;
                Model.MuteEvents = true;
                Model.UI_Position = value;
                Model.MuteEvents = wereMuted;
            }
        }
        public virtual bool Expanded { get => Model.UI_Expanded; set => Model.UI_Expanded = value; }
        public virtual bool SuperCollapsed { get => Model.UI_SuperCollapsed; set => Model.UI_SuperCollapsed = value; }
        
        public virtual void TransitionConnected(TransitionController transition) { }
        public virtual void TransitionDisconnected(TransitionController transition) { }

        internal void ForceUpdate()
        {
            ModelChanged(Model);
        }
    }
}
