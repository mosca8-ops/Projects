namespace TXT.WEAVR.Cockpit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(CockpitLED), true)]
    public class CockpitLEDEditor : CockpitElementEditor
    {
        protected override void DrawState(int index) {
            var state = _cockpitElement.EditorStates[index];
            EditorGUILayout.BeginVertical(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
            {
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField(state.state, EditorStyles.boldLabel, GUILayout.MaxWidth(60));
                    //if (_cockpitElement.EditorAnimator != null && _cockpitElement.EditorAnimator.runtimeAnimatorController != null) {
                    //    EditorGUIUtility.labelWidth = 100;
                    //    state.useAnimator = EditorGUILayout.Toggle("Animator State: ", state.useAnimator);
                    //}
                    EditorGUIUtility.labelWidth = 50;
                    state.Value = WeavrGUILayout.ValueField("Value:", state.Value, _cockpitElement.Binding.type);
                }
                EditorGUILayout.EndHorizontal();
                //DrawStepInternals(index, state);
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
            EditorGUILayout.EndVertical();
        }
    }
}
