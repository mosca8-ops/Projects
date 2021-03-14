using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.RemoteControl
{

    [AddComponentMenu("WEAVR/Remote Control/Commands/Scene Load")]
    public class SceneLoadCommands : BaseCommandUnit
    {
        [RemotelyCalled]
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName);
        }

        [RemotelyCalled]
        public void LoadSceneAndNotify(string sceneName, Action<bool> callback)
        {
            SceneManager.LoadScene(sceneName);
            callback(true);
        }
    }
}
