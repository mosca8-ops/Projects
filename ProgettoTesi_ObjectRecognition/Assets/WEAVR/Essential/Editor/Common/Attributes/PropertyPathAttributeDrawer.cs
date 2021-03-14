namespace TXT.WEAVR.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(PropertyPathAttribute))]
    public class PropertyPathAttributeDrawer : PropertyDrawer
    {
        private PropertyPathField _propertyPathField;

        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            PropertyPathAttribute attr = attribute as PropertyPathAttribute;
            if (property.propertyType == SerializedPropertyType.String) {
                if(_propertyPathField == null) {
                    _propertyPathField = new PropertyPathField();
                }
                if (string.IsNullOrEmpty(attr.ObjectPropertyName)) {
                    property.stringValue = _propertyPathField.DrawPropertyPathField(position, label.text, property.serializedObject.targetObject, property.stringValue, true, true);
                }
                else {
                    var objProperty = property.serializedObject.FindProperty(attr.ObjectPropertyName);
                    if(objProperty != null) {
                        property.stringValue = _propertyPathField.DrawPropertyPathField(position, label.text, objProperty.objectReferenceValue, property.stringValue, true, true);
                    }
                    else {
                        EditorGUI.LabelField(position, label.text, "Unable to find '" + attr.ObjectPropertyName + "'");
                    }
                }
            }
            else {
                EditorGUI.LabelField(position, label.text, "Use PropertyPath with strings.");
            }
        }
    }
}
