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

    [CustomPropertyDrawer(typeof(EnableIfComponentExistsAttribute))]
    public class EnableIfComponentExistsAttributeDrawer : ComposablePropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var attr = (EnableIfComponentExistsAttribute)attribute;
            var gameObject = property.serializedObject.targetObject as GameObject;
            if(gameObject == null) {
                var component = property.serializedObject.targetObject as Component;
                gameObject = component != null ? component.gameObject : null;
            }
            if (gameObject == null || attr.ControllingComponents == null || attr.ControllingComponents.Length == 0) {
                //EditorGUI.PropertyField(position, property, label);
                base.OnGUI(position, property, label);
                return;
            }
            
            bool enable = true;
            foreach (var type in attr.ControllingComponents) {
                enable &= gameObject.GetComponent(type) != null;
            }
            bool wasEnabled = GUI.enabled;
            GUI.enabled = enable ^ attr.DisableWhenTrue;
            //EditorGUI.PropertyField(position, property, label);
            base.OnGUI(position, property, label);
            GUI.enabled = wasEnabled;
        }
    }
}
