using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Localization;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public class BaseNode : GraphObject, IFlowContext
    {
        //protected static readonly List<ProcedureObject> s_emptyPortsList = new List<ProcedureObject>();

        [SerializeField]
        [HideInInspector]
        [DoNotClone]
        protected BaseStep m_step;

        public BaseStep Step
        {
            get => m_step;
            set
            {
                if(m_step != value)
                {
                    BeginChange();
                    if (m_step)
                    {
                        m_step.Nodes.Remove(this);
                    }
                    m_step = value;
                    if (m_step && !m_step.Nodes.Contains(this))
                    {
                        m_step.Nodes.Add(this);
                    }
                    PropertyChanged(nameof(Step));
                }
            }
        }

        public virtual bool CanBeShared => false;
        
        protected ContextState m_currentState;

        public IProcedureStep ProcedureStep => Step ? Step : this as IProcedureStep;

        public ContextState CurrentState {
            get => m_currentState;
            set {
                if(m_currentState != value)
                {
                    m_currentState = value;
                    PropertyChanged(nameof(CurrentState));
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeGUID();
            CurrentState = ContextState.Standby;
        }

        //public virtual IEnumerable<ProcedureObject> GetOutputPorts()
        //{
        //    return s_emptyPortsList;
        //}

        //public virtual IEnumerable<ProcedureObject> GetInputPorts()
        //{
        //    if(m_selfInputPortList == null)
        //    {
        //        m_selfInputPortList = new List<ProcedureObject>() { this };
        //    }
        //    return m_selfInputPortList;
        //}

        public virtual void OnExecutionEnded(ExecutionFlow flow)
        {
            
        }

        public virtual void OnExecutionStarted(ExecutionFlow flow)
        {
            
        }
    }
}
