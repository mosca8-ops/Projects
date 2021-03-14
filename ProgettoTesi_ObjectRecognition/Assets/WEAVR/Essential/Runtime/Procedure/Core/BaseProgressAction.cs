using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{

    public abstract class BaseProgressAction : BaseAction, IProgressElement
    {
        public float Progress { get; protected set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetProgress();
        }

        public virtual void ResetProgress()
        {
            Progress = 0;
        }
    }
}
