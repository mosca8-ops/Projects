using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Switch 3-Way Events")]
    public class Switch3WayEventsXApi : BaseInteractableXApi<AbstractThreeWaySwitch>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractThreeWaySwitch interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnDown.AddListener(() => callbackToRaise("SWITCH_DOWN", interactable.gameObject.name, default));
            interactable.OnMiddle.AddListener(() => callbackToRaise("SWITCH_MIDDLE", interactable.gameObject.name, default));
            interactable.OnUp.AddListener(() => callbackToRaise("SWITCH_UP", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractThreeWaySwitch interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnDown.RemoveListener(() => callbackToRaise("SWITCH_DOWN", interactable.gameObject.name, default));
            interactable.OnMiddle.RemoveListener(() => callbackToRaise("SWITCH_MIDDLE", interactable.gameObject.name, default));
            interactable.OnUp.RemoveListener(() => callbackToRaise("SWITCH_UP", interactable.gameObject.name, default));
        }
    }
}