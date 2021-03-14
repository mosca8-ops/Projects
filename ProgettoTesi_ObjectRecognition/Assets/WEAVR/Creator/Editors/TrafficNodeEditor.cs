using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(TrafficNode), true)]
    class TrafficNodeEditor : GraphObjectEditor
    {
        private Vector2 m_scrollPosition;
        protected TrafficNode Node => target as TrafficNode;

        private class Styles : BaseStyles
        {
            public GUIStyle arrowButton;

            protected override void InitializeStyles(bool isProSkin)
            {
                arrowButton = WeavrStyles.EditorSkin2.FindStyle("transition_ArrowButton") ?? EditorStyles.miniButton;
            }
        }

        private static Styles s_styles = new Styles();
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Node)
            {
                Node.OnModified -= Node_OnModified;
                Node.OnModified += Node_OnModified;
            }
        }

        private void Node_OnModified(ProcedureObject obj)
        {
            //QueryActionsWithErrors();
        }

        public override void OnInspectorGUI()
        {
            s_styles.Refresh();
            bool wasEnabled = GUI.enabled;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 90;
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_inputTransitions");
            GUILayout.Label($"Input Transitions: {property.intValue}");
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("m_endIncomingFlows"));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Transitions", s_baseStyles.sectionLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            
            for (int i = 0; i < Node.OutputTransitions.Count; i++)
            {
                var transition = Node.OutputTransitions[i];
                if (transition && transition.To)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(GUIContent.none, s_styles.arrowButton, GUILayout.Width(40)))
                    {
                        ProcedureEditor.Instance.Select(transition, true);
                        //ProcedureObjectInspector.Selected = node;
                    }
                    if (GUILayout.Button(transition.To.Title, s_baseStyles.button, GUILayout.ExpandWidth(true)))
                    {
                        ProcedureEditor.Instance.Select(transition.To, true);
                        //ProcedureObjectInspector.Selected = node;
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    GUI.enabled = false;
                    GUILayout.Button("Error", s_baseStyles.button, GUILayout.ExpandWidth(true));
                    GUI.enabled = wasEnabled;
                }
            }

            EditorGUILayout.EndScrollView();
            
            EditorGUIUtility.labelWidth = labelWidth;

            if (serializedObject.ApplyModifiedProperties())
            {
                Node.Modified();
            }
        }

        protected override void DrawHeaderLayout()
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;
            bool wasEnabled = GUI.enabled;

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_title"), GUILayout.ExpandWidth(true));
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("m_number"), GUILayout.ExpandWidth(true));
            //GUILayout.FlexibleSpace();
            var property = serializedObject.FindProperty("m_endIncomingFlows");
            property.boolValue = EditorGUILayout.Toggle("End Incoming Flows", property.boolValue /*, s_baseStyles.textToggle*/);
            //GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
