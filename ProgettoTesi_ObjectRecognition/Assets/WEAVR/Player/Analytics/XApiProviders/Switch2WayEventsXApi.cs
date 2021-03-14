using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Switch 2-Way Events")]
    public class Switch2WayEventsXApi : BaseInteractableXApi<AbstractTwoWaySwitch>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractTwoWaySwitch interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnDown.AddListener(() => callbackToRaise("SWITCH_DOWN", interactable.gameObject.name, default));
            interactable.OnUp.AddListener(() => callbackToRaise("SWITCH_UP", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractTwoWaySwitch interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnDown.RemoveListener(() => callbackToRaise("SWITCH_DOWN", interactable.gameObject.name, default));
            interactable.OnUp.RemoveListener(() => callbackToRaise("SWITCH_UP", interactable.gameObject.name, default));
        }
    }
}