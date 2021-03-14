using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.RemoteControl
{

    [AddComponentMenu("WEAVR/Remote Control/Commands/Activate Object")]
    public class ActivateObjectCommands : BaseCommandUnit
    {
        [RemoteEvent]
        public event Action<string, bool> ObjectActivated;

        public void ObjectActivationChanged(GameObject go)
        {
            ObjectActivated?.Invoke(go.GetHierarchyPath(), go.activeInHierarchy);
        }

        [RemotelyCalled]
        public void ActivateObject(string objectPath, bool enable)
        {
            var go = Query.Find<GameObject>(QuerySearchType.Scene, objectPath).First();
            if (go)
            {
                go.SetActive(enable);
            }
        }
    }
}
