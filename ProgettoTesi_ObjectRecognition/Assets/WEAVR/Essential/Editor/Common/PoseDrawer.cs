namespace TXT.WEAVR.Common
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(Pose))]
    public class PoseDrawer : PropertyDrawer
    {
        private float _basePropertyHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var posProperty = property.FindPropertyRelative("position");
            var rotProperty = property.FindPropertyRelative("rotation");
            var eulerAngles = property.FindPropertyRelative("euler");

            var fieldRect = position;
            fieldRect.height /= 3f;
            posProperty.vector3Value = EditorGUI.Vector3Field(fieldRect, (string)null, posProperty.vector3Value);
            fieldRect.y += fieldRect.height;
            eulerAngles.vector3Value = EditorGUI.Vector3Field(fieldRect, (string)null, eulerAngles.vector3Value);

            //rotProperty.
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return base.GetPropertyHeight(property, label) * 3;
        }
    }
}