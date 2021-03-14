using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Core;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomEditor(typeof(IDBookkeeper))]
    public class IDBookkeeperEditor : UnityEditor.Editor
    {
        private bool m_showAll;

        public override void OnInspectorGUI()
        {
            var idb = target as IDBookkeeper;
            var color = GUI.color;
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.HelpBox($"Registered Objects: {idb.RegisteredObjects}", MessageType.Info);
            EditorGUILayout.BeginVertical("Box", GUILayout.Width(100));
            var property = serializedObject.FindProperty("m_autoUpdate");
            if(property != null)
            {
                property.boolValue = EditorGUILayout.ToggleLeft("Auto Update", property.boolValue, GUILayout.Width(100));
            }
            GUI.color = Color.green;
            if (GUILayout.Button("Update"))
            {
                idb.RegisterAllUids();
            }
            GUI.color = color;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            m_showAll = EditorGUILayout.Foldout(m_showAll, "All registered objects", true);
            if (m_showAll)
            {
                foreach(var pair in idb.RegistredIds)
                {
                    EditorGUILayout.BeginHorizontal("Box");
                    WeavrGUILayout.DraggableObjectField(this, null, pair.Value?.gameObject, typeof(GameObject), true, GUILayout.Width(150));
                    //EditorGUILayout.ObjectField(pair.Value?.gameObject, typeof(GameObject), true, GUILayout.Width(150));
                    GUILayout.Label(pair.Key);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}