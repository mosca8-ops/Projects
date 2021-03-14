using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{
    [AddComponentMenu("WEAVR/Analytics/XAPI/Connectable Events")]
    public class ConnectableEventsXApi : BaseInteractableXApi<AbstractConnectable>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractConnectable interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnConnected.AddListener(() => callbackToRaise("CONNECT", interactable.gameObject.name, default));
            interactable.OnDisconnected.AddListener(() => callbackToRaise("DISCONNECT", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractConnectable interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.OnConnected.RemoveListener(() => callbackToRaise("CONNECT", interactable.gameObject.name, default));
            interactable.OnDisconnected.RemoveListener(() => callbackToRaise("DISCONNECT", interactable.gameObject.name, default));
        }
    }
}