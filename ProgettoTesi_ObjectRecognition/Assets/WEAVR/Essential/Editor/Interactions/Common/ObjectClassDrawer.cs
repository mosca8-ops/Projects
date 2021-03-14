namespace TXT.WEAVR.Interaction
{
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(ObjectClass))]
    public class ObjectClassDrawer : PropertyDrawer
    {
        const float min_textField_width = 340;
        private bool _isModifying;
        private string _lastValidValue;
        private string _newValue;
        
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            var typeProperty = property.FindPropertyRelative("type");
            if (_isModifying) {
                Rect buttonRect = position;
                if(EditorGUIUtility.currentViewWidth > min_textField_width) {
                    position.width -= 100;
                    buttonRect.x += position.width + 5;
                }
                else {
                    buttonRect.x += EditorGUIUtility.labelWidth;
                    buttonRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                var lastColor = GUI.color;
                GUI.color = Color.green;
                _newValue = EditorGUI.TextField(position, label, _newValue/*, WeavrStyles.TextFieldGreen*/);
                GUI.color = lastColor;
                buttonRect.width = 40;
                if (GUI.Button(buttonRect, "Save")) {
                    if (_newValue != typeProperty.stringValue) {
                        UpdateGlobalClasses(typeProperty, _newValue);
                    }
                    _isModifying = false;

                    GUI.FocusControl(null);
                }

                buttonRect.x += buttonRect.width + 5;
                buttonRect.width = 50;
                if(GUI.Button(buttonRect, "Cancel")) {
                    _isModifying = false;
                    typeProperty.stringValue = _lastValidValue;

                    GUI.FocusControl(null);
                }
            }
            else {
                position = EditorGUI.PrefixLabel(position, label);
                position.width -= 55;
                string newSelectedValue = WeavrGUI.Popup(this, position, typeProperty.stringValue, ObjectClassContainer.GetClasses(), s => s);
                if(newSelectedValue != typeProperty.stringValue && !string.IsNullOrEmpty(newSelectedValue)) {
                    UpdateGlobalClasses(typeProperty, newSelectedValue);
                }
                position.x += position.width + 5;
                position.width = 50;
                if (GUI.Button(position, "New")) {
                    _isModifying = true;
                    _lastValidValue = typeProperty.stringValue;
                    _newValue = typeProperty.stringValue;
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return _isModifying && EditorGUIUtility.currentViewWidth <= min_textField_width ? 
                   base.GetPropertyHeight(property, label) * 2 : base.GetPropertyHeight(property, label);
        }

        private static void UpdateGlobalClasses(SerializedProperty typeProperty, string newValue) {
            ObjectClassContainer.Remove(typeProperty.stringValue);
            typeProperty.stringValue = newValue;
            if (!string.IsNullOrEmpty(newValue)) {
                ObjectClassContainer.Add(newValue);
            }
        }
    }
}