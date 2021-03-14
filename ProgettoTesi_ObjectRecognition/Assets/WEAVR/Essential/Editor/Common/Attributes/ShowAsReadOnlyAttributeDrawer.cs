namespace TXT.WEAVR.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using TXT.WEAVR.Common;
    using UnityEditor;
    using UnityEditor.Experimental;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.SceneManagement;

    [CustomPropertyDrawer(typeof(ShowAsReadOnlyAttribute))]
    public class ShowAsReadOnlyAttributeDrawer : ComposablePropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            base.OnGUI(position, property, label);
            GUI.enabled = wasEnabled;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var element = base.CreatePropertyGUI(property);
            Debug.Log($"Got Property: {element}");
            element.SetEnabled(false);
            return element;
            //return new Label(property.name);
        }
    }
}
