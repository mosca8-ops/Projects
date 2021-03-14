using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Editor
{
    [CustomEditor(typeof(ColorPalette))]
    public class ColorPaletteEditor : UnityEditor.Editor
    {
        private ColorPalette m_palette;

        private Vector2 m_scrollPosition;
        private bool m_colorsExpand;
        private bool m_groupsExpand;
        private Dictionary<ColorGroup, bool> m_expandedStates;

        private void OnEnable()
        {
            m_palette = target as ColorPalette;
            if (m_palette)
            {
                m_palette.Refresh();
                m_palette.Refreshed -= UpdateExpandedStates;
                m_palette.Refreshed += UpdateExpandedStates;
                UpdateExpandedStates();
            }
        }

        private void UpdateExpandedStates()
        {
            var newStates = new Dictionary<ColorGroup, bool>();
            foreach(var group in m_palette.Groups)
            {
                newStates[group] = false;
            }
            if (m_expandedStates != null)
            {
                foreach (var pair in m_expandedStates)
                {
                    newStates[pair.Key] = pair.Value;
                }
            }
            m_expandedStates = newStates;
            EditorUtility.SetDirty(m_palette);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_title"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_autoGenerate"));
            serializedObject.ApplyModifiedProperties();
            
            bool requireRefresh = false;
            var lastColor = GUI.color;
            GUILayout.Space(10);
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            m_colorsExpand = EditorGUILayout.Foldout(m_colorsExpand, "Colors");

            ColorHolder holderToEliminate = null;
            ColorGroup groupToEliminate = null;

            if (m_colorsExpand)
            {

                foreach (var colorHolder in m_palette.ColorHolders)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    bool cannotModify = m_palette.GetColorHolder(colorHolder.name) != colorHolder;
                    if (cannotModify) {
                        GUI.color = Color.red;
                    }
                    string newName = EditorGUILayout.TextField(colorHolder.name, GUILayout.ExpandWidth(true));
                    var color = EditorGUILayout.ColorField(colorHolder.color, GUILayout.Width(100));
                    if(color != colorHolder.color)
                    {
                        colorHolder.color = color;
                        EditorUtility.SetDirty(m_palette);
                    }
                    if(GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        holderToEliminate = colorHolder;
                    }
                    EditorGUILayout.EndHorizontal();
                    if(newName != colorHolder.name && !m_palette.ContainsName(newName))
                    {
                        colorHolder.name = newName;
                        requireRefresh = true;
                    }
                    else if (cannotModify)
                    {
                        EditorGUILayout.HelpBox($"Color with name {newName} already exists", MessageType.Error);
                    }

                    GUI.color = lastColor;
                }

            }
            if (GUILayout.Button("Add Color"))
            {
                m_palette.AddColor($"Color {m_palette.ColorHolders.Count + 1}", Color.black);
                requireRefresh = false;
            }
            GUILayout.Space(10);

            m_groupsExpand = EditorGUILayout.Foldout(m_groupsExpand, "Color Groups");
            if (m_groupsExpand)
            {
                foreach(var group in m_palette.Groups)
                {
                    if (DrawGroup(group, ref groupToEliminate))
                    {
                        requireRefresh = true;
                    }
                }
            }

            if (GUILayout.Button("Add Group"))
            {
                m_palette.AddColorGroup($"Group {m_palette.Groups.Count + 1}");
                requireRefresh = false;
            }

            EditorGUILayout.EndScrollView();
            
            if(holderToEliminate != null)
            {
                m_palette.RemoveColor(holderToEliminate);
            }
            else if(groupToEliminate != null)
            {
                m_palette.RemoveColorGroup(groupToEliminate);
            }
            else if (requireRefresh)
            {
                m_palette.Refresh();
            }
        }

        private bool DrawGroup(ColorGroup group, ref ColorGroup eliminate)
        {
            bool returnValue = false;
            var lastGuiColor = GUI.color;
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            m_expandedStates[group] = EditorGUILayout.Foldout(m_expandedStates[group], "Group ");
            bool cannotModify = m_palette.GetGroup(group.Name) != group;
            if (cannotModify)
            {
                GUI.color = Color.red;
            }
            EditorGUI.BeginDisabledGroup(group.Readonly);
            var newName = EditorGUILayout.TextField(group.Name);
            if (newName != group.Name && !m_palette.ContainsGroupName(newName))
            {
                group.Name = newName;
                returnValue = true;
            }
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                eliminate = group;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            GUI.color = lastGuiColor;

            if (m_expandedStates[group])
            {
                ColorHolder holderToEliminate = null;
                bool requireRefresh = false;
                foreach (var colorHolder in group.ColorHolders)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(40);
                    //cannotModify = group.GetColorHolder(colorHolder.name) != colorHolder;
                    //if (cannotModify)
                    //{
                    //    GUI.color = Color.red;
                    //}
                    EditorGUI.BeginDisabledGroup(group.Readonly);
                    newName = EditorGUILayout.TextField(colorHolder.name, GUILayout.ExpandWidth(true));
                    EditorGUI.EndDisabledGroup();
                    var color = EditorGUILayout.ColorField(colorHolder.color, GUILayout.Width(100));
                    if(color != colorHolder.color)
                    {
                        colorHolder.color = color;
                        EditorUtility.SetDirty(m_palette);
                    }
                    if (!group.Readonly && GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        holderToEliminate = colorHolder;
                    }
                    EditorGUILayout.EndHorizontal();
                    if (newName != colorHolder.name && !group.ContainsName(newName))
                    {
                        colorHolder.name = newName;
                        requireRefresh = true;
                    }
                    GUI.color = lastGuiColor;
                }

                if(!group.Readonly && GUILayout.Button("Add Color"))
                {
                    group.AddColor();
                    returnValue = true;
                }
                else if(holderToEliminate != null)
                {
                    group.Remove(holderToEliminate);
                    returnValue = true;
                }
                else if (requireRefresh)
                {
                    group.Refresh();
                    returnValue = true;
                }
            }

            EditorGUILayout.EndVertical();

            return returnValue;
        }
    }
}
