using System;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public interface IVisualInspector
    {
        bool IsAllowedToInspect { get; set; }
        bool CanSee(Bounds bounds, float maxDistance);
    }

    public interface IVisualInspectionLogic
    {
        void InspectTarget(IVisualInspector inspector, GameObject target, Pose localPose, Bounds? bounds);
        bool IsInspected { get; }
        bool TargetIsVisible { get; }

        void ResetValues();

        void ForceInspectionDone();
    }

    public interface IFocusInspectionLogic : IVisualInspectionLogic
    {
        float InspectionDistance { get; set; }
        float InspectionTime { get; set; }
        bool IsAccumulativeInspection { get; set; }
    }

    public interface IVisualInspectionEvents
    {
        event Action<GameObject> InspectionTargetChanged;
        event Action OnInspectionStarted;
        event Action<float> OnOngoingInspectionNormalized;
        event Action OnInspectionLost;
        event Action OnInspectionDone;
    }
}