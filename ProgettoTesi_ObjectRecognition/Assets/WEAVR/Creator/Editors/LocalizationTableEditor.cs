using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Controls;
using TXT.WEAVR.Editor;
using TXT.WEAVR.Localization;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;

using ReorderableList = UnityEditorInternal.ReorderableList;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(LocalizationTable))]
    public class LocalizationTableEditor : UnityEditor.Editor
    {
        private class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle shortFoldout;
            public GUIStyle languageBox;

            protected override void InitializeStyles(bool isProSkin)
            {
                shortFoldout = new GUIStyle(EditorStyles.foldout);
                shortFoldout.fixedWidth = 1;
                languageBox = new GUIStyle("Box");
            }
        }

        private Styles m_styles = new Styles();
        public GUIContent m_removeContent = new GUIContent(@"✕", "Remove Language");

        private LocalizationTable m_table;

        private Action m_preRenderAction;

        private ReorderableList m_reorderableList;
        
        private ReorderableList LanguagesList
        {
            get
            {
                if (m_reorderableList == null)
                {
                    m_styles.Refresh();
                    m_reorderableList = new ReorderableList(serializedObject,
                                                            serializedObject.FindProperty("m_languages"),
                                                            false, false, false, false)
                    {
                        elementHeightCallback = List_ElementHeight,
                        drawElementCallback = List_DrawElement,
                        drawElementBackgroundCallback = List_DrawElementBackground,
                        drawHeaderCallback = List_DrawHeader,
                        drawFooterCallback = List_DrawFooter,
                    };
                }
                return m_reorderableList;
            }
        }

        private void List_DrawFooter(Rect rect)
        {
            rect.x += rect.width - 120;
            rect.width = 100;
            if(GUI.Button(rect, "Add Language", EditorStyles.miniButton))
            {
                GenericDropDownWindow.Show(new Rect(rect.x, rect.y, rect.width * 2, rect.height),
                                      AssetDatabase.FindAssets($"t:{typeof(Language).Name}")
                                                   .Select(guid => AssetDatabase.LoadAssetAtPath<Language>(AssetDatabase.GUIDToAssetPath(guid)))
                                                   .Where(l => l && !m_table.Languages.Contains(l))
                                                   .Distinct(),
                                      l => m_preRenderAction = () => m_table.Languages.Add(l),
                                      l => l.DisplayName,
                                      l => l.DisplayName,
                                      l => l.Icon,
                                      "Languages");
            }
        }

        private void List_DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Languages");
        }


        private void List_DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= m_table.Languages.Count) { return; }
            if (isActive && isFocused && Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, WeavrStyles.Colors.selection);
            }
        }


        private float List_ElementHeight(int index)
        {
            if(index < 0 || index >= m_table.Languages.Count) { return 0; }
            float line = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return m_table.Languages[index] ? 
                line * 2 + 4 : 
                line + 4;
        }

        private void List_DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= m_table.Languages.Count) { return; }

            rect.height -= EditorGUIUtility.standardVerticalSpacing;

            if (Event.current.type == EventType.Repaint)
            {
                m_styles.Refresh();
                m_styles.languageBox.Draw(rect, false, false, false, false);
            }

            rect.y += 2;
            rect.x += 2;
            rect.height -= 4;
            rect.width -= 4;

            var removeRect = new Rect(rect.x + rect.width - 24, rect.y, 24, 18);

            var language = m_table.Languages[index];
            if (language)
            {
                float iconWidth = rect.height - EditorGUIUtility.standardVerticalSpacing;
                if (language.Icon)
                {
                    GUI.DrawTexture(new Rect(rect.x, rect.y, iconWidth, iconWidth), language.Icon);
                }
                else
                {
                    var iconRect = new Rect(rect.x, rect.y, iconWidth, iconWidth);
                    EditorGUI.DrawRect(iconRect, Color.black);
                    GUI.Label(iconRect, "No Icon", EditorStyles.centeredGreyMiniLabel);
                }
                rect.width -= rect.height + 2;
                rect.x += rect.height + 2;

                rect.height = EditorGUIUtility.singleLineHeight;
                GUI.Label(rect, language.Name);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                GUI.Label(rect, language.DisplayName);
            }
            else
            {
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= removeRect.width + 2;
                var newLanguage = EditorGUI.ObjectField(rect, "Drag&Drop Language", language, typeof(Language), false);
                if (newLanguage)
                {
                    var property = serializedObject.FindProperty("m_languages").GetArrayElementAtIndex(index);
                    property.objectReferenceValue = newLanguage;
                }
            }

            var color = GUI.backgroundColor;
            GUI.backgroundColor = Color.red;
            if(GUI.Button(removeRect, m_removeContent, EditorStyles.miniButton))
            {
                m_preRenderAction = () => m_table.Languages.Remove(language);
            }
            GUI.backgroundColor = color;
        }

        private void OnEnable()
        {
            m_table = target as LocalizationTable;
        }

        public override void OnInspectorGUI()
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            serializedObject.Update();

            DrawLanguages();

            serializedObject.ApplyModifiedProperties();
        }

        public void DrawLanguages()
        {
            var property = serializedObject.FindProperty("m_defaultLanguage");
            EditorGUILayout.PropertyField(property, true);
            LanguagesList.DoLayoutList();
        }
    }
}
