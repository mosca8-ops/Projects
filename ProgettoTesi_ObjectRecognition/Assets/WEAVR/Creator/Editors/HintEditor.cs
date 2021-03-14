using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    [CustomEditor(typeof(Hint))]
    public class HintEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_Script");
            while (property.NextVisible(false))
            {
                EditorGUILayout.PropertyField(property, true);
            }
            serializedObject.ApplyModifiedProperties();
        }

        public float GetHeight()
        {
            float height = EditorGUIUtility.standardVerticalSpacing;
            serializedObject.Update();
            var property = serializedObject.FindProperty("m_Script");
            while (property.NextVisible(false))
            {
                height += EditorGUI.GetPropertyHeight(property, property.isExpanded) + EditorGUIUtility.standardVerticalSpacing;
            }
            return height;
        }
    }
}
