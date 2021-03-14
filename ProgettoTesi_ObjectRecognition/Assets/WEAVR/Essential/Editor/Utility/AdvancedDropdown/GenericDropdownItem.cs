using System;
using TXT.WEAVR.Controls;
using TXT.WEAVR.Editor;
using UnityEditor;
using UnityEngine;

namespace TXT.WEAVR.Controls
{
    internal class GenericDropdownItem : AdvancedDropdownItem
    {
        private string m_menuPath;
        private Action m_callback;

        internal GenericDropdownItem() : this("ROOT")
        {
        }

        internal GenericDropdownItem(string name) : base(name, -1)
        {
        }

        public GenericDropdownItem(string name, string localizedName, string menuPath, Texture2D icon, Action callback) : base(name, localizedName, menuPath, -1)
        {
            m_callback = callback;
            m_menuPath = menuPath;
            if (icon)
            {
                m_content = new GUIContent(name, icon);
            }
            else
            {
                m_content = new GUIContent(name);
            }
            m_contentWhenSearching = new GUIContent(m_content);
        }

        public override bool OnAction()
        {
            m_callback?.Invoke();
            return true;
        }

        public override string ToString()
        {
            return m_menuPath;
        }
    }
}
