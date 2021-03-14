using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Procedure;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.EditorBridge
{

    public static class CommonComponentsInjectionResolver
    {
        private static HashSet<Action<Scene, LoadSceneMode>> s_sceneOpeningCallbacks = new HashSet<Action<Scene, LoadSceneMode>>();

        [DidReloadScripts]
        [InitializeOnLoadMethod]
        static void Resolve()
        {
            AnimatorParameterSetter.s_getParameters = a => (a.runtimeAnimatorController as AnimatorController).parameters;

            ProcedureLauncher.s_LoadSceneAsync = s => EditorSceneManager.OpenScene(s);
            ProcedureLauncher.s_RegisterSceneOpenEventHandler = c => s_sceneOpeningCallbacks.Add(c);
            ProcedureLauncher.s_UnregisterSceneOpenEventHandler = c => s_sceneOpeningCallbacks.Remove(c);

            EditorSceneManager.sceneOpened -= EditorSceneManager_SceneOpened;
            EditorSceneManager.sceneOpened += EditorSceneManager_SceneOpened;
        }

        private static void EditorSceneManager_SceneOpened(Scene scene, OpenSceneMode mode)
        {
            foreach(var c in s_sceneOpeningCallbacks)
            {
                c?.Invoke(scene, mode == OpenSceneMode.Single ? LoadSceneMode.Single : LoadSceneMode.Additive);
            }
        }
    }
}
