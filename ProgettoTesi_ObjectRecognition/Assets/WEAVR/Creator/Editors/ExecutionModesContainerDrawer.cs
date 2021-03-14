using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomPropertyDrawer(typeof(ExecutionModesContainer))]
    public class ExecutionModesContainerDrawer : ComposablePropertyDrawer
    {

        private class Styles : BaseStyles
        {
            public GUIStyle textToggle;

            public float fullLineHeight;

            protected override void InitializeStyles(bool isProSkin)
            {
                textToggle = WeavrStyles.EditorSkin2.FindStyle("actionEditor_TextToggle") ?? WeavrStyles.MiniToggleTextOn;
            }
        }

        private static Styles s_styles = new Styles();
        private static GUIContent s_noModesContent = new GUIContent("No modes available");

        private bool m_initialized;
        private Procedure m_procedure;
        private GUIContent m_modeContent = new GUIContent();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!m_initialized)
            {
                s_styles.Refresh();
                m_procedure = property.FindPropertyRelative("m_procedure").objectReferenceValue as Procedure;
            }
            else
            {
                var procedureProperty = property.FindPropertyRelative("m_procedure");
                if (procedureProperty.objectReferenceValue != m_procedure)
                {
                    s_styles.Refresh();
                    m_procedure = procedureProperty.objectReferenceValue as Procedure;
                }
            }

            var modes = m_procedure ? m_procedure.ExecutionModes : ProcedureDefaults.Current?.ExecutionModes;
            if(modes != null && modes.Count > 0)
            {
                float width = position.width;
                position.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(position, label);

                position.x += EditorGUIUtility.labelWidth;
                position.width = width - EditorGUIUtility.labelWidth;

                var list = property.FindPropertyRelative("m_modes");

                foreach(var mode in modes)
                {
                    if (!mode) { continue; }
                    m_modeContent.text = mode.ModeShortName;
                    m_modeContent.tooltip = mode.ModeName;
                    position.width = s_styles.textToggle.CalcSize(m_modeContent).x + 1;

                    bool containsMode = false;
                    int index = -1;

                    for (int i = 0; i < list.arraySize; i++)
                    {
                        index = i;
                        if(list.GetArrayElementAtIndex(i).objectReferenceValue == mode)
                        {
                            containsMode = true;
                            continue;
                        }
                    }
                    if (containsMode != GUI.Toggle(position, containsMode, m_modeContent, s_styles.textToggle))
                    {
                        if (containsMode)
                        {
                            // Remove it from exec modes
                            list.DeleteArrayElementAtIndex(index);
                        }
                        else
                        {
                            list.InsertArrayElementAtIndex(list.arraySize);
                            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = mode;
                        }
                    }

                    position.x += position.width + 3;
                }
            }
            else
            {
                EditorGUI.LabelField(position, label, s_noModesContent, s_styles.textToggle);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
