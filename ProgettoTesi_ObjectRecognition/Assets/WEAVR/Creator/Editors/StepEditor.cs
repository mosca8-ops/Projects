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
    [CustomEditor(typeof(BaseStep), true)]
    class StepEditor : GraphObjectEditor
    {
        private Vector2 m_scrollPosition;
        protected BaseStep Step => target as BaseStep;

        private class Styles : BaseStyles
        {
            

            protected override void InitializeStyles(bool isProSkin)
            {
                
            }
        }

        private static Styles s_styles = new Styles();
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (Step)
            {
                Step.OnModified -= Step_OnModified;
                Step.OnModified += Step_OnModified;
            }
        }

        private void Step_OnModified(ProcedureObject obj)
        {
            //QueryActionsWithErrors();
        }

        public override void OnInspectorGUI()
        {
            bool wasEnabled = GUI.enabled;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 90;
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_description"));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Nodes", s_baseStyles.sectionLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            
            for (int i = 0; i < Step.Nodes.Count; i++)
            {
                var node = Step.Nodes[i];
                if (node)
                {
                    if(GUILayout.Button(node.Title, s_baseStyles.button, GUILayout.ExpandWidth(true)))
                    {
                        ProcedureEditor.Instance.Select(node, true);
                        //ProcedureObjectInspector.Selected = node;
                    }
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
                Step.Modified();
            }
        }

        protected override void DrawHeaderLayout()
        {
            EditorGUIUtility.labelWidth = 60;
            bool wasEnabled = GUI.enabled;

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_title"), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_number"), GUILayout.ExpandWidth(true));
            //var property = serializedObject.FindProperty("m_isMandatory");
            //property.boolValue = GUILayout.Toggle(property.boolValue, "Mandatory", s_baseStyles.textToggle);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
