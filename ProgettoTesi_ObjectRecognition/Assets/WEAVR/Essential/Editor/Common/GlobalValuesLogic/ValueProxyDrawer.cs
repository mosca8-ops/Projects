using System.Reflection;
using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

namespace TXT.WEAVR.Common
{
    [CustomPropertyDrawer(typeof(ValueProxy), true)]
    public class ValueProxyDrawer : ComposablePropertyDrawer
    {
        private class Styles : BaseStyles
        {
            public GUIStyle box;
            public GUIStyle isVarToggle;
            public GUIStyle varText;

            protected override void InitializeStyles(bool isProSkin)
            {
                box = new GUIStyle("Box");
                isVarToggle = new GUIStyle(EditorStyles.miniButton)
                {
                    fixedWidth = 20f
                };
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

        private FieldInfo m_valueFieldInfo;

        private SerializedProperty GetValue(SerializedProperty property) => property.FindPropertyRelative("m_value");
        private SerializedProperty GetPropertyName(SerializedProperty property) => property.FindPropertyRelative("m_variableName");
        private SerializedProperty GetIsVar(SerializedProperty property) => property.FindPropertyRelative("m_isVar");

        private FieldInfo GetValueFieldInfo(SerializedProperty property)
        {
            if(m_valueFieldInfo == null)
            {
                m_valueFieldInfo = property.GetFieldInfo();
            }
            return m_valueFieldInfo;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout)
            {
                s_styles.Refresh();
                //m_styles.box.Draw(position, false, false, false, false);
            }

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;

            var isVar = GetIsVar(property);

            position.width -= s_styles.isVarToggle.fixedWidth + 2;
            var isVariable = GUI.Toggle(new Rect(position.xMax + 2, position.y, s_styles.isVarToggle.fixedWidth, EditorGUIUtility.singleLineHeight), isVar.boolValue, "V", s_styles.isVarToggle);

            if(isVariable != isVar.boolValue)
            {
                isVar.boolValue = isVariable;
                if (isVariable)
                {
                    var valueProperty = GetValue(property);
                    // Clear the reference -> to not hold them hidden
                    if(valueProperty.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        var prevObj = valueProperty.objectReferenceValue;
                        valueProperty.objectReferenceValue = null;
                        NotifyObjectValueChange(valueProperty, null, prevObj);
                    }
                }
            }

            if (isVariable)
            {
                var name = GetPropertyName(property);
                position = EditorGUI.PrefixLabel(position, label);
                GUI.Label(new Rect(position.x, position.y, 36, position.height), "VAR", s_styles.varText);
                position.x += 36;
                position.width -= 36;
                var color = GUI.color;
                if (string.IsNullOrEmpty(name.stringValue))
                {
                    GUI.color = Color.red;
                }
                name.stringValue = EditorGUI.TextField(position, name.stringValue);
                GUI.color = color;
            }
            else
            {
                var value = GetValue(property);
                if (value.propertyType == SerializedPropertyType.ObjectReference)
                {
                    var newObj = WeavrGUI.DraggableObjectField(this, position, label, value.objectReferenceValue, GetValueFieldInfo(value).FieldType, true);
                    if (newObj != value.objectReferenceValue)
                    {
                        var prevObj = value.objectReferenceValue;
                        value.objectReferenceValue = newObj;
                        NotifyObjectValueChange(value, newObj, prevObj);
                    }
                }
                else
                {
                    EditorGUI.PropertyField(position, value, label);
                }
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetIsVar(property).boolValue ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing : EditorGUI.GetPropertyHeight(GetValue(property));
        }

        private void NotifyObjectValueChange(SerializedProperty property, Object newObj, Object prevObj)
        {
            if (property.serializedObject.targetObject is Procedure.ProcedureObject pObj)
            {
                var editor = Procedure.ProcedureObjectEditor.Get(pObj);
                if (editor)
                {
                    editor.RegisterObjectValueChange(property.propertyPath, newObj, prevObj);
                }
            }
        }
    }
}