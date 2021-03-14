namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Common;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [CustomPropertyDrawer(typeof(EmphasizeAttribute))]
    public class EmphasizeAttributeDrawer : ComposablePropertyDrawer
    {
        private static readonly GUIContent s_emptyContent = new GUIContent("");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.PrefixLabel(position, label, EditorStyles.boldLabel);
            position.width -= EditorGUIUtility.labelWidth;
            position.x += EditorGUIUtility.labelWidth;
            EditorGUI.PropertyField(position, property, s_emptyContent);
        }
    }
}
