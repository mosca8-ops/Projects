#if  WEAVR_EXTENSIONS_MRTK && TO_TEST 
using System;
using TXT.WEAVR.Common;
using UnityEngine;

namespace TXT.WEAVR.InteractionUI
{
    [Flags]
    public enum InteractionType
    {
        None = 0,
        PointerUp = 0x01,
        PointerDown = 0x02,
        PointerLongUp = 0x03,   // Pointer Up included...
    }

    /// <summary>
    /// Provides functionality for pointer events
    /// </summary>
    public interface IPointerHandler
    {
        /// <summary>
        /// Handle the pointer action with specified type and position
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pointerPosition"></param>
        void PointerAction(InteractionType type, Vector3 pointerPosition);
    }

    /// <summary>
    /// Defines an object which can be interacted with
    /// </summary>
    public interface IInteractiveObject
    {
        /// <summary>
        /// Whether this object can interact or not
        /// </summary>
        bool CanInteract { get; }

        /// <summary>
        /// Trigger the interaction with specified type and point
        /// </summary>
        /// <param name="type">The interaction type</param>
        /// <param name="interactionPoint">[Optional] The point where the interaction occurs</param>
        void Interact(InteractionType type, Vector3? interactionPoint = null);
    }

    /// <summary>
    /// A generic object which holds a text value
    /// </summary>
    public interface ITextInput
    {
        string TextValue { get; set; }
        void Clear();
    }
}
#endif
