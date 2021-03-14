using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.LowLevel;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace TXT.WEAVR
{

    public class GuidManagerWindow : EditorWindow
    {
#if !WEAVR_DLL
        [MenuItem("WEAVR/Diagnostics/Guid Manager", priority = 10)]
#endif
        static void ShowWindow()
        {
            GetWindow<GuidManagerWindow>("Guid Manager");
        }

        private Vector2 m_scrollPosition;
        private Dictionary<Guid, object> m_dictionary = new Dictionary<Guid, object>();

        [NonSerialized]
        private GUIStyle m_boxStyle;
        [NonSerialized]
        private GUIStyle m_highlightStyle;
        [NonSerialized]
        private Color[] m_colors;
        [NonSerialized]
        private GUIContent m_tempContent = new GUIContent();

        private Color m_highlightColor = new Color(0.9f, 0.9f, 0.1f, 0.01f);
        [NonSerialized]
        private bool m_highlightInScene;
        private bool HighlightInScene {
            get => m_highlightInScene;
            set {
                if (m_highlightInScene != value)
                {
                    m_highlightInScene = value;
                    if (m_highlightInScene)
                    {
                        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyDraw;
                        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyDraw;
                    }
                    else
                    {
                        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyDraw;
                    }
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
        }

        [NonSerialized]
        private GUIStyle m_searchStyle;
        private string m_searchValue;
        private Dictionary<Guid, GameObject> m_objectsToShow;
        private bool m_isSearching;

        private void OnHierarchyDraw(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID);
            var foundPair = GuidManager.GameObjects.FirstOrDefault(k => k.Value == go);
            if (foundPair.Value)
            {
                //EditorGUI.DrawRect(selectionRect, m_highlightColor);
                selectionRect.width *= 0.5f;
                selectionRect.x += selectionRect.width;
                m_tempContent.text = foundPair.Key.ToString().Substring(0, 10) + "...";
                m_tempContent.tooltip = foundPair.Key.ToString();
                GUI.Label(selectionRect, m_tempContent, m_highlightStyle);
            }
        }

        private void OnGUI()
        {
            InitializeIfNeeded();

            EditorGUILayout.BeginHorizontal("GroupBox");
            GUILayout.Label("GUID MANAGER", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            var tempSearchValue = EditorGUILayout.TextField(m_searchValue, m_searchStyle);
            if (m_searchValue != tempSearchValue)
            {
                m_searchValue = tempSearchValue;
                if (string.IsNullOrEmpty(m_searchValue))
                {
                    m_objectsToShow = (Dictionary<Guid, GameObject>)GuidManager.GameObjects;
                    m_isSearching = false;
                }
                else
                {
                    m_objectsToShow = Search(m_searchValue);
                    m_isSearching = true;
                }
            }
            else
            {
                if (!m_isSearching)
                    m_objectsToShow = (Dictionary<Guid, GameObject>)GuidManager.GameObjects;
            }

            HighlightInScene = GUILayout.Toggle(HighlightInScene, "Highlight in Scenes", "Button");
            if (GUILayout.Button("Refresh"))
            {
                UpdateFromScenes();
            }
            if (GUILayout.Button("Clear"))
            {
                GuidManager.ClearAll();
            }
            EditorGUILayout.EndHorizontal();

            DrawList(m_objectsToShow);
        }

        private void DrawList(Dictionary<Guid, GameObject> objects)
        {
            EditorGUILayout.BeginHorizontal("Box");
            GUILayout.Label("#", GUILayout.Width(25));
            GUILayout.Label("GUID", GUILayout.Width(260));
            GUILayout.Label("Object");
            GUILayout.FlexibleSpace();
            GUILayout.Label("Hash Code");
            EditorGUILayout.EndHorizontal();

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
            if (objects != null)
            {
                var bgColor = GUI.backgroundColor;
                var fgColor = GUI.contentColor;
                int elemIndex = 0;

                foreach (var pair in objects)
                {
                    elemIndex++;

                    GUI.backgroundColor = m_colors[(elemIndex % m_colors.Length)];
                    EditorGUILayout.BeginHorizontal(m_boxStyle);
                    GUI.backgroundColor = bgColor;

                    GUILayout.Label(elemIndex.ToString() + ".", GUILayout.Width(25));
                    if (pair.Value == null)
                    {
                        GUI.contentColor = Color.red;
                    }
                    GUILayout.Label(pair.Key.ToString(), GUILayout.Width(260));
                    GUI.contentColor = fgColor;

                    if (pair.Value is UnityEngine.Object obj)
                    {
                        EditorGUILayout.ObjectField(obj, obj.GetType(), true, GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        GUILayout.Label(pair.Value.ToString());
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(pair.Value?.GetHashCode().ToString());
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void UpdateFromScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    foreach (var guid in root.GetComponentsInChildren<GuidComponent>(true))
                    {
                        GuidManager.Register(guid.Guid, guid);
                    }
                }
            }
        }

        private void InitializeIfNeeded()
        {
            if (m_boxStyle == null)
            {
                m_boxStyle = new GUIStyle("Box");
                m_boxStyle.margin = new RectOffset(0, 0, 0, 0);
                m_colors = new Color[] {
                                     EditorGUIUtility.isProSkin ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.6f, 0.6f, 0.6f),
                                     EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(1.0f, 1.0f, 1.0f) };
                m_highlightStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    fontSize = 9,
                };
            }

            if (m_searchStyle == null)
            {
                m_searchStyle = new GUIStyle(GUI.skin.textField);
                m_searchStyle.stretchWidth = true;
            }
        }

        private Dictionary<Guid, GameObject> Search(string value)
        {
            var dictionary = new Dictionary<Guid, GameObject>();
            Guid guid;
            if (Guid.TryParse(value, out guid))
            {
                KeyValuePair<Guid, GameObject> result = GuidManager.GameObjects.FirstOrDefault(k => k.Key == guid);
                if (result.Value != null)
                    dictionary.Add(result.Key, result.Value);
            }
            else
            {
                var results = GuidManager.GameObjects.Where(k => k.Value.name.StartsWith(value, StringComparison.InvariantCultureIgnoreCase) ||
                                                            (k.Value.name.IndexOf(value, 0, StringComparison.CurrentCultureIgnoreCase) != -1))
                                                            .OrderBy(d => d.Value.ToString()).ToList();
                foreach (var result in results)
                {
                    if (!dictionary.ContainsKey(result.Key))
                        dictionary.Add(result.Key, result.Value);
                }
            }
            return dictionary;
        }
    }
}