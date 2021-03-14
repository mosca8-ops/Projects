using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.ImpactSystem
{
    [CustomEditor(typeof(TerrainMaterial))]
    public class TerrainMaterialEditor : UnityEditor.Editor
    {
        TerrainMaterial m_target;

        private void OnEnable()
        {
            m_target = target as TerrainMaterial;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("LAYERS", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fallbackMaterial"));


            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;

            var layersProperty = serializedObject.FindProperty("m_layers");
            for (int i = 0; i < layersProperty.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal("Box");
                if (!m_target.Layers[i].layer)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Layer is NULL");
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(40), GUILayout.Height(40));
                    EditorGUI.DrawPreviewTexture(GUILayoutUtility.GetRect(40, 40), GetFirstTexture(m_target.Layers[i].layer));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginVertical();
                    GUILayout.Label(m_target.Layers[i].layer.name, EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(layersProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(TerrainMaterial.LayerMaterial.material)));
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUIUtility.labelWidth = labelWidth;

            serializedObject.ApplyModifiedProperties();
        }

        private Texture GetFirstTexture(TerrainLayer layer)
        {
            return !layer ? null : layer.diffuseTexture ? layer.diffuseTexture : layer.normalMapTexture ? layer.normalMapTexture : layer.maskMapTexture;
        }
    }
}
