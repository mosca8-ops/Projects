namespace TXT.WEAVR.Common
{
    using UnityEditor;
    using UnityEditor.Animations;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(AnimatorParametersArray))]
    public class AnimatorParametersDrawer : PropertyDrawer
    {
        private float _separatingRatio = 1.05f;
        private float _padding = 30f;
        private float _rowHeight;
        private int _typeIndex = -1;

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label) {
            SerializedProperty parameters = prop.FindPropertyRelative("_parameters");
            SerializedProperty type = prop.FindPropertyRelative("_type");

            // Check if type has changed
            if(_typeIndex != type.enumValueIndex && _typeIndex != -1) {
                parameters.ClearArray();
            }
            _typeIndex = type.enumValueIndex;


            pos.y += _padding * 0.5f;
            var fullRect = pos;
            var heightSkip = _rowHeight * _separatingRatio;

            pos.y -= _padding * 0.25f;
            pos.x -= _padding * 0.25f;
            pos.width += _padding * 0.5f;
            pos.height -= _padding * 0.5f;
            EditorGUI.DrawRect(pos, new Color(0.9f, 0.9f, 0.9f));

            pos.y = fullRect.y;
            pos.x = fullRect.x;
            pos.height = _rowHeight;
            pos.width = fullRect.width * 0.75f;
            EditorGUI.PropertyField(pos, type, label);

            if(type.enumValueIndex < 0) {
                return;
            }

            pos.x += fullRect.width * 0.8f;
            pos.width = fullRect.width * 0.2f;
            if(GUI.Button(pos, "Clear")) {
                parameters.ClearArray();
            }

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            var indentX = fullRect.x;

            pos.x = fullRect.x;
            pos.y += heightSkip;
            pos.width = fullRect.width * 0.4f;
            EditorGUI.LabelField(pos, "Name");

            pos.x += pos.width;
            EditorGUI.LabelField(pos, "Value");

            pos.x += pos.width;
            pos.width = _rowHeight * 1.5f;
            if(GUI.Button(pos, "+")) {
                parameters.InsertArrayElementAtIndex(parameters.arraySize);
            }

            // Draw a horizontal line
            pos.width = fullRect.width;
            pos.x = indentX * 2;
            pos.y += heightSkip - 1;
            pos.height = 1;
            EditorGUI.DrawRect(pos, Color.black);

            // Prepare the rects for each element
            Rect namePos = fullRect;
            Rect valuePos = fullRect;
            Rect removePos = fullRect;
            namePos.y = valuePos.y = removePos.y = pos.y + pos.height + 1;
            namePos.height = valuePos.height = removePos.height = _rowHeight;

            namePos.width = valuePos.width = fullRect.width * 0.4f;
            removePos.width = _rowHeight * 1.5f;

            valuePos.x += namePos.width;
            removePos.x = valuePos.x + valuePos.width;

            // Start drawing each element
            int i = 0;
            while(i < parameters.arraySize) {
                var elem = parameters.GetArrayElementAtIndex(i);

                var name = elem.FindPropertyRelative("name");
                name.stringValue = EditorGUI.TextField(namePos, name.stringValue);
                
                switch (type.enumNames[type.enumValueIndex]) {
                    case "Bool":
                        var boolValue = elem.FindPropertyRelative("boolValue");
                        boolValue.boolValue = EditorGUI.Toggle(valuePos, boolValue.boolValue);
                        break;
                    case "Float":
                        var floatValue = elem.FindPropertyRelative("numericValue");
                        floatValue.floatValue = EditorGUI.FloatField(valuePos, floatValue.floatValue);
                        break;
                    case "Int":
                        var intValue = elem.FindPropertyRelative("numericValue");
                        intValue.floatValue = EditorGUI.IntField(valuePos, (int)intValue.floatValue);
                        break;
                    case "Trigger":
                        EditorGUI.LabelField(valuePos, "Trigger");
                        break;
                    default:
                        EditorGUI.LabelField(valuePos, "Unknown");
                        break;
                }

                if (GUI.Button(removePos, "-")) {
                    parameters.DeleteArrayElementAtIndex(i);
                } else {
                    namePos.y = valuePos.y = removePos.y = namePos.y + heightSkip;
                    i++;
                }
            }

            EditorGUI.indentLevel = indentLevel;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            SerializedProperty parameters = property.FindPropertyRelative("_parameters");
            _rowHeight = base.GetPropertyHeight(property, label);
            return _rowHeight * (parameters.arraySize + 2.5f) * _separatingRatio + _padding;
        }
    }
}