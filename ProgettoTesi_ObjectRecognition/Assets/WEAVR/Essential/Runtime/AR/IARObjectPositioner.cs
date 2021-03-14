using System.Threading.Tasks;
using TXT.WEAVR.Core;
using UnityEngine;

namespace TXT.WEAVR.AR
{
    public enum ARTargetMode
    {
        Surface,
        Marker
    }

    public interface IARObjectPositioner
    {
        bool Active { get; set; }
        bool ShowLineToSurface { get; set; }
        bool ShowWorldAxes { get; set; }
        ARTargetMode CurrentARTargetMode { get; set; }

        Gradient LineToSurfaceGradient { get; set; }
        Camera ARCamera { get; }
        GameObject ARTarget { get; set; }

        Vector3 PositionOffset { get; set; }
        Vector3 RotationOffset { get; set; }
        float Scale { get; set; }

        event OnValueChanged<bool> MarkerAquired;
        event OnValueChanged<bool> ObjectSetOnSurface;

        Task<bool> CheckIfARIsSupported();

        void StopTracking();
        void ResumeTracking();
        
        void ResetPosition();
        void ResetRotation();
        void ResetScale();

        void SaveRotation();
        void SavePosition();
        void SaveScale();

        void PositionOnSurface(Vector2 touchPosition);
        bool TryMoveOnSurface(Vector2 touchPosition, bool isDeltaMove);
    }
}
