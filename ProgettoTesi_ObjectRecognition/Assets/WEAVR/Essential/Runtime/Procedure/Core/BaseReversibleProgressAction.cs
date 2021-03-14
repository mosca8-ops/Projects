using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class BaseReversibleProgressAction : BaseAction, IProgressElement, IFlowContextClosedElement
    {
        [SerializeField]
        [HideInInspector]
        private bool m_revertOnExit;

        public bool RevertOnExit
        {
            get => m_revertOnExit;
            set
            {
                if (m_revertOnExit != value)
                {
                    BeginChange();
                    m_revertOnExit = value;
                    PropertyChanged(nameof(RevertOnExit));
                }
            }
        }

        public float Progress { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetProgress();
        }

        public abstract void OnContextExit(ExecutionFlow flow);

        public virtual void ResetProgress()
        {
            Progress = 0;
        }

        public override void FastForward()
        {
            base.FastForward();
            Progress = 1;
        }
    }
}
