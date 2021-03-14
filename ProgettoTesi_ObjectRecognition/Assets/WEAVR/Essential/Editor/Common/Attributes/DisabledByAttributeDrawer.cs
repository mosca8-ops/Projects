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

    [CustomPropertyDrawer(typeof(DisabledByAttribute))]
    public class DisabledByAttributeDrawer : ComposablePropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var attr = (DisabledByAttribute)attribute;
            if (string.IsNullOrEmpty(attr.ControllingProperties)) {
                base.OnGUI(position, property, label);
                return;
            }
            string[] controllingProperties = attr.ControllingProperties.Split(';');
            bool? finalBoolValue = null;
            bool oneValidProperty = false;
            foreach (var split in controllingProperties) {
                var controller = property.serializedObject.FindProperty(split);
                if (controller != null) {
                    oneValidProperty = true;
                    if (controller.propertyType == SerializedPropertyType.Boolean)
                    {
                        finalBoolValue = (finalBoolValue ?? true) && controller.boolValue;
                    }
                    else if(controller.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        finalBoolValue = (finalBoolValue ?? true) && controller.objectReferenceValue;
                    }
                }
            }
            if (!oneValidProperty) {
                base.OnGUI(position, property, label);
                return;
            }
            bool wasEnabled = GUI.enabled;
            GUI.enabled = finalBoolValue.Value ^ attr.DisableWhenTrue;
            base.OnGUI(position, property, label);
            GUI.enabled = wasEnabled;
        }
    }
}
