using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(GlobalToggleComponentAction), true)]
    class GlobalToggleComponentEditor : ActionEditor
    {
        private static GUIContent s_componentLabel = new GUIContent("Components");

        private class Styles : BaseStyles
        {
            public GUIStyle hierarchyPreview;
            public GUIStyle previewToggle;

            protected override void InitializeStyles(bool isProSkin)
            {
                hierarchyPreview = WeavrStyles.EditorSkin2.FindStyle("actionEditor_HierarchyPreview");
                previewToggle = WeavrStyles.EditorSkin2.FindStyle("actionEditor_PreviewToggle") ?? "Button";
            }
        }

        private static Styles s_styles = new Styles();

        private GlobalToggleComponentAction m_action;

        private HashSet<int> m_objectsInScene;
        private GUIContent m_objectsInSceneContent;

        private string ComponentTypename
        {
            get => serializedObject.FindProperty("m_component").stringValue;
            set
            {
                serializedObject.Update();
                serializedObject.FindProperty("m_component").stringValue = value;
                serializedObject.ApplyModifiedProperties();
            }
        }

        private Type ComponentType
        {
            get => m_action?.ComponentType;
            set
            {
                if (m_action && m_action.ComponentType != value)
                {
                    bool eventsWereMuted = m_action.MuteEvents;
                    m_action.MuteEvents = true;
                    m_action.ComponentType = value;
                    ComponentTypename = value?.AssemblyQualifiedName;
                    m_action.MuteEvents = eventsWereMuted;

                    ComponentName.text = EditorTools.NicifyName(value?.Name ?? "No component selected");
                    UpdateObjectsInScene();
                }
            }
        }

        private GUIContent m_componentName;
        private GUIContent ComponentName
        {
            get
            {
                if(m_componentName == null)
                {
                    m_componentName = new GUIContent(EditorTools.NicifyName(ComponentType?.Name ?? "No component selected"));
                }
                return m_componentName;
            }
        }

        private bool m_showInHierarchy;
        private GameObject[] m_prevSelection;

        private bool ShowInHierarchy
        {
            get => m_showInHierarchy;
            set
            {
                if (m_showInHierarchy != value)
                {
                    m_showInHierarchy = value;
                    EditorApplication.hierarchyWindowItemOnGUI -= DrawAffectedItem;

                    if (m_showInHierarchy)
                    {
                        EditorApplication.hierarchyWindowItemOnGUI += DrawAffectedItem;
                    }
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            m_action = target as GlobalToggleComponentAction;
            if (m_action)
            {
                m_action.OnModified -= Action_OnModified;
                m_action.OnModified += Action_OnModified;
            }
        }

        private void Action_OnModified(ProcedureObject obj)
        {
            UpdateObjectsInScene();
        }

        protected override void OnDisable()
        {
            ShowInHierarchy = false;
            Selection.selectionChanged -= RestoreSelection;
            if (m_action)
            {
                m_action.OnModified -= Action_OnModified;
            }
            base.OnDisable();
        }

        protected override bool HasMiniPreview => ComponentType != null;

        protected override float MiniPreviewHeight => ComponentType != null ? EditorGUIUtility.singleLineHeight : 0;

        protected override void DrawProperties(Rect rect, SerializedProperty property)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            Rect buttonRect = EditorGUI.PrefixLabel(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), s_componentLabel);
            if (GUI.Button(buttonRect, ComponentName, EditorStyles.popup))
            {
                var comparer = new DistinctComponentType();
                var scene = m_action.Procedure?.GetScene() ?? SceneManager.GetActiveScene();
                GetComponentWindow.Show(buttonRect,
                        scene .GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Component>(true))
                        .Where(c => c).Distinct(comparer).OrderBy(c => c?.GetType().Name),
                        c => m_preRenderAction = () => ComponentType = c?.GetType(), scene.name);
            }
            EditorGUIUtility.labelWidth = labelWidth;
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            rect.height -= EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            using (new EditorGUI.DisabledGroupScope(ComponentType == null))
            {
                base.DrawProperties(rect, property);
            }
        }

        protected override float GetHeightInternal()
        {
            return base.GetHeightInternal() + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private void UpdateObjectsInScene()
        {
            if(m_objectsInScene == null) { m_objectsInScene = new HashSet<int>(); }
            else { m_objectsInScene.Clear(); }

            if(ComponentType != null)
            {
                var scene = m_action.Procedure?.GetScene() ?? SceneManager.GetActiveScene();
                m_objectsInScene = new HashSet<int>(scene.GetRootGameObjects()
                    .SelectMany(g => g.GetComponentsInChildren(ComponentType, m_action.IncludeInactive))
                    .Where(c => c && (c.gameObject.activeInHierarchy || m_action.IncludeInactive)).Select(c => c.gameObject.GetInstanceID()));
            }
            else if (m_objectsInScene == null) { m_objectsInScene = new HashSet<int>(); }
            else { m_objectsInScene.Clear(); }

            m_objectsInSceneContent = new GUIContent($"Affected objects: {m_objectsInScene.Count}");

            if (ShowInHierarchy)
            {
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        protected override void DrawMiniPreview(Rect r)
        {
            if(m_objectsInSceneContent == null)
            {
                UpdateObjectsInScene();
            }
            s_styles.Refresh();
            GUI.Label(r, m_objectsInSceneContent, s_baseStyles.miniPreviewLabel);
            ShowInHierarchy = GUI.Toggle(new Rect(r.x + r.width - 60, r.y, 60, r.height), ShowInHierarchy, "View", EditorStyles.miniButtonRight);
            using(new EditorGUI.DisabledGroupScope(!ShowInHierarchy))
            {
                if(GUI.Button(new Rect(r.x + r.width - 140, r.y, 80, r.height), "Select All", EditorStyles.miniButtonLeft))
                {
                    m_prevSelection = Selection.gameObjects;
                    //Selection.selectionChanged -= RestoreSelection;
                    //Selection.selectionChanged += RestoreSelection;
                    Selection.objects = m_objectsInScene.Select(id => EditorUtility.InstanceIDToObject(id)).ToArray();
                    //Selection.objects = currentSelection;
                }
            }
        }

        private void RestoreSelection()
        {
            Selection.selectionChanged -= RestoreSelection;
            Selection.objects = m_prevSelection;
        }

        private void DrawAffectedItem(int instanceID, Rect r)
        {
            if(Event.current.type != EventType.Repaint) { return; }
            if (m_objectsInScene.Contains(instanceID))
            {
                if (s_styles.hierarchyPreview != null)
                {
                    var r2 = new Rect(r.x + s_styles.hierarchyPreview.margin.left,
                                                            r.y + s_styles.hierarchyPreview.margin.top,
                                                            r.width - s_styles.hierarchyPreview.margin.horizontal,
                                                            r.height - s_styles.hierarchyPreview.margin.vertical);
                    EditorGUI.DrawRect(r2, s_styles.hierarchyPreview.normal.textColor);
                    s_styles.hierarchyPreview.Draw(r2, false, false, false, false);
                }
                else
                {
                    EditorGUI.DrawRect(r, WeavrStyles.Colors.transparentYellow);
                }
            }
        }


        private class DistinctComponentType : IEqualityComparer<Component>
        {
            public bool Equals(Component x, Component y)
            {
                return x?.GetType() == y?.GetType();
            }

            public int GetHashCode(Component obj)
            {
                return obj?.GetType().GetHashCode() ?? 0;
            }
        }
    }
}
