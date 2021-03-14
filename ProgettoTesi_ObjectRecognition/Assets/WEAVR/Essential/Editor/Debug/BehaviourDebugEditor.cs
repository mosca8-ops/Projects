using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Utility;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Debugging
{

    [CustomEditor(typeof(BehaviourDebug))]
    public class BehaviourDebugEditor : UnityEditor.Editor
    {

        private UnityEditorInternal.ReorderableList m_reorderableList;
        private Color m_backgroundColor;

        private Object m_componentToRemove;

        private BehaviourDebug m_behaviour;

        private void OnEnable()
        {
            m_behaviour = target as BehaviourDebug;
            m_backgroundColor = EditorGUIUtility.isProSkin ? Color.clear : Color.grey;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_lines");

            if (m_reorderableList == null)
            {
                m_reorderableList = new UnityEditorInternal.ReorderableList(serializedObject, property, true, true, false, false)
                {
                    drawElementCallback = (rect, i, a, f) =>
                    {
                        rect.y += 1;
                        rect.height -= 2;
                        rect.width -= 24;

                        var debugLine = property.GetArrayElementAtIndex(i).GetObjectFromProperty<DebugLine>();
                        if (debugLine == null) { return; }
                        DrawLine(rect, debugLine);
                        //EditorGUI.LabelField(rect, debugLine.label.text);
                        //EditorGUI.PropertyField(rect, serObj.FindProperty("m_isActive"), GUIContent.none);
                        rect.x += rect.width + 4;
                        rect.width = 20;
                        if (GUI.Button(rect, @"–"))
                        {
                            m_behaviour.RemoveLine(debugLine);
                            //property.DeleteArrayElementAtIndex(i);
                            //DestroyImmediate(debugLine.gameObject);
                        }
                    },
                    elementHeight = 20,
                    drawHeaderCallback = rect =>
                    {
                        var r = rect;
                        r.width -= 40;
                        EditorGUI.LabelField(r, property.displayName);
                        r.x += r.width;
                        r.width = 40;
                        if (GUI.Button(r, "Edit", EditorStyles.toolbarButton)) {
                            r.x -= rect.width * 2;
                            r.width = rect.width;
                            r.position = GUIUtility.GUIToScreenPoint(r.position);
                            SelectMemberPopup.ShowAsPopup(r, m_behaviour.behaviour, m => !m_behaviour.Contains(m), m_behaviour.AddLine);
                        }
                    },
                    onAddDropdownCallback = (r, l) =>
                    {
                        r.position = GUIUtility.GUIToScreenPoint(r.position);
                        SelectMemberPopup.ShowAsPopup(r, m_behaviour.behaviour, m => !m_behaviour.Contains(m), m_behaviour.AddLine);
                    },
                    //headerHeight = 20,
                    onCanAddCallback = l => true,
                    onReorderCallbackWithDetails = (l, oldIndex, newIndex) => {
                        ApplyReordering(serializedObject.FindProperty("m_lines"), oldIndex, newIndex);
                    },
                    //drawFooterCallback = r => {
                    //    r.width -= 40;
                    //    //EditorGUI.LabelField(r, property.displayName);
                    //    r.x += r.width;
                    //    r.width = 40;
                    //    GUI.color = Color.cyan;
                    //    if (GUI.Button(r, "Edit", EditorStyles.toolbarButton)) {
                    //        r.position = GUIUtility.GUIToScreenPoint(r.position);
                    //        SelectMemberPopup.ShowAsPopup(r);
                    //    }
                    //}
                };
            }

            //bool wasEnabled = GUI.enabled;
            m_reorderableList.DoLayoutList();

            //EditorGUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            //if(GUILayout.Button("Edit Lines")) {
            //    SelectMemberPopup.ShowAsPopup(GUILayoutUtility.GetLastRect());
            //}
            //GUILayout.FlexibleSpace();
            //EditorGUILayout.EndHorizontal();

            //GUI.enabled = wasEnabled;
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLine(Rect rect, DebugLine line)
        {
            var lastColor = GUI.color;
            //EditorGUI.DrawRect(rect, m_backgroundColor);
            switch (line.Type)
            {
                case DebugLine.LineType.Field:
                    GUI.color = m_behaviour.colors.fieldColor;
                    EditorGUI.LabelField(rect, line.label.text, $"[FIELD] {line.MemberTypename}");
                    break;
                case DebugLine.LineType.Property:
                    GUI.color = m_behaviour.colors.propertyColor;
                    EditorGUI.LabelField(rect, line.label.text, $"[PROPERTY] {line.MemberTypename}");
                    break;
                case DebugLine.LineType.UnityEvent:
                case DebugLine.LineType.Event:
                    GUI.color = m_behaviour.colors.eventColor;
                    EditorGUI.LabelField(rect, line.label.text, $"[EVENT] {line.MemberTypename}");
                    break;
                case DebugLine.LineType.Method:
                    GUI.color = m_behaviour.colors.methodColor;
                    EditorGUI.LabelField(rect, line.label.text, $"[METHOD] {line.MemberTypename}");
                    break;
            }
            GUI.color = lastColor;
        }

        private void ApplyReordering(IEnumerable<Component> list, int oldIndex, int newIndex)
        {
            IList<Component> components = list is IList<Component> ? list as IList<Component> : new List<Component>(list);
            if (newIndex + 1 < components.Count)
            {
                components[newIndex].transform.SetSiblingIndex(components[newIndex + 1].transform.GetSiblingIndex());
            }
            else
            {
                components[newIndex].transform.SetAsLastSibling();
            }
        }

        private void ApplyReordering(SerializedProperty arrayProperty, int oldIndex, int newIndex)
        {
            arrayProperty.serializedObject.ApplyModifiedProperties();
            arrayProperty.serializedObject.Update();

            int otherIndex = newIndex;
            if (newIndex > oldIndex)
            {
                otherIndex = newIndex > 0 ? newIndex - 1 : 0;
            }
            else
            {
                otherIndex = newIndex + 1 < arrayProperty.arraySize ? newIndex + 1 : arrayProperty.arraySize - 1;
            }
            var toMove = arrayProperty.GetArrayElementAtIndex(newIndex).GetGameObject();
            var nextObj = arrayProperty.GetArrayElementAtIndex(otherIndex).GetGameObject();
            int toMoveIndex = toMove.transform.GetSiblingIndex();
            int nextObjIndex = nextObj.transform.GetSiblingIndex();
            toMove.transform.SetSiblingIndex(nextObj.transform.GetSiblingIndex());
        }
    }
}
