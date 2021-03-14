namespace TXT.WEAVR.Interaction
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(AbstractInteractiveBehaviour), true)]
    [CanEditMultipleObjects]
    public class InteractiveBehaviourEditor : Editor
    {
        public override void OnInspectorGUI() {
            // Draw class type
            var baseProperty = serializedObject.FindProperty("_objectClass");
            serializedObject.Update();
            EditorGUILayout.PropertyField(baseProperty);
            DrawInspector(baseProperty);
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawInspector(SerializedProperty currentProperty) {
            while (currentProperty.NextVisible(false)) {
                EditorGUILayout.PropertyField(currentProperty, true);
            }
        }
    }
}
