using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Utility;
using UnityEditor;
using UnityEngine;


namespace TXT.WEAVR.Debugging
{

    [CustomEditor(typeof(GroupBehaviourDebug))]
    public class GroupBehaviourDebugEditor : UnityEditor.Editor
    {

        private UnityEditorInternal.ReorderableList m_reorderableList;

        private Object m_componentToRemove;

        private GroupBehaviourDebug m_group;

        private void OnEnable()
        {
            m_group = target as GroupBehaviourDebug;
            m_group.UpdateDebuggers();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_behavioursToDebug");
            
            if (m_reorderableList == null)
            {
                m_reorderableList = new UnityEditorInternal.ReorderableList(serializedObject, property, true, true, true, false)
                {
                    drawElementCallback = (rect, i, a, f) =>
                    {
                        rect.y += 1;
                        rect.height -= 2;
                        rect.width -= 80;

                        var behaviourDebug = property.GetArrayElementAtIndex(i).GetObjectFromProperty<BehaviourDebug>();
                        if (behaviourDebug == null) { return; }
                        //var name = nameof(BehaviourDebug.behaviour);
                        //var behaviourProperty = arrayElement.FindPropertyRelative(name);
                        var serObj = new SerializedObject(behaviourDebug);
                        serObj.Update();
                        var behaviourProperty = serObj.FindProperty(nameof(BehaviourDebug.behaviour));
                        bool wasEnabled = GUI.enabled;
                        GUI.enabled = behaviourProperty.objectReferenceValue == null;
                        EditorGUI.PropertyField(rect, behaviourProperty, GUIContent.none);
                        serObj.ApplyModifiedProperties();
                        GUI.enabled = wasEnabled;
                        behaviourDebug.OnValidate();
                        rect.x += rect.width + 2;
                        rect.width = 30;
                        if(GUI.Button(rect, "Edit", EditorStyles.miniButton))
                        {
                            Selection.activeObject = behaviourDebug;
                        }
                        rect.x += rect.width + 4;
                        rect.width = 20;
                        behaviourDebug.IsActive = EditorGUI.Toggle(rect, behaviourDebug.IsActive);
                        //EditorGUI.PropertyField(rect, serObj.FindProperty("m_isActive"), GUIContent.none);
                        rect.x += rect.width + 4;
                        rect.width = 20;
                        if(GUI.Button(rect, @"–"))
                        {
                            m_group.RemoveBehaviourAt(i);
                        }
                    },
                    elementHeight = 20,
                    drawHeaderCallback = r =>
                    {
                        EditorGUI.LabelField(r, property.displayName);
                    },
                    onAddCallback = l =>
                    {
                        m_group.AddBehaviour();
                    },
                    onCanAddCallback = l =>  m_group.moduleSample != null && m_group.lineSample != null,
                    onReorderCallbackWithDetails = (l, oldIndex, newIndex) => {
                        ApplyReordering(serializedObject.FindProperty("m_behavioursToDebug"), oldIndex, newIndex);
                    }
                };
            }

            //bool wasEnabled = GUI.enabled;
            m_reorderableList.DoLayoutList();
            //GUI.enabled = wasEnabled;
            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyReordering(IEnumerable<Component> list, int oldIndex, int newIndex) {
            IList<Component> components = list is IList<Component> ? list as IList<Component> : new List<Component>(list);
            if(newIndex + 1 < components.Count)
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
            if(newIndex > oldIndex)
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
