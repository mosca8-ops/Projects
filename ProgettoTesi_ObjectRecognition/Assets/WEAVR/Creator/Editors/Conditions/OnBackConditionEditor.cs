using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(OnBackCondition), true)]
    class OnBackConditionEditor : ConditionEditor
    {
        private GUIStyle m_labelStyle;
        public override bool ShouldDrawNotToggle => false;

        protected override void DrawProperties(Rect rect, SerializedProperty property)
        {
            if(m_labelStyle == null)
            {
                m_labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
            }
            GUI.Label(rect, "On Previous Clicked", m_labelStyle);
        }

        protected override float GetHeightInternal()
        {
            s_styles.Refresh();
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + HeaderHeight;
        }
    }
}