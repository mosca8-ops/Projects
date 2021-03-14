using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace TXT.WEAVR.Common
{
    //[CustomPropertyDrawer(typeof(IndexedList<>))]
    //public class IndexedListDrawer : PropertyDrawer
    //{
        
    //}

    [CustomPropertyDrawer(typeof(AbstractIndexedPair), true)]
    public class GenericIndexedPairDrawer : PropertyDrawer
    {
        private Type type;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(type == null)
            {
                type = (fieldInfo.GetValue(property.serializedObject.targetObject) as AbstractIndexedPair)?.type;
                if(type == null) { return; }
            }
            float width = position.width;
            var p = property.FindPropertyRelative("key");
            position.width *= 0.3f;
            p.stringValue = EditorGUI.TextField(position, p.stringValue);
            position.x += position.width;
            position.width = width - position.width;
            p = property.FindPropertyRelative("value");
            EditorGUI.PropertyField(position, p, GUIContent.none);
        }
    }
}
