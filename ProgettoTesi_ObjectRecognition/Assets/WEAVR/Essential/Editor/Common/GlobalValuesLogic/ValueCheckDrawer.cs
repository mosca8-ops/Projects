    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(GlobalValueComponent.ValueCheck), true)]
    public class ValueCheckDrawer : ComposablePropertyDrawer
    {
        private class Styles : BaseStyles
        {
            public GUIStyle box;

            protected override void InitializeStyles(bool isProSkin)
            {
                box = new GUIStyle("Box");
            }
        }

        private Styles m_styles = new Styles();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(Event.current.type == EventType.Repaint)
            {
                m_styles.Refresh();
                m_styles.box.Draw(position, false, false, false, false);
            }

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            position.x += 10;
            position.y += 2;
            position.width -= 10;
            position.height -= 4;

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width - 2, EditorGUIUtility.singleLineHeight), property.FindPropertyRelative("value"));
            EditorGUIUtility.labelWidth = labelWidth;
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            position.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_onValueEquals"));

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("m_onValueEquals")) + 4;
        }
    }
}