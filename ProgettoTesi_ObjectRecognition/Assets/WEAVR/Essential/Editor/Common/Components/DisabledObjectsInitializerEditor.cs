namespace TXT.WEAVR.Editor
{
    using TXT.WEAVR.Common;
    using TXT.WEAVR.Core;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(DisabledObjectsInitializer))]
    public class DisabledObjectsInitializerEditor : Editor
    {
        public override void OnInspectorGUI() 
        {
            base.OnInspectorGUI();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Find All In Scene"))
            {
                (target as DisabledObjectsInitializer).FindInScene();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
    }
}