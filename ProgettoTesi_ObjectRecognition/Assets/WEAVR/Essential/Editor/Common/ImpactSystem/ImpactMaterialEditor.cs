using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TXT.WEAVR.ImpactSystem
{
    [CustomEditor(typeof(ImpactMaterial))]
    public class ImpactMaterialEditor : UnityEditor.Editor
    {

        private static GUIStyle s_boxStyle;
        private static GUIContent s_followGo = new GUIContent("F", "Follow collider");
        private static GUIContent s_revImpulse = new GUIContent("R", "Rotate to impulse");

        private ImpactMaterial m_material;

        private System.Action m_preRenderAction;

        private void OnEnable()
        {
            m_material = target as ImpactMaterial;
        }

        public override void OnInspectorGUI()
        {
            if(m_preRenderAction != null)
            {
                m_preRenderAction();
                m_preRenderAction = null;
            }

            serializedObject.Update();

            // Material
            var property = serializedObject.FindProperty("m_material");
            EditorGUILayout.PropertyField(property);

            // Sample Audio Source
            property.Next(false);
            EditorGUILayout.PropertyField(property);

            // Default Impact
            property.Next(false);
            EditorGUILayout.PropertyField(property);

            // Groupped Impacts
            property.Next(false);

            GUILayout.Label("IMPACTS WITH OTHER MATERIALS", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(4);

            for (int i = 0; i < property.arraySize; i++)
            {
                EditorGUILayout.BeginVertical("Box");
                DrawImpactGroup(property, i);
                EditorGUILayout.EndVertical();
                GUILayout.Space(6);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("ADD MATERIAL"))
            {
                property.InsertArrayElementAtIndex(property.arraySize);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }


        protected void DrawImpactGroup(SerializedProperty arrayProperty, int index)
        {
            var groupProperty = arrayProperty.GetArrayElementAtIndex(index);
            var dataProperty = groupProperty.FindPropertyRelative(nameof(ImpactMaterial.ImpactDataGroup.data));
            var labelWidth = EditorGUIUtility.labelWidth;
            var guiColor = GUI.color;

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 100;
            EditorGUILayout.PropertyField(groupProperty.FindPropertyRelative(nameof(ImpactMaterial.ImpactDataGroup.material)));
            EditorGUIUtility.labelWidth = labelWidth;

            //GUI.color = Color.red;
            if (GUILayout.Button("REMOVE", GUILayout.Width(80)))
            {
                arrayProperty.DeleteArrayElementAtIndex(index);
            }
            //GUI.color = guiColor;
            EditorGUILayout.EndHorizontal();

            if(arrayProperty.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("+", GUILayout.Width(32)))
            {
                dataProperty.InsertArrayElementAtIndex(dataProperty.arraySize);
            }
            GUILayout.Space(6);
            GUILayout.Label("IMPACTS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < dataProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(dataProperty.GetArrayElementAtIndex(i));
                //GUI.color = Color.red;
                if(GUILayout.Button("X", GUILayout.Height(20), GUILayout.Width(20)))
                {
                    dataProperty.DeleteArrayElementAtIndex(i--);
                }
                //GUI.color = guiColor;
                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                int impactIndex = index;
                m_preRenderAction = () => m_material.Impacts[impactIndex].Reorder();
            }
        }
    }
}
