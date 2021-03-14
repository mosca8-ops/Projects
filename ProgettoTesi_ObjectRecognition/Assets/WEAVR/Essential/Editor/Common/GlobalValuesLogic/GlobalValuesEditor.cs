using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{
    [CustomEditor(typeof(GlobalValues))]
    public class GlobalValuesEditor : UnityEditor.Editor
    {
        private GlobalValues m_component;

        private Vector2 m_scrollPosition;

        private void OnEnable()
        {
            m_component = target as GlobalValues;
        }
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                DrawAllVariables();
            }
        }

        private void DrawAllVariables()
        {
            EditorGUILayout.BeginVertical("Box");
            foreach(var variable in m_component.AllVariables)
            {
                DrawVariable(variable);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawVariable(ValuesStorage.Variable variable)
        {
            EditorGUILayout.BeginHorizontal("Box");
            GUILayout.Label(variable.Name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(variable.Type.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(70));
            GUILayout.BeginVertical("Box");
            GUILayout.Label(variable.Value.ToString());
            GUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
    }
}
