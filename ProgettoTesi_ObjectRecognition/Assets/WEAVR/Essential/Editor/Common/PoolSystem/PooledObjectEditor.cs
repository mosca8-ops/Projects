using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR.Common
{
    [CustomEditor(typeof(PooledObject))]
    public class PooledObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var label = new Label("This is a pooled object");
            label.styleSheets.Add(WeavrStyles.StyleSheets.Common);
            label.AddToClassList("pooled-object-label");
            return label;
        }
    }
}
