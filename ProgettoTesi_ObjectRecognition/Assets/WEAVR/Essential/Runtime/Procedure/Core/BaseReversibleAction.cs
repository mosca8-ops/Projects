using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public abstract class BaseReversibleAction : BaseAction, IFlowContextClosedElement
    {
        [SerializeField]
        [HideInInspector]
        private bool m_undoOnExit;

        public bool RevertOnExit
        {
            get => m_undoOnExit;
            set
            {
                if (m_undoOnExit != value)
                {
                    BeginChange();
                    m_undoOnExit = value;
                    PropertyChanged(nameof(RevertOnExit));
                }
            }
        }

        public abstract void OnContextExit(ExecutionFlow flow);
    }
}
