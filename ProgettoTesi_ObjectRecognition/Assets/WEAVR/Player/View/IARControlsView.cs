using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.AR;
using TXT.WEAVR.Core;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.Events;

namespace TXT.WEAVR.Player.Views
{
    public interface IARControlsView : IView
    {
        ARTargetMode CurrentMode { get; set; }
        float Scale { get; set; }

        void TargetDetectionChanged(bool targetIsSet);

        void SetARInteractivity(bool buttonsActive);
        void EnableAR(bool enable);
        void EnableModeSelection(bool enable);
        void EnableAdvancedPlacement(bool enable);

        event OnValueChanged<bool> OnAREnabledChanged;
        event OnValueChanged<bool> OnPlacementUnlockChanged;
        event OnValueChanged<ARTargetMode> OnModeChanged;

        event UnityAction<float> OnRelativeRotationX;
        event UnityAction<float> OnRelativeRotationY;
        event UnityAction<float> OnRelativeRotationZ;
        event UnityAction OnARSaveRotation;
        event UnityAction OnARResetRotation;

        event UnityAction<float> OnRelativePositionX;
        event UnityAction<float> OnRelativePositionY;
        event UnityAction<float> OnRelativePositionZ;
        event UnityAction OnARSavePosition;
        event UnityAction OnARResetPosition;

        event Action<float> OnScaleChange;
        event UnityAction OnSaveScale;
        event UnityAction OnResetScale;
    }
}