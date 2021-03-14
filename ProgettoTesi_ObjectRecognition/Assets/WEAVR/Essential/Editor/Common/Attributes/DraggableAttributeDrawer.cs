using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using TXT.WEAVR.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Editor
{
    [CustomPropertyDrawer(typeof(DraggableAttribute))]
    public class DraggableAttributeDrawer : ComposablePropertyDrawer
    {
        static GUIContent s_dragContent = new GUIContent(string.Empty, "Drag this object generically onto other object fields.\nHold Control for specific object type (e.g. RigidBody only for RigidBody fields)");
        public class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle dragIcon;
            public float boxWidth;

            protected override void InitializeStyles(bool isProSkin)
            {
                dragIcon = WeavrStyles.ControlsSkin.FindStyle("draggable_icon") ?? new GUIStyle();
                box = WeavrStyles.ControlsSkin.FindStyle("draggable_box") ?? new GUIStyle("Box");
                boxWidth = box.fixedWidth > 0 ? box.fixedWidth : 16;
            }
        }

        private static Styles s_styles = new Styles();

        Dictionary<string, (bool potentialDrag, bool dragStarted)> m_propertiesState = new Dictionary<string, (bool potentialDrag, bool dragStarted)>();

        private UnityEditor.Editor m_activeEditor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                s_styles.Refresh();

                float boxWidth = s_styles.boxWidth - EditorGUI.indentLevel * 15f;

                position = EditorGUI.PrefixLabel(position, AddTooltip(label));
                var dragArea = new Rect(position.x, position.y + s_styles.box.margin.top, s_styles.boxWidth, position.height - s_styles.box.margin.vertical);

                position.x += boxWidth;
                position.width -= boxWidth;

                base.OnGUI(position, property, GUIContent.none);

                var e = Event.current;
                var isFocused = e.control || e.command;
                bool wasEnabled = GUI.enabled;
                GUI.enabled = property.objectReferenceValue;
                s_dragContent.image = isFocused ? s_styles.dragIcon.focused.background : s_styles.dragIcon.normal.background;
                GUI.Box(dragArea, s_dragContent, s_styles.box);

                if (property.objectReferenceValue && dragArea.Contains(e.mousePosition))
                {
                    if(!m_propertiesState.TryGetValue(property.propertyPath, out (bool potentialDrag, bool dragStarted) state))
                    {
                        state.potentialDrag = false;
                        state.dragStarted = false;
                    }
                    switch (e.type)
                    {
                        case EventType.MouseDown:
                            if (!state.dragStarted)
                            {
                                state.potentialDrag = true;
                                e.Use();
                            }
                            break;
                        case EventType.MouseDrag:
                            if (state.potentialDrag && !state.dragStarted)
                            {
                                DragAndDrop.PrepareStartDrag();
                                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                DragAndDrop.objectReferences = new UnityEngine.Object[] { isFocused ? property.objectReferenceValue : GetCompatibleObject(property.objectReferenceValue) };
                                DragAndDrop.StartDrag("Move object");
                                state.dragStarted = true;
                                e.Use();
                            }
                            break;
                        case EventType.MouseUp:
                            if (state.dragStarted)
                            {
                                state.dragStarted = false;
                                e.Use();
                            }
                            break;
                    }

                    m_propertiesState[property.propertyPath] = state;
                }
                else if(e.type != EventType.Layout)
                {
                    m_propertiesState[property.propertyPath] = (false, false);
                }

                GUI.enabled = wasEnabled;
            }
            else
            {
                base.OnGUI(position, property, label);
            }
        }

        private UnityEngine.Object GetCompatibleObject(UnityEngine.Object obj) => obj is Component c ? c.gameObject : obj is GameObject go ? go : obj;

        private void RepaintEditor(SerializedObject obj)
        {
            if (!m_activeEditor)
            {
                foreach(var editor in ActiveEditorTracker.sharedTracker.activeEditors)
                {
                    if(editor.serializedObject == obj)
                    {
                        m_activeEditor = editor;
                        break;
                    }
                }
            }
            if (m_activeEditor)
            {
                m_activeEditor.Repaint();
            }
        }
    }
}
