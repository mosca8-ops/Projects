using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Core
{
    [CustomEditor(typeof(WeavrCamera))]
    public class WeavrCameraEditor : UnityEditor.Editor
    {

        void OnEnable()
        {

        }


        //-------------------------------------------------
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_type"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}