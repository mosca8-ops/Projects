using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Switch N-Way Events")]
    public class SwitchNWayEventsXApi : BaseInteractableXApi<AbstractNWaySwitch>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractNWaySwitch interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnStateChanged.AddListener(s => callbackToRaise($"SWITCH_{s}", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractNWaySwitch interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnStateChanged.RemoveListener(s => callbackToRaise($"SWITCH_{s}", interactable.gameObject.name, default));
        }
    }
}