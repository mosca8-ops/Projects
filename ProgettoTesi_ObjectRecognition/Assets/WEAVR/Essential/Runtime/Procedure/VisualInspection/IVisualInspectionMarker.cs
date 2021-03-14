using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    public enum VisualInspectionState
    {
        NotReady,
        OutOfView,
        Inspecting,
        Inspected,
    }
    
    public interface IVisualInspectionMarker : IVisualMarker
    {
        VisualInspectionState State { get; set; }
        void StartInspection(IVisualInspectionLogic inspectionTarget, IVisualInspector inspector);
        void EndInspection();
    }
}
