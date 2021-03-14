using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Door Events")]
    public class DoorEventsXApi : BaseInteractableXApi<AbstractDoor>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractDoor door, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            door.OnOpening.AddListener(() => callbackToRaise("OPEN", door.gameObject.name, default));
            door.OnClosed.AddListener(() => callbackToRaise("CLOSE", door.gameObject.name, default));
            door.OnLocked.AddListener(() => callbackToRaise("LOCK", door.gameObject.name, default));
            door.OnUnlocked.AddListener(() => callbackToRaise("UNLOCK", door.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractDoor door, XAPIEventDelegate callbackToRaise)
        {
            door.OnOpening.RemoveListener(() => callbackToRaise("OPEN", door.gameObject.name, default));
            door.OnClosed.RemoveListener(() => callbackToRaise("CLOSE", door.gameObject.name, default));
            door.OnLocked.RemoveListener(() => callbackToRaise("LOCK", door.gameObject.name, default));
            door.OnUnlocked.RemoveListener(() => callbackToRaise("UNLOCK", door.gameObject.name, default));
        }
    }
}