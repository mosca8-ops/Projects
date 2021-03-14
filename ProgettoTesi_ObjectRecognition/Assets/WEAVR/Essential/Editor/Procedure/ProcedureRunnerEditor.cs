using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(ProcedureRunner))]
    public class ProcedureRunnerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                var runner = target as ProcedureRunner;
                var wasEnabled = GUI.enabled;
                GUI.enabled = runner.CurrentProcedure && !runner.RunningProcedure;
                if (GUILayout.Button("Start Procedure"))
                {
                    runner.StartCurrentProcedure();
                }
                GUI.enabled = wasEnabled;
            }
        }
    }
}
