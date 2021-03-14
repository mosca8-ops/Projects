using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(ArrayElementAttribute))]
    public class ArrayElementAttributeDrawer : PropertyDrawer
    {
        private System.Func<object> m_changeValueAction;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = attribute as ArrayElementAttribute;
            SerializedProperty arrayProp = GetArrayProperty(property, attr);
            if (arrayProp == null || !arrayProp.isArray)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if(m_changeValueAction != null)
            {
                property.TrySetValue(m_changeValueAction());
                m_changeValueAction = null;
            }

            if (label != null)
            {
                float lastWidth = position.width;
                position.width = EditorGUIUtility.labelWidth;
                EditorGUI.LabelField(position, label);
                position.x += position.width;
                position.width = lastWidth - position.width;
            }
            
            //object value = GetValue(property)
            var propertyValue = property.TryGetValue();
            if(attr.NotNull && propertyValue == null && arrayProp.arraySize > 0)
            {
                property.TryCopyValueFrom(arrayProp.GetArrayElementAtIndex(0));
                propertyValue = property.TryGetValue();
            }

            string popupLabel = null;
            if (!string.IsNullOrEmpty(attr.NamePath))
            {
                popupLabel = GetNameFromPath(property, attr.NamePath);
            }

            if (GUI.Button(position, popupLabel ?? GetNiceString(propertyValue), EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                var serializedObject = property.serializedObject;
                var propertyPath = property.propertyPath;
                for (int i = 0; i < arrayProp.arraySize; i++)
                {
                    var element = arrayProp.GetArrayElementAtIndex(i);
                    var value = element.TryGetValue();
                    menu.AddItem(new GUIContent(!string.IsNullOrEmpty(attr.NamePath) ? GetNameFromPath(element, attr.NamePath) : GetNiceString(value)),
                                SerializedProperty.DataEquals(property, element),
                                () => m_changeValueAction = () => value);
                }
                menu.DropDown(position);
            }
        }

        private static string GetNameFromPath(SerializedProperty property, string path)
        {
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue)
            {
                using(var serObj = new SerializedObject(property.objectReferenceValue))
                {
                    var nameProperty = serObj.FindProperty(path);
                    return nameProperty != null && nameProperty.propertyType == SerializedPropertyType.String ?
                        nameProperty.stringValue : null;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Generic)
            {
                var nameProperty = property.FindPropertyRelative(path);
                return nameProperty != null && nameProperty.propertyType == SerializedPropertyType.String ?
                        nameProperty.stringValue : null;
            }
            return null;
        }

        private string GetNiceString(object value)
        {
            if(value != null)
            {
                return value is Object ? (value as Object).name : value.ToString();
            }
            return "Nothing";
        }

        private static SerializedProperty GetArrayProperty(SerializedProperty property, ArrayElementAttribute attr)
        {
            var splits = attr.ArrayPath.Split('.');
            if(splits.Length > 1)
            {
                SerializedProperty inner = property.serializedObject.FindProperty(splits[0]);
                int i = 1;
                while(i < splits.Length && inner != null && inner.objectReferenceValue != null)
                {
                    if(inner.objectReferenceValue is Object)
                    {
                        inner = new SerializedObject(inner.objectReferenceValue).FindProperty(splits[i]);
                    }
                    else
                    {
                        inner = inner.FindPropertyRelative(splits[i]);
                    }
                    i++;
                }
                return inner;
            }
            return property.serializedObject.FindProperty(attr.ArrayPath);
        }

        private void UpdateValue(SerializedObject obj, string propertyPath, object value) {
            obj.UpdateIfRequiredOrScript();
            obj.FindProperty(propertyPath).TrySetValue(value);
            obj.ApplyModifiedProperties();
            //obj.ApplyModifiedProperties();
        }
    }
}