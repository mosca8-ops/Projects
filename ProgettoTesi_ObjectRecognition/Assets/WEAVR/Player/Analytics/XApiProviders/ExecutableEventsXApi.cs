using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Interaction;
using TXT.WEAVR.Maintenance;
using TXT.WEAVR.Procedure;
using UnityEngine;

namespace TXT.WEAVR.Player.Analytics
{

    [AddComponentMenu("WEAVR/Analytics/XAPI/Executable Events")]
    public class ExecutableEventsXApi : BaseInteractableXApi<AbstractExecutable>, IXApiProvider
    {
        protected override void RegisterEvents(AbstractExecutable interactable, Procedure.Procedure procedure, ExecutionMode mode, XAPIEventDelegate callbackToRaise)
        {
            interactable.onExecute.AddListener(() => callbackToRaise("EXECUTE", interactable.gameObject.name, default));
        }

        protected override void UnregisterEvents(AbstractExecutable interactable, XAPIEventDelegate callbackToRaise)
        {
            interactable.onExecute.RemoveListener(() => callbackToRaise("EXECUTE", interactable.gameObject.name, default));
        }
    }
}