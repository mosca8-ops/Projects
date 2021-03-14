using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Operable Events")]
    public class OperableEventsXApi : BaseInteractableXApi<AbstractOperable>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractOperable interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.onValidRangeEnter.AddListener(() => callbackToRaise("ENTER", interactable.gameObject.name, default));
            interactable.onValidRangeExit.AddListener(() => callbackToRaise("LEAVE", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractOperable interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.onValidRangeEnter.RemoveListener(() => callbackToRaise("ENTER", interactable.gameObject.name, default));
            interactable.onValidRangeExit.RemoveListener(() => callbackToRaise("LEAVE", interactable.gameObject.name, default));
        }
    }
}