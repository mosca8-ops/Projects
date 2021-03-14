using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Localization
{
    [CustomEditor(typeof(Language))]
    public class LanguageEditor : UnityEditor.Editor
    {

        private GUIContent m_dropDownContent = new GUIContent("Select from available");

        private CultureInfo[] m_cultures;
        private CultureInfo[] Cultures
        {
            get
            {
                if(m_cultures == null)
                {
                    m_cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                }
                return m_cultures;
            }
        }

        private Language m_target;
        private Language Target
        {
            get
            {
                if(m_target == null)
                {
                    m_target = target as Language;
                }
                return m_target;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 44;
            var property = serializedObject.FindProperty("m_icon");
            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Height(50), GUILayout.Width(50));
            property.objectReferenceValue = EditorGUILayout.ObjectField(GUIContent.none, 
                                            property.objectReferenceValue, 
                                            typeof(Texture2D), false, GUILayout.Height(80), GUILayout.Width(80));
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            property.Next(false);
            if (EditorGUILayout.PropertyField(property))
            {
                Target.UpdateCultureInfo();
            }
            GUILayout.Label(Target.DisplayName ?? "No culture found", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if(EditorGUILayout.DropdownButton(m_dropDownContent, FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                var targetedCultures = Cultures.Where(c => c.Name.StartsWith(Target.Name, System.StringComparison.InvariantCultureIgnoreCase));
                if (targetedCultures.Count() > 0)
                {
                    menu.AddDisabledItem(new GUIContent($"Similar to '{Target.Name}'"));
                    foreach (var culture in targetedCultures)
                    {
                        menu.AddItem(new GUIContent($"{culture.Name} - {culture.EnglishName}"), culture.Name == Target.Name, () => SetName(culture.Name));
                    }
                    menu.AddSeparator("");
                    menu.AddDisabledItem(new GUIContent("All Others"));
                }
                foreach (var culture in Cultures.Where(c => !targetedCultures.Contains(c)).OrderBy(c => c.Name))
                {
                    menu.AddItem(new GUIContent($"{culture.Name} - {culture.EnglishName}"), culture.Name == Target.Name, () => SetName(culture.Name));
                }
                menu.DropDown(GUILayoutUtility.GetLastRect());
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = labelWidth;
            if (serializedObject.ApplyModifiedProperties())
            {
                Target.UpdateCultureInfo();
            }
        }

        private void SetName(string name)
        {
            serializedObject.Update();
            serializedObject.FindProperty("m_name").stringValue = name;
            if (serializedObject.ApplyModifiedProperties())
            {
                Target.UpdateCultureInfo();
            }
        }
    }
}