using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Grabbable Events")]
    public class GrabbableEventsXApi : BaseInteractableXApi<AbstractGrabbable>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractGrabbable interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.onGrab.AddListener(() => callbackToRaise("GRAB", interactable.gameObject.name, default));
            interactable.onUngrab.AddListener(() => callbackToRaise("UNGRAB", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractGrabbable interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.onGrab.RemoveListener(() => callbackToRaise("GRAB", interactable.gameObject.name, default));
            interactable.onUngrab.RemoveListener(() => callbackToRaise("UNGRAB", interactable.gameObject.name, default));
        }
    }
}