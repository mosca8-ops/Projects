using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Push Button Events")]
    public class PushButtonEventsXApi : BaseInteractableXApi<AbstractPushButton>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractPushButton interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnDown.AddListener(() => callbackToRaise("BUTTON_DOWN", interactable.gameObject.name, default));
            interactable.OnUp.AddListener(() => callbackToRaise("BUTTON_UP", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractPushButton interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnDown.RemoveListener(() => callbackToRaise("BUTTON_DOWN", interactable.gameObject.name, default));
            interactable.OnUp.RemoveListener(() => callbackToRaise("BUTTON_UP", interactable.gameObject.name, default));
        }
    }
}