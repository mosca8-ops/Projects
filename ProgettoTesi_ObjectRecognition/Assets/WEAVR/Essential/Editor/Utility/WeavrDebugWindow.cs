using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{

    public class WeavrDebugWindow : EditorWindow
    {
#if !WEAVR_DLL && WEAVR_INTERNAL_USE
        [MenuItem("WEAVR/Diagnostics/Debug")]
#endif
        private static void ShowWindow()
        {
            GetWindow<WeavrDebugWindow>().Show();
        }

        private void OnEnable()
        {
            WeavrDebug.Trace = EditorPrefs.GetBool("WeavrDebug:Trace");
        }

        private void OnGUI()
        {
            WeavrDebug.Trace = EditorGUILayout.Toggle("TRACE", WeavrDebug.Trace);
            EditorPrefs.SetBool("WeavrDebug:Trace", WeavrDebug.Trace);
        }
    }
}
