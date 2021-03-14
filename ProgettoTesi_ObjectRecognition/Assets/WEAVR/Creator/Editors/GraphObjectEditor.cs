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
    [CustomEditor(typeof(GraphObject), true)]
    class GraphObjectEditor : UnityEditor.Editor
    {
        protected const float k_revealSpeedpx = 32f;
        private double m_lastTimeSinceStartup = 0;
        protected float deltaTime;
        protected float inverseDeltaTime;

        protected class GraphObjectStyles : BaseStyles
        {
            public GUIStyle button;
            public GUIStyle textToggle;
            public GUIStyle header;
            public GUIStyle headerIcon;
            public GUIStyle sectionLabel;
            public GUIContent targetIcon;

            protected override void InitializeStyles(bool isProSkin)
            {
                textToggle = WeavrStyles.EditorSkin2.FindStyle("graphObjectEditor_TextToggle") ?? EditorStyles.miniButton;
                header = WeavrStyles.EditorSkin2.FindStyle("graphObjectEditor_Header") ?? new GUIStyle("Box");
                headerIcon = WeavrStyles.EditorSkin2.FindStyle("graphObjectEditor_HeaderIcon") ?? new GUIStyle()
                {
                    fixedHeight = 36,
                    fixedWidth = 24
                };

                button = WeavrStyles.EditorSkin2.FindStyle("graphObjectEditor_Button") ?? new GUIStyle("Button");
                sectionLabel = WeavrStyles.EditorSkin2.FindStyle("graphObject_SectionLabel") ??
                                        new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            }
        }

        protected static GraphObjectStyles s_baseStyles = new GraphObjectStyles();

        protected virtual Texture GetIcon()
        {
            return WeavrStyles.Icons.Originals["procedure"];
        }

        protected override void OnHeaderGUI()
        {
            s_baseStyles.Refresh();
            if(s_baseStyles.targetIcon == null)
            {
                s_baseStyles.targetIcon = new GUIContent(GetIcon());
            }
            float labelWidth = EditorGUIUtility.labelWidth;
            serializedObject.Update();

            EditorGUILayout.BeginVertical(s_baseStyles.header, GUILayout.ExpandHeight(false));

            if (s_baseStyles.targetIcon.image != null)
            {
                EditorGUILayout.BeginHorizontal();
                float size = s_baseStyles.headerIcon.fixedHeight - s_baseStyles.headerIcon.border.vertical - s_baseStyles.headerIcon.margin.bottom;
                EditorGUILayout.BeginVertical(GUILayout.Width(size));
                var iconRect = GUILayoutUtility.GetRect(size, size);
                EditorGUIUtility.AddCursorRect(iconRect, MouseCursor.Zoom);
                var e = Event.current;
                if (e.type == EventType.Repaint)
                {
                    GUI.DrawTexture(iconRect, s_baseStyles.targetIcon.image);
                }
                else if(e.type == EventType.MouseUp && GUIUtility.GUIToScreenRect(iconRect).Contains(GUIUtility.GUIToScreenPoint(e.mousePosition)))
                {
                    Selection.activeObject = target;
                    e.Use();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false));
            }

            DrawHeaderLayout();

            if (s_baseStyles.targetIcon.image != null)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            //EditorGUILayout.Space();

            EditorGUIUtility.labelWidth = labelWidth;

            if (serializedObject.ApplyModifiedProperties())
            {
                (target as GraphObject).Modified();
            }
        }

        protected virtual void DrawHeaderLayout()
        {
            
        }

        protected virtual void OnEnable()
        {
            EditorApplication.update -= UpdateDeltaTime;
            EditorApplication.update += UpdateDeltaTime;
            m_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
        }

        private void UpdateDeltaTime()
        {
            deltaTime = Mathf.Min((float)(EditorApplication.timeSinceStartup - m_lastTimeSinceStartup), 0.2f);
            inverseDeltaTime = deltaTime == 0 ? 1f / deltaTime : 1f;
            m_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= UpdateDeltaTime;
        }

        public override void OnInspectorGUI()
        {
            //deltaTime = Mathf.Min((float)(EditorApplication.timeSinceStartup - m_lastTimeSinceStartup), 0.2f);
            //m_lastTimeSinceStartup = EditorApplication.timeSinceStartup;
        }
    }
}
