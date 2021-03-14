using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public abstract class BaseAction : GraphObject, IFlowAsyncElement, IRequiresValidation
    {
        [SerializeField]
        [DoNotClone]
        private List<ExecutionMode> m_executionModes;
        [SerializeField]
        protected int m_asyncThread;
        [SerializeField]
        protected int m_variant;

        [NonSerialized]
        private string m_errorMessage;

        [SerializeField]
        public int separator;

        private ExecutionState m_state = ExecutionState.NotStarted;

        public List<ExecutionMode> ExecutionModes
        {
            get
            {
                if(m_executionModes == null)
                {
                    m_executionModes = new List<ExecutionMode>();
                }
                return m_executionModes;
            }
        }

        public ExecutionState CurrentState
        {
            get { return m_state; }
            set
            {
                if(m_state != value)
                {
                    BeginChange();
                    m_state = value;
                    OnStateChanged(value);
                }
            }
        }
        
        public int AsyncThread {
            get => m_asyncThread;
            set
            {
                if(m_asyncThread != value)
                {
                    BeginChange();
                    m_asyncThread = value;
                    PropertyChanged(nameof(AsyncThread));
                }
            }
        }

        public string ErrorMessage {
            get => m_errorMessage;
            set
            {
                if(m_errorMessage != value)
                {
                    m_errorMessage = value;
                    PropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        public Exception Exception { get; set; }

        public int Variant
        {
            get => m_variant;
            set
            {
                if(m_variant != value)
                {
                    BeginChange();
                    m_variant = value;
                    PropertyChanged(nameof(Variant));
                }
            }
        }

        public virtual void OnValidate()
        {

        }

        protected virtual void OnStateChanged(ExecutionState value)
        {
            StateChanged?.Invoke(this, value);
        }

        public event OnExecutionStateChanged StateChanged;
        
        public virtual void OnStart(ExecutionFlow flow, ExecutionMode executionMode) { }
        public abstract bool Execute(float dt);
        public virtual void OnStop() { }

        public bool CanExecute(ExecutionMode executionMode)
        {
            return m_executionModes == null || m_executionModes.Contains(executionMode);
        }

        public virtual void OnPause() { }

        public virtual void OnResume() { }

        public virtual void FastForward()
        {
            //OnStop();
        }
    }
}
