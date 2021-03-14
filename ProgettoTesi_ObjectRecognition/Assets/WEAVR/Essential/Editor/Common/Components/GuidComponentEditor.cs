using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TXT.WEAVR
{
    [CustomEditor(typeof(GuidComponent))]
    public class GuidComponentEditor : UnityEditor.Editor
    {
        private GuidComponent m_component;

        private void OnEnable()
        {
            m_component = target as GuidComponent;
        }

        public override VisualElement CreateInspectorGUI()
        {
            TextField label = new TextField();
            label.styleSheets.Add(WeavrStyles.StyleSheets.Common);
            label.styleSheets.Add(WeavrStyles.StyleSheets.Active);
            label.AddToClassList("guid-component");
            if(PrefabUtility.IsPartOfPrefabAsset(m_component) || m_component.gameObject.IsEditingInPrefabMode())
            {
                label.value = "Prefabs cannot have GUIDs";
                label.AddToClassList("prefab-guid");
            }
            else if(m_component.Guid == System.Guid.Empty)
            {
                label.value = "INVALID GUID";
                label.AddToClassList("invalid-guid");
            }
            else
            {
                label.value = m_component.Guid.ToString();
            }

            label.isReadOnly = true;

            return label;
        }
    }
}