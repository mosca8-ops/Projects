using System;
using TXT.WEAVR.Controls;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Procedure
{
    internal class ComponentDropdownItem : AdvancedDropdownItem
    {
        private string m_menuPath;
        private bool m_isLegacy;

        private Component m_component;
        private Action<Component> m_callback;

        internal ComponentDropdownItem() : this("ROOT")
        {
        }

        internal ComponentDropdownItem(string name) : base(name, -1)
        {
        }

        public ComponentDropdownItem(string name, string localizedName, int index) : base(name, localizedName, index)
        {
        }

        public ComponentDropdownItem(string name, string localizedName, string menuPath, string command) : base(name, localizedName, menuPath, -1)
        {
            m_menuPath = menuPath;
            m_isLegacy = menuPath.Contains("Legacy");

            if (command.StartsWith("SCRIPT"))
            {
                var scriptId = int.Parse(command.Substring(6));
                var obj = EditorUtility.InstanceIDToObject(scriptId);
                var icon = AssetPreview.GetMiniThumbnail(obj);
                m_content = new GUIContent(name, icon);
            }
            m_contentWhenSearching = new GUIContent(m_content);
            if (m_isLegacy)
            {
                m_contentWhenSearching.text += " (Legacy)";
            }
        }

        public ComponentDropdownItem(Component component, Action<Component> callback) : 
            this(EditorTools.NicifyName(component.GetType().Name), EditorTools.NicifyName(component.GetType().Name), EditorTools.NicifyName(component.GetType().Name), component, callback)
        {

        }

        public ComponentDropdownItem(string name, string localizedName, string menuPath, Component component, Action<Component> callback) : base(name, localizedName, menuPath, -1)
        {
            m_component = component;
            m_callback = callback;
            m_menuPath = menuPath;
            if (component)
            {
                var icon = AssetPreview.GetMiniThumbnail(component);
                m_content = new GUIContent(name, icon);
            }
            else
            {
                m_content = new GUIContent(name);
            }
            m_contentWhenSearching = new GUIContent(m_content);
        }

        public override int CompareTo(object o)
        {
            if (o is ComponentDropdownItem)
            {
                // legacy elements should always come after non legacy elements
                var componentElement = (ComponentDropdownItem)o;
                if (m_isLegacy && !componentElement.m_isLegacy)
                    return 1;
                if (!m_isLegacy && componentElement.m_isLegacy)
                    return -1;
            }
            return base.CompareTo(o);
        }

        public override bool OnAction()
        {
            m_callback?.Invoke(m_component);
            return true;
        }

        public override string ToString()
        {
            return m_menuPath;
        }
    }
}
