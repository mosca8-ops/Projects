using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Procedure
{
    public abstract class AbstractInspectionLogic : MonoBehaviour, IVisualInspectionLogic
    {
        public abstract bool IsInspected { get; protected set; }

        public abstract bool TargetIsVisible { get; protected set; }

        public virtual void ForceInspectionDone()
        {
            StartCoroutine(ForcedDoneInspection());
        }

        private IEnumerator ForcedDoneInspection()
        {
            IsInspected = true;
            yield return new WaitForEndOfFrame();
            IsInspected = true;
        }

        public abstract void InspectTarget(IVisualInspector inspector, GameObject target, Pose localPose, Bounds? bounds);

        public virtual void ResetValues()
        {
            
        }
    }
}
