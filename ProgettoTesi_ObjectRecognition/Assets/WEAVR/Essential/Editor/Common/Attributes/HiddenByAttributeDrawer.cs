using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TXT.WEAVR.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TXT.WEAVR.Editor
{
    [CustomPropertyDrawer(typeof(HiddenByAttribute), true)]
    public class HiddenByAttributeDrawer : ComposablePropertyDrawer
    {
        private List<string> m_validationPaths;
        private bool m_initialized;
        private SerializedObject m_lastSerObj;
        private HiddenByAttribute m_attribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if(m_validationPaths == null)
            {
                Initialize(property);
            }
            if (m_validationPaths.Count == 0 || CheckIfShouldRender(property)) {
                base.OnGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if(!m_initialized || m_lastSerObj != property.serializedObject)
            {
                m_initialized = true;
                m_lastSerObj = property.serializedObject;
                Initialize(property);
            }
            return CheckIfShouldRender(property) ? base.GetPropertyHeight(property, label) 
                                                                               : -EditorGUIUtility.standardVerticalSpacing;
        }

        private void Initialize(SerializedProperty property)
        {
            m_attribute = attribute as HiddenByAttribute;
            m_validationPaths = new List<string>();
            string[] controllingProperties = m_attribute.ControllingProperties.Split(';');
            foreach(var split in controllingProperties)
            {
                var controller = property.serializedObject.FindProperty(split);
                if(controller == null)
                {
                    controller = property.serializedObject.FindProperty(property.propertyPath.Replace(property.name, split));
                }
                if (controller == null)
                {
                    Debug.LogError($"[HiddenByAttribute]: Unable to find property with path {split}");
                    continue;
                }
                else if (controller.propertyType == SerializedPropertyType.Boolean
                    || controller.propertyType == SerializedPropertyType.Integer
                    || controller.propertyType == SerializedPropertyType.Float
                    || controller.propertyType == SerializedPropertyType.String
                    || controller.propertyType == SerializedPropertyType.ObjectReference)
                {
                    m_validationPaths.Add(controller.propertyPath);
                }
            }
        }

        private bool CheckIfShouldRender(SerializedProperty property) {
            bool finalBoolValue = true;
            bool oneValidProperty = false;
            foreach (var validationPath in m_validationPaths)
            {
                var controller = property.serializedObject.FindProperty(validationPath);
                switch (controller.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        oneValidProperty = true;
                        finalBoolValue &= controller.boolValue;
                        break;
                    case SerializedPropertyType.Integer:
                        oneValidProperty = true;
                        finalBoolValue &= controller.intValue != 0;
                        break;
                    case SerializedPropertyType.Float:
                        oneValidProperty = true;
                        finalBoolValue &= controller.floatValue != 0;
                        break;
                    case SerializedPropertyType.String:
                        oneValidProperty = true;
                        finalBoolValue &= !string.IsNullOrEmpty(controller.stringValue);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        oneValidProperty = true;
                        finalBoolValue &= controller.objectReferenceValue;
                        break;
                }
            }
            return !oneValidProperty || (finalBoolValue ^ m_attribute.HideWhenTrue);
        }

    }
}
