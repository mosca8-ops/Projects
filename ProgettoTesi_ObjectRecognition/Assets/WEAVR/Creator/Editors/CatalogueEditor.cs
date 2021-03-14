using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine.UIElements;
using System;
using TXT.WEAVR.Editor;
using System.Linq;

namespace TXT.WEAVR.Procedure
{
    public abstract class CatalogueEditor<TCatalogue, TDescriptor, T> : UnityEditor.Editor where TCatalogue : BaseCatalogue<TDescriptor>  where TDescriptor : Descriptor
    {
        private const int k_MaxDepthLevel = 3;
        private List<Type> m_descriptorTypes;

        private List<DescriptorGroup> m_currentPreviewPath;

        private Dictionary<Descriptor, SerializedObject> m_serObjs = new Dictionary<Descriptor, SerializedObject>();

        private TCatalogue Catalogue => target as TCatalogue;

        protected Action m_preRenderAction;

        private List<Vector2> m_scrollPositions = new List<Vector2>();
        private bool m_showAll;

        protected class Styles : BaseStyles
        {
            public GUIStyle path;
            public GUIStyle placeholder;
            public GUIStyle descriptorType;
            public GUIStyle groupName;

            protected override void InitializeStyles(bool isProSkin)
            {
                path = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                path.alignment = TextAnchor.LowerLeft;

                placeholder = new GUIStyle("Label");
                var color = placeholder.normal.textColor;
                color.a *= 0.5f;
                placeholder.normal.textColor = color;

                descriptorType = new GUIStyle("Label");
                descriptorType.fontStyle = FontStyle.Bold;

                groupName = new GUIStyle(descriptorType);
                color = placeholder.normal.textColor;
                color.b *= 0.75f;
                color.r *= 0.75f;
                groupName.normal.textColor = color;
            }
        }

        protected static Styles s_styles = new Styles();

        private void OnEnable()
        {
            if (m_descriptorTypes == null)
            {
                m_descriptorTypes = GetDescriptorTypes();
            }
            if (m_currentPreviewPath == null)
            {
                m_currentPreviewPath = new List<DescriptorGroup>();
            }
            else
            {
                m_currentPreviewPath.Clear();
            }
            if(m_scrollPositions == null)
            {
                m_scrollPositions = new List<Vector2>();
            }
            if(m_scrollPositions.Count == 0)
            {
                m_scrollPositions.Add(Vector2.zero);
            }
            foreach(var catalogueObj in Catalogue?.Root.Children)
            {
                if(catalogueObj == null)
                {
                    Catalogue.Root.Clear();
                    break;
                }
            }
            m_currentPreviewPath.Add(Catalogue?.Root);
            
        }

        protected virtual List<Type> GetDescriptorTypes()
        {
            return typeof(T).GetAllSubclassesOf().Where(t => !t.IsAbstract).ToList();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawMainPanel();
            DrawMenuPanel();
            EditorGUILayout.EndVertical();

            //serializedObject.Update();
            //serializedObject.ApplyModifiedProperties();
        }

        public void DrawMenuPanel()
        {
            DrawMenuPreview();
        }

