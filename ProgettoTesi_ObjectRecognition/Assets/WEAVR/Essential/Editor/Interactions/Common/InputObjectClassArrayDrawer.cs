namespace TXT.WEAVR.Interaction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(InputObjectClassArray))]
    public class InputObjectClassArrayDrawer : PropertyDrawer
    {
        private int valuesPerLine = 2;
        const float min_value_width = 150;

        private List<string> m_availableClassTypes = new List<string>();

        private GUIStyle _redPopupStyle;
        private GUIStyle RedPopupStyle {
            get {
                if(_redPopupStyle == null) {
                    _redPopupStyle = new GUIStyle(EditorStyles.popup);
                    _redPopupStyle.normal.textColor = Color.red;
                }
                return _redPopupStyle;
            }
        }
        
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            var propPosition = position;
            position.y += 4;

            Rect backgroundPosition = new Rect() {
                width = position.width + EditorGUIUtility.standardVerticalSpacing,
                height = position.height - EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing - 4,
                x = position.x,
                y = position.y + EditorGUIUtility.singleLineHeight
            };
            EditorGUI.DrawRect(backgroundPosition, WeavrStyles.Colors.faintGreen);

            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, label);

            //propPosition.x += propPosition.width - 100;
            //propPosition.width = 100;
            //propPosition.height = EditorGUIUtility.singleLineHeight + 4;
            //var lastColor = GUI.color;
            //GUI.color = Color.green;
            //if(GUI.Button(propPosition, "Sync Others"))
            //{
            //    SyncAll(property);
            //}
            //GUI.color = lastColor;

            property = property.FindPropertyRelative("_inputClasses");
            PrepareAvailableClasses(property);
            EditorGUI.indentLevel++;
            propPosition = position;
            propPosition.width = (propPosition.width / valuesPerLine) - 20;
            propPosition.y += propPosition.height + EditorGUIUtility.standardVerticalSpacing;
            var xPosition = propPosition;
            xPosition.width = 20;
            xPosition.x += propPosition.width;
            int index;
            float editorLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30;
            var color = GUI.backgroundColor;
            for (index = 0; index < property.arraySize; index++) {
                int indexMod = (index + 1) % valuesPerLine;
                var element = property.GetArrayElementAtIndex(index);
                DrawElement(propPosition, element, string.Format("{0}. ", index + 1));
                //EditorGUI.PropertyField(propPosition, element, new GUIContent(string.Format("{0}. ", index)));
                GUI.backgroundColor = Color.red;
                if (GUI.Button(xPosition, "X")) {
                    property.DeleteArrayElementAtIndex(index);
                }
                GUI.backgroundColor = color;
                propPosition.x = position.x + (propPosition.width + xPosition.width) * indexMod;
                xPosition.x = propPosition.x + propPosition.width;
                if (indexMod == 0) {
                    propPosition.y += propPosition.height + EditorGUIUtility.standardVerticalSpacing;
                    xPosition.y = propPosition.y;
                }
            }
            EditorGUIUtility.labelWidth = editorLabelWidth;
            EditorGUI.indentLevel--;

            propPosition.x += xPosition.width;
            if (property.arraySize < ObjectClassContainer.Count && GUI.Button(propPosition, "Add New")) {
                property.InsertArrayElementAtIndex(property.arraySize);
                property.GetArrayElementAtIndex(property.arraySize - 1).FindPropertyRelative("validType").stringValue = m_availableClassTypes.FirstOrDefault();
            }
            EditorGUI.EndProperty();
        }

        private void PrepareAvailableClasses(SerializedProperty property) {
            m_availableClassTypes.Clear();
            m_availableClassTypes.AddRange(ObjectClassContainer.GetClasses());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            valuesPerLine = (int)(EditorGUIUtility.currentViewWidth / min_value_width);
            property = property.FindPropertyRelative("_inputClasses");
            int arraySizeWithButton = property.arraySize < ObjectClassContainer.Count ? property.arraySize : property.arraySize - 1;
            float additionalSize = property.arraySize == 0 || valuesPerLine == 0 ? 2 : arraySizeWithButton / valuesPerLine + 2;
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * additionalSize + 4;
        }

        public void DrawElement(Rect position, SerializedProperty property, string label) {
            var typeProperty = property.FindPropertyRelative("validType");
            if (string.IsNullOrEmpty(typeProperty.stringValue) && m_availableClassTypes.Count > 0) {
                typeProperty.stringValue = m_availableClassTypes[0];
            }
            if (m_availableClassTypes.Contains(typeProperty.stringValue)) {
                typeProperty.stringValue = WeavrGUI.Popup(GUIUtility.GetControlID(FocusType.Passive), position, label, typeProperty.stringValue, m_availableClassTypes, s => s);
            }
            else {
                // Make it red
                var color = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                m_availableClassTypes.Add(typeProperty.stringValue);
                typeProperty.stringValue = WeavrGUI.Popup(GUIUtility.GetControlID(FocusType.Passive), position, label, typeProperty.stringValue, m_availableClassTypes, s => s);
                m_availableClassTypes.Remove(typeProperty.stringValue);
                GUI.backgroundColor = color;
            }
            m_availableClassTypes.Remove(typeProperty.stringValue);
        }
    }
}