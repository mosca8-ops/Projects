using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(AssignableFromAttribute))]
    public class AssignableFromAttributeDrawer : ComposablePropertyDrawer
    {
        private class Styles : BaseStyles
        {
            public GUIStyle varText;

            protected override void InitializeStyles(bool isProSkin)
            {
                varText = WeavrStyles.ControlsSkin.FindStyle("globalValues_varLabel");
                if (varText == null)
                {
                    varText = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    varText.fontStyle = FontStyle.Bold;
                    varText.wordWrap = true;
                    varText.normal.textColor = Color.cyan;
                }
            }
        }

        private static Styles s_styles = new Styles();

        private AssignableFromAttribute m_attribute;
        private AssignableFromAttribute Attribute
        {
            get
            {
                if(m_attribute == null)
                {
                    m_attribute = attribute as AssignableFromAttribute;
                }
                return m_attribute;
            }
        }

        private VariableFieldDrawer Drawer { get; set; } = new VariableFieldDrawer();


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsValidForAssignment(property))
            {
                if (!string.IsNullOrEmpty(Attribute?.VariableName))
                {
                    bool wasEnabled = GUI.enabled;
                    GUI.enabled = false;
                    DrawVariableField(position, label, Attribute.VariableName);
                    GUI.enabled = wasEnabled;
                    return;
                }
                if (!string.IsNullOrEmpty(Attribute?.VariableFieldName))
                {
                    var varProperty = property.serializedObject.FindProperty(Attribute.VariableFieldName) ?? property.GetParent()?.FindPropertyRelative(Attribute.VariableFieldName);
                    if(varProperty != null && varProperty.propertyType == SerializedPropertyType.String)
                    {
                        if(!Drawer.TryDrawField(position, varProperty, label, out position))
                        {
                            if (property.propertyType == SerializedPropertyType.ObjectReference)
                            {
                                var newObj = WeavrGUI.DraggableObjectField(this, position, label, property.objectReferenceValue, fieldInfo.FieldType, true);
                                if (newObj != property.objectReferenceValue)
                                {
                                    var path = property.propertyPath;
                                    var prevObj = property.objectReferenceValue;
                                    property.objectReferenceValue = newObj;
                                    NotifyObjectValueChange(property, path, newObj, prevObj);
                                }
                            }
                            else
                            {
                                base.OnGUI(position, property, label);
                            }
                        }
                        return;
                    }
                }
            }
            base.OnGUI(position, property, label);
        }

        private void NotifyObjectValueChange(SerializedProperty property, string path, UnityEngine.Object newObj, UnityEngine.Object prevObj)
        {
            if(property.serializedObject.targetObject is Procedure.ProcedureObject pObj)
            {
                var editor = Procedure.ProcedureObjectEditor.Get(pObj);
                if (editor)
                {
                    editor.RegisterObjectValueChange(path, newObj, prevObj);
                }
            }
        }

        private string DrawVariableField(Rect position, GUIContent label, string varName)
        {
            s_styles.Refresh();
            position = EditorGUI.PrefixLabel(position, label);
            GUI.Label(new Rect(position.x, position.y, 36, position.height), "VAR", s_styles.varText);
            position.x += 36;
            position.width -= 36;
            return EditorGUI.DelayedTextField(position, varName);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if(property.propertyType != SerializedPropertyType.String)
            {
                return base.GetPropertyHeight(property, label);
            }
            if (IsValidForAssignment(property))
            {
                if (!string.IsNullOrEmpty(Attribute?.VariableName))
                {
                    return EditorGUIUtility.singleLineHeight;
                }
                if (!string.IsNullOrEmpty(Attribute?.VariableFieldName))
                {
                    var varProperty = property.serializedObject.FindProperty(Attribute.VariableFieldName);
                    if (varProperty != null && varProperty.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(varProperty.stringValue))
                    {
                        return EditorGUIUtility.singleLineHeight;
                    }
                }
            }
            return base.GetPropertyHeight(property, label);
        }

        private bool IsValidForAssignment(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer
                || property.propertyType == SerializedPropertyType.Float
                || property.propertyType == SerializedPropertyType.Boolean
                || property.propertyType == SerializedPropertyType.String
                || property.propertyType == SerializedPropertyType.Color
                || property.propertyType == SerializedPropertyType.Vector3
                || property.propertyType == SerializedPropertyType.ObjectReference;
        }
    }

    public class VariableFieldDrawer
    {
        public delegate void DrawDelegate(Rect position, SerializedProperty property, GUIContent label);

        private class Styles : BaseStyles
        {
            public GUIStyle varText;

            protected override void InitializeStyles(bool isProSkin)
            {
                varText = WeavrStyles.ControlsSkin.FindStyle("globalValues_varLabel");
                if (varText == null)
                {
                    varText = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                    varText.fontStyle = FontStyle.Bold;
                    varText.wordWrap = true;
                    varText.normal.textColor = Color.cyan;
                }
            }
        }

        private static Styles s_styles = new Styles();

        public bool TryDrawField(Rect position, SerializedProperty varProperty, GUIContent label, out Rect innerRect)
        {
            innerRect = position;
            if (varProperty != null)
            {
                if (varProperty != null && varProperty.propertyType == SerializedPropertyType.String)
                {
                    bool isVariable = !string.IsNullOrEmpty(varProperty.stringValue);
                    position.width -= 22;
                    if (isVariable)
                    {
                        varProperty.stringValue = DrawVariableField(position, label, varProperty.stringValue);
                    }

                    innerRect = position;
                    if (isVariable != GUI.Toggle(new Rect(position.xMax + 2, position.y, 20, EditorGUIUtility.singleLineHeight), isVariable, "V", EditorStyles.miniButton))
                    {
                        if (isVariable)
                        {
                            varProperty.stringValue = string.Empty;
                            return false;
                        }
                        else
                        {
                            varProperty.stringValue = "VariableName";
                            return true;
                        }
                    }
                    return isVariable;
                }
            }
            return false;
        }

        private string DrawVariableField(Rect position, GUIContent label, string varName)
        {
            s_styles.Refresh();
            position = EditorGUI.PrefixLabel(position, label);
            GUI.Label(new Rect(position.x, position.y, 36, position.height), "VAR", s_styles.varText);
            position.x += 36;
            position.width -= 36;
            return EditorGUI.DelayedTextField(position, varName);
        }
    }
}
