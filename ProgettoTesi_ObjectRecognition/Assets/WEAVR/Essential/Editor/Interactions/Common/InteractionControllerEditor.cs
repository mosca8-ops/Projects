namespace TXT.WEAVR.Interaction
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(AbstractInteractionController), true)]
    public class InteractionControllerEditor : Editor
    {
        private UnityEditorInternal.ReorderableList m_reorderableList;

        private static GUIContent s_alwaysShowMenu;
        private static GUIStyle s_alwaysShowMenuStyle;

        private Object m_componentToRemove;

        private void OnEnable()
        {
            if(s_alwaysShowMenu == null)
            {
                s_alwaysShowMenu = new GUIContent(
                                serializedObject.FindProperty(nameof(AbstractInteractionController.alwaysShowMenu))
                                                .displayName, 
                                "Show menu even if there is only one action available");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var defaultBehaviourProperty = serializedObject.FindProperty("m_defaultBehaviour");
            var baseProperty = serializedObject.FindProperty("bagHolder");
            serializedObject.Update();
            EditorGUILayout.PropertyField(baseProperty);
            //LegacyDraw(baseProperty);
            CompactDraw(baseProperty);
            baseProperty = serializedObject.FindProperty("m_behaviours");
            //EditorGUILayout.LabelField("Behaviours", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            if (m_reorderableList == null)
            {
                m_reorderableList = new UnityEditorInternal.ReorderableList(serializedObject, baseProperty, true, false, false, true)
                {
                    drawElementCallback = (rect, i, a, f) =>
                    {
                        rect.width -= 80;
                        var arrayElement = baseProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (arrayElement == null) { return; }
                        bool isDefault = arrayElement == defaultBehaviourProperty.objectReferenceValue;
                        if (isDefault)
                        {
                            EditorGUI.LabelField(rect, arrayElement.GetType().Name, EditorStyles.boldLabel);
                        }
                        else
                        {
                            EditorGUI.LabelField(rect, arrayElement.GetType().Name);
                        }

                        rect.x += rect.width;
                        rect.width = 80;
                        if (isDefault)
                        {
                            var lastColor = GUI.color;
                            GUI.color = Color.cyan;
                            EditorGUI.LabelField(rect, "[ DEFAULT ]");
                            GUI.color = lastColor;
                        }
                        else if (arrayElement is AbstractInteractiveBehaviour && ((AbstractInteractiveBehaviour)arrayElement).CanBeDefault && GUI.Button(rect, "Set default", EditorStyles.miniButton))
                        {
                            defaultBehaviourProperty.objectReferenceValue = arrayElement;
                        }
                    },
                    drawHeaderCallback = r =>
                    {
                        r.width -= 120;
                        EditorGUI.LabelField(r, "Behaviours", EditorStyles.boldLabel);
                        r.x += r.width;
                        r.width = 120;
                        if (GUI.Button(r, "Remove Default", EditorStyles.miniButton))
                        {
                            defaultBehaviourProperty.objectReferenceValue = null;
                        }
                    },
                    onRemoveCallback = l =>
                    {
                        var toRemove = baseProperty.GetArrayElementAtIndex(l.index);
                        if (toRemove != null)
                        {
                            DeleteComponent(toRemove.objectReferenceValue);
                        }
                    },
                };
            }
            m_reorderableList.DoLayoutList();
            //bool guiWasEnabled = GUI.enabled;
            //GUI.enabled = false;
            //for (int i = 0; i < baseProperty.arraySize; i++) {
            //    EditorGUILayout.PropertyField(baseProperty.GetArrayElementAtIndex(i));
            //}
            //GUI.enabled = guiWasEnabled;
            EditorGUILayout.BeginHorizontal("Box", GUILayout.MinHeight(24));
            if (GUILayout.Button("Update List") || baseProperty.arraySize != ((AbstractInteractionController)target).GetComponents<AbstractInteractiveBehaviour>().Length)
            {
                ((AbstractInteractionController)target).UpdateList();
            }
            if (GUILayout.Button("Sync Class Types"))
            {
                ((AbstractInteractionController)target).SyncClassTypes();
            }
            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        private void CompactDraw(SerializedProperty baseProperty)
        {
            EditorGUILayout.BeginHorizontal("Box");

            if(s_alwaysShowMenuStyle == null)
            {
                s_alwaysShowMenuStyle = new GUIStyle("Button");
                s_alwaysShowMenuStyle.wordWrap = true;
                s_alwaysShowMenuStyle.fontSize = 10;
            }

            baseProperty = serializedObject.FindProperty(nameof(AbstractInteractionController.alwaysShowMenu));
            baseProperty.boolValue = GUILayout.Toggle(baseProperty.boolValue, s_alwaysShowMenu, 
                                                      s_alwaysShowMenuStyle, 
                                                      GUILayout.MaxWidth(100),
                                                      GUILayout.ExpandHeight(true));
            
            EditorGUILayout.BeginVertical("Box");
            baseProperty = serializedObject.FindProperty(nameof(AbstractInteractionController.hoverColor));
            GUILayout.Label(baseProperty.displayName, EditorStyles.centeredGreyMiniLabel);
            baseProperty.colorValue = EditorGUILayout.ColorField(baseProperty.colorValue);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("Box");
            baseProperty = serializedObject.FindProperty(nameof(AbstractInteractionController.selectColor));
            GUILayout.Label(baseProperty.displayName, EditorStyles.centeredGreyMiniLabel);
            baseProperty.colorValue = EditorGUILayout.ColorField(baseProperty.colorValue);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            
            while (baseProperty.NextVisible(false) && baseProperty.name != "m_behaviours")
            {
                EditorGUILayout.PropertyField(baseProperty);
            }
        }

        private void LegacyDraw(SerializedProperty baseProperty)
        {
            baseProperty.NextVisible(false);
            while (baseProperty.name != "m_behaviours")
            {
                EditorGUILayout.PropertyField(baseProperty);
                baseProperty.NextVisible(false);
            }
        }

        private void DeleteComponent(Object component)
        {
            m_componentToRemove = component;
            EditorApplication.update -= DeleteComponent;
            EditorApplication.update += DeleteComponent;
        }

        private void DeleteComponent()
        {
            if(m_componentToRemove != null)
            {
                Undo.DestroyObjectImmediate(m_componentToRemove);
            }
            EditorApplication.update -= DeleteComponent;
        }
    }
}