        public void DrawMainPanel()
        {
            if (m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            if(m_scrollPositions == null || m_scrollPositions.Count == 0)
            {
                OnEnable();
            }
            //DrawGroup(Catalogue.Root);

            //serializedObject.Update();
            s_styles.Refresh();

            var currentGroup = m_currentPreviewPath.Last();

            m_showAll = EditorGUILayout.Toggle("Show all", m_showAll);
            m_scrollPositions[m_currentPreviewPath.Count - 1] = EditorGUILayout.BeginScrollView(m_scrollPositions[m_currentPreviewPath.Count - 1]);
            //EditorGUI.indentLevel++;
            foreach (var descriptor in currentGroup.Children)
            {
                if (descriptor is DescriptorGroup)
                {
                    DrawGroup(currentGroup, descriptor as DescriptorGroup, 1, m_showAll);
                }
                else if (descriptor is TDescriptor t)
                {
                    DrawDescriptor(currentGroup, t);
                }
            }
            //EditorGUI.indentLevel--;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Root", s_styles.groupName);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Group"))
            {
                currentGroup.Add(CreateInstance<DescriptorGroup>());
                RefreshCatalogue();
            }
            if (GUILayout.Button(AddDescriptorLabel()))
            {
                CreateDescriptorsMenu(currentGroup).ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMenuPreview()
        {
            var lastGroup = m_currentPreviewPath.Last();
            GUILayout.Label("PREVIEW", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(40);
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal("Box");
            if (lastGroup != m_currentPreviewPath.First())
            {
                if (GUILayout.Button("<", GUILayout.Width(20)))
                {
                    m_preRenderAction = () => m_currentPreviewPath.RemoveAt(m_currentPreviewPath.Count - 1);
                }
                GUILayout.Space(4);
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(20));
                EditorGUI.DrawRect(rect, lastGroup.Color);
                GUILayout.FlexibleSpace();
                GUILayout.Label(lastGroup.Name);
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(Catalogue.name);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical("Box");
            if (lastGroup.Children.Count > 0)
            {
                foreach (var descriptor in lastGroup.Children)
                {
                    if(descriptor == null)
                    { continue; }
                    EditorGUILayout.BeginHorizontal("Box");
                    var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(16));
                    if (descriptor.Icon != null)
                    {
                        GUI.DrawTexture(rect, descriptor.Icon);
                    }
                    else
                    {
                        EditorGUI.DrawRect(rect, descriptor.Color);
                    }
                    GUILayout.FlexibleSpace();
                    if (descriptor is DescriptorGroup)
                    {
                        GUILayout.Label(descriptor.Name, EditorStyles.boldLabel);
                    }
                    else
                    {
                        GUILayout.Label(descriptor.Name);
                    }
                    GUILayout.FlexibleSpace();
                    if (descriptor is DescriptorGroup && GUILayout.Button(">", GUILayout.Width(20)))
                    {
                        m_preRenderAction = () =>
                        {
                            m_currentPreviewPath.Add(descriptor as DescriptorGroup);
                            if(m_scrollPositions.Count < m_currentPreviewPath.Count)
                            {
                                m_scrollPositions.Add(Vector2.zero);
                            }
                        };
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                GUILayout.Label("Nothing in this group", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            GUILayout.Space(40);
            EditorGUILayout.EndHorizontal();
        }

        private GenericMenu CreateDescriptorsMenu(DescriptorGroup currentGroup)
        {
            GenericMenu menu = new GenericMenu();
            foreach(var type in Catalogue.Descriptors.Keys)
            {
                menu.AddItem(new GUIContent($"Already Added/{type.Name}"), false, () => AddDescriptor(currentGroup, type));
            }
            foreach(var type in m_descriptorTypes.Except(Catalogue.Descriptors.Keys))
            {
                menu.AddItem(new GUIContent(type.Name), false, () => AddDescriptor(currentGroup, type));
            }
            return menu;
        }

        private void AddDescriptor(DescriptorGroup currentGroup, Type type)
        {
            currentGroup.Add(InstantiateDescriptor(type));
            currentGroup.UpdateFullPaths();

            RefreshCatalogue();
        }

        protected abstract TDescriptor InstantiateDescriptor(Type type);
        //{
        //    return ActionDescriptor.CreateDescriptor(type);
        //}

        protected void RefreshCatalogue()
        {
            m_preRenderAction = () =>
            {
                Catalogue.FullSave();
                Catalogue.Refresh();
                m_serObjs.Clear();
            };
        }

        private void OnDisable()
        {
            if (Catalogue)
            {
                Catalogue.FullSave();
            }
        }

        private void DrawGroup(DescriptorGroup parent, DescriptorGroup group, int level, bool showInner)
        {
            EditorGUILayout.BeginVertical("Box");

            EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            GUILayout.Label("Group", s_styles.descriptorType);
            EditorGUILayout.Space();
            GUILayout.Label(group.FullPath, s_styles.path);
            GUILayout.FlexibleSpace();
            var color = GUI.color;
            DrawUpDownArrows(parent, group);
            GUI.color = Color.red;
            if (GUILayout.Button("X"))
            {
                m_preRenderAction = () =>
                {
                    parent.Remove(group);
                    RefreshCatalogue();
                };
            }
            GUI.color = color;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            var serObj = Get(group);
            serObj.Update();
            serObj.FindProperty("m_name").stringValue = group.Name = EditorGUILayout.TextField("Name", group.Name);
            EditorGUILayout.BeginHorizontal();
            serObj.FindProperty("m_color").colorValue = group.Color = EditorGUILayout.ColorField("Color", group.Color);
            serObj.FindProperty("m_fullPath").stringValue = group.FullPath;
            serObj.ApplyModifiedProperties();
            if (GUILayout.Button("Apply All"))
            {
                group.PropagateColor();
            }
            EditorGUILayout.EndHorizontal();
            //group.RelativePath = EditorGUILayout.TextField("Path Name", group.RelativePath);
            //EditorGUILayout.LabelField("Full Path", group.FullPath);

            if (EditorGUI.EndChangeCheck())
            {
                group.UpdateFullPaths();
            }

            if (showInner)
            {
                //EditorGUI.indentLevel++;
                foreach (var descriptor in group.Children)
                {
                    if (descriptor is DescriptorGroup)
                    {
                        DrawGroup(group, descriptor as DescriptorGroup, level + 1, showInner);
                    }
                    else if (descriptor is TDescriptor t)
                    {
                        DrawDescriptor(group, t);
                    }
                }
            }
            else if (GUILayout.Button("Open Group"))
            {
                m_preRenderAction = () =>
                {
                    m_currentPreviewPath.Add(group);
                    if (m_scrollPositions.Count < m_currentPreviewPath.Count)
                    {
                        m_scrollPositions.Add(Vector2.zero);
                    }
                };
            }
            //EditorGUI.indentLevel--;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(group.Name, s_styles.groupName);
            GUILayout.FlexibleSpace();
            if (level < k_MaxDepthLevel && GUILayout.Button("Add Subgroup"))
            {
                group.Add(CreateInstance<DescriptorGroup>());
                RefreshCatalogue();
            }
            if (GUILayout.Button(AddDescriptorLabel()))
            {
                CreateDescriptorsMenu(group).ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.Space(4);
        }

        protected void DrawUpDownArrows(DescriptorGroup parent, Descriptor descriptor)
        {
            int index = parent.IndexOf(descriptor);
            using (new EditorGUI.DisabledGroupScope(index == 0))
            {
                if (GUILayout.Button(@"▲"))
                {
                    m_preRenderAction = () => parent.Insert(index - 1, descriptor);
                }
            }
            using (new EditorGUI.DisabledGroupScope(index >= parent.Children.Count - 1))
            {
                if (GUILayout.Button(@"▼"))
                {
                    m_preRenderAction = () => parent.Insert(index, descriptor);
                }
            }
        }

        protected abstract string AddDescriptorLabel();

        protected abstract void DrawDescriptor(DescriptorGroup parent, TDescriptor descriptor);

        protected SerializedObject Get(Descriptor descriptor)
        {
            if(!m_serObjs.TryGetValue(descriptor, out SerializedObject serObj))
            {
                serObj = new SerializedObject(descriptor);
                m_serObjs[descriptor] = serObj;
            }
            return serObj;
        }

        public override VisualElement CreateInspectorGUI()
        {
            return base.CreateInspectorGUI();
        }
    }
}
