
using UnityEngine;

namespace TXT.WEAVR.InteractionUI
{
    public enum InputType
    {
        Mouse,
        Touch,
    }

    public struct GestureVector2
    {
        /// <summary>
        /// Converted value to handle different screen sizes (works on physical surface units: cm, mm, etc.)
        /// </summary>
        public Vector2 value;
        /// <summary>
        /// Value in pixels
        /// </summary>
        public Vector2 rawValue;

        public GestureVector2(Vector2 value, float factor)
        {
            rawValue = value;
            this.value = value * factor;
        }
    }

    public struct GestureFloat
    {
        /// <summary>
        /// Converted value to handle different screen sizes (works on physical surface units: cm, mm, etc.)
        /// </summary>
        public float value;
        /// <summary>
        /// Value in pixels
        /// </summary>
        public float rawValue;

        public GestureFloat(float value, float factor)
        {
            rawValue = value;
            this.value = value * factor;
        }
    }

    public delegate void RotateEvent(InputType inputType, Vector3 offset, Vector3 actual);
    public delegate void TranslateEvent(InputType inputType, Vector3 offset, Vector3 actual);
    public delegate void ZoomEvent(InputType inputType, float offset, Vector2 zoomCenter);
    public delegate void ClickEvent(InputType inputType, Vector2 screenPosition);

    public interface IInteractablePanel
    {
        bool Active { get; set; }

        event RotateEvent Rotated;
        event TranslateEvent Translated;
        event ZoomEvent Zoomed;
        event ClickEvent Clicked;

        void ResetInput();
    }
}