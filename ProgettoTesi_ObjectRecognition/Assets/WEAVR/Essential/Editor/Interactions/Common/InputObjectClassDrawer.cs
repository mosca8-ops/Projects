namespace TXT.WEAVR.Interaction
{
    using TXT.WEAVR.Editor;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(InputObjectClass))]
    public class InputObjectClassDrawer : PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);
            var typeProperty = property.FindPropertyRelative("validType");
            typeProperty.stringValue = WeavrGUI.Popup(position, position, label, typeProperty.stringValue, ObjectClassContainer.GetClasses(), s => s);
            EditorGUI.EndProperty();
        }
    }
}