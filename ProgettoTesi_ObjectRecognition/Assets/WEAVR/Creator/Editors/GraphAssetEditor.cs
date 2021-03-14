using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(BaseGraph))]
    class GraphAssetEditor : GraphObjectEditor
    {
        private Vector2 m_scrollPosition;
        protected BaseGraph Graph => target as BaseGraph;

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
            if (Graph)
            {
                Graph.OnModified -= Graph_OnModified;
                Graph.OnModified += Graph_OnModified;
            }
        }

        private void Graph_OnModified(ProcedureObject obj)
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

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Sanitize", s_baseStyles.button, GUILayout.MinWidth(120), GUILayout.MinHeight(30)))
            {
                Graph.Sanitize();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Nodes", s_baseStyles.sectionLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            for (int i = 0; i < Graph.Nodes.Count; i++)
            {
                var node = Graph.Nodes[i];
                if (node)
                {
                    if (GUILayout.Button(ToString(node), s_baseStyles.button, GUILayout.ExpandWidth(true)))
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

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Transitions", s_baseStyles.sectionLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < Graph.Transitions.Count; i++)
            {
                var transition = Graph.Transitions[i];
                if (transition && transition.To && transition.From)
                {
                    EditorGUILayout.BeginHorizontal();
                    //GUILayout.FlexibleSpace();
                    if (GUILayout.Button(ToString(transition.From), s_baseStyles.button, GUILayout.ExpandWidth(true)))
                    {
                        ProcedureEditor.Instance.Select(transition.From, true);
                        //ProcedureObjectInspector.Selected = node;
                    }
                    if (GUILayout.Button(GUIContent.none, s_styles.arrowButton, GUILayout.Width(60)))
                    {
                        ProcedureEditor.Instance.Select(transition, true);
                        //ProcedureObjectInspector.Selected = node;
                    }
                    if (GUILayout.Button(ToString(transition.To), s_baseStyles.button, GUILayout.ExpandWidth(true)))
                    {
                        ProcedureEditor.Instance.Select(transition.To, true);
                        //ProcedureObjectInspector.Selected = node;
                    }
                    //GUILayout.FlexibleSpace();
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

            GUILayout.FlexibleSpace();

            EditorGUIUtility.labelWidth = labelWidth;

            if (serializedObject.ApplyModifiedProperties())
            {
                Graph.Modified();
            }
        }

        private string ToString(GraphObject obj)
        {
            return obj is IProcedureStep step ? $"{step.Number}. {step.Title}" : obj is BaseNode node ? $"{node.Step?.Number}. {node.Title}" : obj.Title;
        }

        protected override void DrawHeaderLayout()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("GRAPH ELEMENTS", s_baseStyles.sectionLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
