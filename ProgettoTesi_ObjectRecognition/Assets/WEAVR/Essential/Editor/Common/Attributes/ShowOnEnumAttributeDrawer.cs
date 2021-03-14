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
    [CustomPropertyDrawer(typeof(ShowOnEnumAttribute), true)]
    public class ShowOnEnumAttributeDrawer : ComposablePropertyDrawer
    {
        bool[] isFlags;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (ShowOnEnumAttribute)attribute;
            if (string.IsNullOrEmpty(attr.FullEnumFieldString) || CheckIfShouldRender(property, attr))
            {
                base.OnGUI(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return CheckIfShouldRender(property, (ShowOnEnumAttribute)attribute) ? base.GetPropertyHeight(property, label) 
                                                                                 : -EditorGUIUtility.standardVerticalSpacing;
        }

        private bool CheckIfShouldRender(SerializedProperty property, ShowOnEnumAttribute attr)
        {
            bool? finalBoolValue = null;
            bool oneValidProperty = false;

            if(isFlags == null)
            {
                isFlags = new bool[attr.EnumFields.Length];
                for (int i = 0; i < isFlags.Length; i++)
                {
                    isFlags[i] = property.serializedObject.FindProperty(attr.EnumFields[i])?.GetAttribute<FlagsAttribute>() != null;
                }
            }

            for (int i = 0; i < attr.EnumFields.Length; i++)
            {
                var enumField = attr.EnumFields[i];
                var controller = property.serializedObject.FindProperty(enumField);
                if (controller != null && controller.propertyType == SerializedPropertyType.Enum)
                {
                    oneValidProperty = true;
                    if (isFlags[i])
                    {
                        finalBoolValue = (finalBoolValue ?? true) && ((controller.intValue & attr.EnumValue) == attr.EnumValue);
                    }
                    else
                    {
                        finalBoolValue = (finalBoolValue ?? true) && (controller.intValue == attr.EnumValue);
                    }
                }
            }
            return !oneValidProperty || finalBoolValue.Value ^ attr.HideOnEnum;
        }

    }
}
