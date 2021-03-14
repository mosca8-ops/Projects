namespace TXT.WEAVR.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Common;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(GeneratedObject))]
    public class GeneratedObjecEditor : Editor
    {
        public override void OnInspectorGUI() {
            EditorGUILayout.HelpBox("This object is generated and can be destroyed by generator", MessageType.None);
            GUI.enabled = false;
            EditorGUILayout.ObjectField(serializedObject.FindProperty("m_generator"));
        }
    }
}
