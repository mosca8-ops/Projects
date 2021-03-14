using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    enum Reachability { Always, PrimaryFlow, SecondaryFlow, NotReacheable }
    abstract class BaseNodeController<T> : BaseNodeController where T : BaseNode
    {
        private T m_specificModel;
        public new T Model { get => m_specificModel; }
        
        public BaseNodeController(ProcedureController viewController, T model) : base(viewController, model)
        {
            m_specificModel = model;
        }

        protected override void ModelChanged(BaseNode obj)
        {
            if(obj is T node)
            {
                ModelChanged(node);
            }
        }

        protected abstract void ModelChanged(T obj);
    }

    abstract class BaseNodeController : GraphObjectController<BaseNode>
    {
        public class Change
        {
            public const int Reacheability = 10;
        }

        private Reachability m_isReachable = Reachability.Always;
        public Reachability Reacheability
        {
            get => m_isReachable;
            set
            {
                SetReacheability(value);
            }
        }

        private Reachability SetReacheability(Reachability value)
        {
            if (m_isReachable != value)
            {
                m_isReachable = value;
                NotifyChange(Change.Reacheability);
            }
            return value;
        }

        public Reachability UpdateReacheability()
        {
            if(!WeavrEditor.Settings.GetValue("DataGraphLogic", true))
            {
                return SetReacheability(Reachability.Always);
            }
            else if (ViewController.Graph.IsReacheableFromStartPoints(Model))
            {
                return SetReacheability(Reachability.PrimaryFlow);
            }
            else if (ViewController.Graph.IsReacheableFromFlowStartPoints(Model))
            {
                return SetReacheability(Reachability.SecondaryFlow);
            }
            return SetReacheability(Reachability.NotReacheable);
        }

        public ContextState CurrentState => Model.CurrentState;

        public BaseNodeController(ProcedureController viewController, BaseNode model) : base(viewController, model)
        {
        }

        public override bool HasPosition => true;

        public override bool IsSuperCollapsable => false;

        public override bool IsCollapsable => false;
    }
}
